using System;

namespace ChirpmasCalendar.Data
{
    /// <summary>
    /// Local clock corrected by the last server time seen, so changing the PC clock cannot open doors early
    /// once the calendar is fetched from the VPS.
    /// </summary>
    public sealed class CalendarClock
    {
        private long m_OffsetTicks;

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow.AddTicks(System.Threading.Interlocked.Read(ref m_OffsetTicks));

        public void SyncWithServer(DateTimeOffset serverNow)
        {
            System.Threading.Interlocked.Exchange(ref m_OffsetTicks, (serverNow - DateTimeOffset.UtcNow).Ticks);
        }
    }
}
