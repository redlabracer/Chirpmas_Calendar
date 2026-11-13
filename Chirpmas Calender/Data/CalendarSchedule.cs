using System;
using System.Globalization;

namespace ChirpmasCalendar.Data
{
    /// <summary>
    /// When the doors open. All calendar dates run on Pacific time (America/Los_Angeles, UTC-8 in December),
    /// no matter which time zone the player's PC uses: door N opens on the Nth event date at <see cref="kUnlockTimeOfDay"/>.
    /// The first event date is the date of "eventStart" in calendar.json (server); <see cref="kEventStart"/> is only the fallback.
    /// </summary>
    public static class CalendarSchedule
    {
        // Fallback when calendar.json has no valid "eventStart". Door 1 opens at this wall-clock time in America/Los_Angeles.
        public static readonly DateTime kEventStart = new DateTime(2026, 12, 1, 0, 0, 5, DateTimeKind.Unspecified);

        // Every door opens at this time of day (Pacific time).
        public static readonly TimeSpan kUnlockTimeOfDay = new TimeSpan(0, 0, 0, 5);

        public const string kTimeZoneId = "America/Los_Angeles";

        // Used when the system has no Pacific time zone data (custom/non-Windows environments). Has no daylight saving time.
        public static readonly TimeSpan kFallbackUtcOffset = TimeSpan.FromHours(-8);

        // IANA ids work on Linux/macOS runtimes, "Pacific Standard Time" is the Windows registry id.
        private static readonly string[] kTimeZoneIdCandidates = { kTimeZoneId, "Pacific Standard Time", "US/Pacific", "PST8PDT" };

        private static readonly TimeZoneInfo s_TimeZone = FindTimeZone();

        public static bool UsesFallbackOffset => s_TimeZone == null;

        /// <summary>
        /// Door 1's opening time as Pacific wall-clock time: the date written in the document's "eventStart" (time and offset in the
        /// string are ignored, so "2026-12-01" and "2026-12-01T00:00:00+01:00" both mean December 1st) at <see cref="kUnlockTimeOfDay"/>.
        /// Falls back to <see cref="kEventStart"/> when the document has no valid eventStart.
        /// </summary>
        public static DateTime ResolveEventStart(CalendarDocument document, out bool fromDocument)
        {
            fromDocument = TryParseEventDate(document?.EventStart, out DateTime date);
            return fromDocument ? date + kUnlockTimeOfDay : kEventStart;
        }

        public static bool TryParseEventDate(string value, out DateTime date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(value) || !CalendarService.TryParseDate(value.Trim(), out DateTimeOffset parsed))
                return false;

            // DateTimeOffset.DateTime is the clock time as written (in the string's own offset).
            date = DateTime.SpecifyKind(parsed.DateTime.Date, DateTimeKind.Unspecified);
            return true;
        }

        /// <summary>Door <paramref name="day"/> (1-based) opens at this instant.</summary>
        public static DateTimeOffset GetUnlockTime(DateTime eventStart, int day) => PacificToUtc(eventStart.AddDays(day - 1));

        /// <summary>End of door <paramref name="day"/>'s calendar date, i.e. the following midnight Pacific time.</summary>
        public static DateTimeOffset GetEndOfDate(DateTime eventStart, int day) => PacificToUtc(eventStart.Date.AddDays(day));

        /// <summary>The midnight (Pacific time) that ends the day <paramref name="instant"/> falls on.</summary>
        public static DateTimeOffset GetEndOfPacificDay(DateTimeOffset instant) => PacificToUtc(UtcToPacific(instant).Date.AddDays(1));

        public static DateTime UtcToPacific(DateTimeOffset instant)
        {
            if (s_TimeZone != null)
            {
                try
                {
                    return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(instant.UtcDateTime, s_TimeZone), DateTimeKind.Unspecified);
                }
                catch (Exception)
                {
                }
            }

            return DateTime.SpecifyKind(instant.UtcDateTime + kFallbackUtcOffset, DateTimeKind.Unspecified);
        }

        public static DateTimeOffset PacificToUtc(DateTime pacific)
        {
            DateTime wallClock = DateTime.SpecifyKind(pacific, DateTimeKind.Unspecified);
            if (s_TimeZone != null)
            {
                try
                {
                    // The hour skipped when daylight saving time starts does not exist; use the first valid time after it.
                    if (s_TimeZone.IsInvalidTime(wallClock))
                        wallClock = wallClock.AddHours(1);

                    return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(wallClock, s_TimeZone), TimeSpan.Zero);
                }
                catch (Exception)
                {
                }
            }

            return new DateTimeOffset(wallClock, kFallbackUtcOffset).ToUniversalTime();
        }

        public static string Describe(DateTime eventStart, bool fromDocument)
        {
            string zone = s_TimeZone != null ? $"time zone '{s_TimeZone.Id}'" : $"fixed UTC{kFallbackUtcOffset.TotalHours:+0;-0} fallback";
            return string.Format(
                CultureInfo.InvariantCulture,
                "Door 1 opens {0:yyyy-MM-dd HH:mm:ss} {1} = {2:yyyy-MM-dd HH:mm:ss}Z, door {3} opens {4:yyyy-MM-dd HH:mm:ss}Z ({5}, start date from {6})",
                eventStart,
                kTimeZoneId,
                GetUnlockTime(eventStart, 1).UtcDateTime,
                CalendarService.kDayCount,
                GetUnlockTime(eventStart, CalendarService.kDayCount).UtcDateTime,
                zone,
                fromDocument ? "calendar.json eventStart" : "CalendarSchedule.kEventStart fallback");
        }

        private static TimeZoneInfo FindTimeZone()
        {
            foreach (string id in kTimeZoneIdCandidates)
            {
                try
                {
                    TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(id);
                    if (zone != null && zone.BaseUtcOffset == kFallbackUtcOffset)
                        return zone;
                }
                catch (Exception)
                {
                    // TimeZoneNotFoundException, InvalidTimeZoneException, SecurityException, ... -> try the next id.
                }
            }

            return null;
        }
    }
}
