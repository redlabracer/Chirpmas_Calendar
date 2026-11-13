using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChirpmasCalendar.Diagnostics;

namespace ChirpmasCalendar.Data
{
    // Downloads calendar.json from the VPS by winter.
    // Order = server (validated, then cached) -> last good cached download -> calendar.json shipped with the mod.
    // Without a URL (or with "Use local file") it behaves like LocalFileCalendarSource.
    // The HTTP Date header of a successful download is used as trusted "now" for unlocking doors.
    public sealed class RemoteCalendarSource : ICalendarSource
    {
        public const string kCacheFileName = "calendar.cache.json";

        private static readonly TimeSpan kTimeout = TimeSpan.FromSeconds(10);

        private static readonly HttpClient s_Client = CreateClient();

        private readonly Func<string> m_GetUrl;
        private readonly Func<bool> m_UseLocalFile;

        public RemoteCalendarSource(LocalFileCalendarSource local, Func<string> getUrl, Func<bool> useLocalFile)
        {
            Local = local;
            m_GetUrl = getUrl;
            m_UseLocalFile = useLocalFile;
        }

        public LocalFileCalendarSource Local { get; }

        public static string CacheFilePath => Path.Combine(LocalFileCalendarSource.UserDirectory, kCacheFileName);

        public async Task<CalendarFetchResult> FetchAsync(CancellationToken cancellationToken)
        {
            string url = m_GetUrl()?.Trim();

            if (m_UseLocalFile() || string.IsNullOrEmpty(url))
                return await Local.FetchAsync(cancellationToken).ConfigureAwait(false);

            if (!TryValidateUrl(url, out Uri uri, out string reason))
                return await FallbackAsync(reason, cancellationToken).ConfigureAwait(false);

            try
            {
                CalendarFetchResult result = await DownloadAsync(uri, cancellationToken).ConfigureAwait(false);

                // Reject broken server data here, so it neither replaces the cache nor empties the calendar.
                CalendarParser.Parse(result.Json);
                WriteCache(result.Json);
                return result;
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                return await FallbackAsync($"download from {uri} failed: {e.GetBaseException().Message}", cancellationToken).ConfigureAwait(false);
            }
        }

        // Only https, plus plain http to localhost for testing with a local web server.
        public static bool TryValidateUrl(string url, out Uri uri, out string reason)
        {
            reason = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                reason = $"'{url}' is not a valid URL";
                return false;
            }

            if (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
                return true;

            reason = $"'{url}' must start with https://";
            uri = null;
            return false;
        }

        private static async Task<CalendarFetchResult> DownloadAsync(Uri uri, CancellationToken cancellationToken)
        {
            using var profile = ModProfiler.Download.Measure();

            // HttpClient.Timeout stops at the response headers; this deadline also covers reading the body.
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                timeout.CancelAfter(kTimeout);
                CancellationToken token = timeout.Token;

                // The admin form edits the file during the event; never accept a stale proxy/CDN copy.
                request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

                try
                {
                    using (HttpResponseMessage response = await s_Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

                        long? length = response.Content.Headers.ContentLength;
                        if (length > CalendarParser.kMaxJsonLength)
                            throw new InvalidDataException($"response is larger than {CalendarParser.kMaxJsonLength / 1024} KB");

                        // Not every stream implementation observes the token once a read is pending; disposing the response always ends it.
                        using (token.Register(response.Dispose))
                        {
                            string json = await ReadLimitedAsync(response.Content, token).ConfigureAwait(false);
                            return new CalendarFetchResult(json, uri.ToString(), response.Headers.Date);
                        }
                    }
                }
                catch (Exception) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"no complete response within {kTimeout.TotalSeconds:0} s");
                }
            }
        }

        private static async Task<string> ReadLimitedAsync(HttpContent content, CancellationToken cancellationToken)
        {
            using (Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[16 * 1024];
                int read;
                while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    buffer.Write(chunk, 0, read);
                    if (buffer.Length > CalendarParser.kMaxJsonLength)
                        throw new InvalidDataException($"response is larger than {CalendarParser.kMaxJsonLength / 1024} KB");
                }

                buffer.Position = 0;
                using (var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    return reader.ReadToEnd();
            }
        }

        private async Task<CalendarFetchResult> FallbackAsync(string reason, CancellationToken cancellationToken)
        {
            using var profile = ModProfiler.Fallback.Measure();
            Mod.Log.Warn($"Using offline calendar data ({reason})");

            CalendarFetchResult offline = await Task.Run(() => TryReadCache(), cancellationToken).ConfigureAwait(false)
                ?? await Local.FetchBundledAsync(cancellationToken).ConfigureAwait(false);

            return new CalendarFetchResult(offline.Json, offline.Origin, isOffline: true);
        }

        private static CalendarFetchResult TryReadCache()
        {
            using var profile = ModProfiler.CacheRead.Auto();
            try
            {
                if (!File.Exists(CacheFilePath))
                    return null;

                string json = File.ReadAllText(CacheFilePath);
                CalendarParser.Parse(json);
                return new CalendarFetchResult(json, $"{CacheFilePath} (last download)");
            }
            catch (Exception e)
            {
                Mod.Log.Warn($"Ignoring unreadable calendar cache: {e.Message}");
                return null;
            }
        }

        private static void WriteCache(string json)
        {
            using var profile = ModProfiler.CacheWrite.Auto();
            try
            {
                Directory.CreateDirectory(LocalFileCalendarSource.UserDirectory);
                string temp = CacheFilePath + ".tmp";
                File.WriteAllText(temp, json, new UTF8Encoding(false));

                // Replace swaps the files in one step, so a crash never leaves the cache missing.
                if (File.Exists(CacheFilePath))
                    File.Replace(temp, CacheFilePath, null);
                else
                    File.Move(temp, CacheFilePath);
            }
            catch (Exception e)
            {
                // Not fatal the downloaded data is still used for this session.
                Mod.Log.Warn($"Could not write calendar cache: {e.Message}");
            }
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = kTimeout };
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Mod.Id}/{typeof(RemoteCalendarSource).Assembly.GetName().Version}");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return client;
        }
    }
}
