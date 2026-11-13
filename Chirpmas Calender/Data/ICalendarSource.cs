using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChirpmasCalendar.Data
{
    public interface ICalendarSource
    {
        Task<CalendarFetchResult> FetchAsync(CancellationToken cancellationToken);
    }

    public sealed class CalendarFetchResult
    {
        public CalendarFetchResult(string json, string origin, DateTimeOffset? serverTime = null, bool isOffline = false)
        {
            Json = json;
            Origin = origin;
            ServerTime = serverTime;
            IsOffline = isOffline;
        }

        public string Json { get; }

        public string Origin { get; }

        public DateTimeOffset? ServerTime { get; }

        public bool IsOffline { get; }
    }
}
