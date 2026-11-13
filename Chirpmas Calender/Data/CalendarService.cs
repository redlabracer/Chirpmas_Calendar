using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ChirpmasCalendar.Diagnostics;

namespace ChirpmasCalendar.Data
{
    public enum CalendarStatus
    {
        Loading,
        Ready,
        Error,
    }

    public sealed class CalendarService
    {
        public const int kDayCount = 24;

        // How often GetView re-reads the clock when no door opens sooner. Catches server clock re-syncs and PC clock changes.
        private static readonly TimeSpan kViewRecheckInterval = TimeSpan.FromSeconds(5);

        // Rebuild slightly after a transition so the new view already contains it.
        private static readonly TimeSpan kTransitionMargin = TimeSpan.FromMilliseconds(50);

        private readonly object m_Lock = new object();
        private CalendarDocument m_Document;
        private CalendarStatus m_Status = CalendarStatus.Loading;
        private string m_Error;
        private bool m_Offline;
        private int m_Version;
        private int m_IsLoading;
        private long m_LastLoadStartedTicks;

        // View cache, main thread only (CalendarUISystem and CalendarChirpSystem).
        private CalendarView m_CachedView;
        private int m_CachedSimulatedDay = -1;
        private long m_CachedViewValidUntil;

        public CalendarService(ICalendarSource source, CalendarClock clock)
        {
            Source = source;
            Clock = clock;
        }

        public ICalendarSource Source { get; }

        public CalendarClock Clock { get; }
        public int Version => Volatile.Read(ref m_Version);

        public void Reload()
        {
            if (Interlocked.Exchange(ref m_IsLoading, 1) == 1)
                return;

            Interlocked.Exchange(ref m_LastLoadStartedTicks, DateTime.UtcNow.Ticks);
            Task.Run(LoadAsync);
        }

        /// Reloads only if the last attempt is older than <paramref name="maxAge"/>, so opening the panel often doesn't spam the server.
        public void ReloadIfOlderThan(TimeSpan maxAge)
        {
            long last = Interlocked.Read(ref m_LastLoadStartedTicks);
            if (DateTime.UtcNow.Ticks - last >= maxAge.Ticks)
                Reload();
        }

        private async Task LoadAsync()
        {
            using var profile = ModProfiler.Load.Measure();
            try
            {
                CalendarFetchResult result = await Source.FetchAsync(CancellationToken.None).ConfigureAwait(false);
                CalendarDocument document = CalendarParser.Parse(result.Json);

                if (result.ServerTime.HasValue)
                    Clock.SyncWithServer(result.ServerTime.Value);

                lock (m_Lock)
                {
                    m_Document = document;
                    m_Status = CalendarStatus.Ready;
                    m_Error = null;
                    m_Offline = result.IsOffline;
                }

                Mod.Log.Info($"Calendar loaded from {result.Origin} ({document.Days.Count} day entries{(result.IsOffline ? ", offline" : string.Empty)})");

                DateTime eventStart = CalendarSchedule.ResolveEventStart(document, out bool fromDocument);
                Mod.Log.Info($"Calendar schedule: {CalendarSchedule.Describe(eventStart, fromDocument)}");
            }
            catch (Exception e)
            {
                lock (m_Lock)
                {
                    m_Status = CalendarStatus.Error;
                    m_Error = e.Message;
                }

                Mod.Log.Warn(e, "Calendar could not be loaded");
            }
            finally
            {
                Interlocked.Exchange(ref m_IsLoading, 0);
                Interlocked.Increment(ref m_Version);
            }
        }

        /// <summary>
        /// The current view, rebuilt only when the data or <paramref name="simulatedDay"/> changed, when the next door opens/day ends,
        /// or every <see cref="kViewRecheckInterval"/>. In between it only costs a monotonic timer read, so it is safe to call every frame.
        /// Main thread only.
        /// </summary>
        public CalendarView GetView(int simulatedDay)
        {
            using var profile = ModProfiler.GetView.Auto();
            long timestamp = Stopwatch.GetTimestamp();
            int version = Version;
            if (m_CachedView != null && m_CachedView.DataVersion == version && m_CachedSimulatedDay == simulatedDay && timestamp < m_CachedViewValidUntil)
                return m_CachedView;

            CalendarView view = BuildView(simulatedDay);
            view.DataVersion = version;

            TimeSpan wait = kViewRecheckInterval;
            if (view.NextChangeAt.HasValue)
            {
                TimeSpan untilChange = view.NextChangeAt.Value - Clock.UtcNow + kTransitionMargin;
                if (untilChange < wait)
                    wait = untilChange > TimeSpan.Zero ? untilChange : TimeSpan.Zero;
            }

            m_CachedView = view;
            m_CachedSimulatedDay = simulatedDay;
            m_CachedViewValidUntil = timestamp + (long)(wait.TotalSeconds * Stopwatch.Frequency);
            return view;
        }

        /// <param name="simulatedDay">0 = real clock; 1-24 = noon of that event day; 25 = after the event.</param>
        public CalendarView BuildView(int simulatedDay)
        {
            using var profile = ModProfiler.BuildView.Auto();
            CalendarDocument document;
            CalendarStatus status;
            string error;
            bool offline;
            lock (m_Lock)
            {
                document = m_Document;
                status = m_Status;
                error = m_Error;
                offline = m_Offline;
            }

            return CalendarView.Build(document, status, error, offline, Clock.UtcNow, simulatedDay);
        }

        public static bool TryParseDate(string value, out DateTimeOffset result)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);
        }
    }
}
