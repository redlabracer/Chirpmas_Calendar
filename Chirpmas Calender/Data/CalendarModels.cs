using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChirpmasCalendar.Data
{
    public sealed class CalendarDocument
    {
        public int SchemaVersion { get; set; }

        public bool EventActive { get; set; } = true;

        /// First event date, e.g. "2026-12-01". Only the date counts: door 1 opens on it at 00:00:05 America/Los_Angeles, door N
        /// N-1 days later (see <see cref="CalendarSchedule.ResolveEventStart"/>); a day can still set its own UnlockAt.
        public string EventStart { get; set; }

        public int? PackModId { get; set; }

        public string Announcement { get; set; }

        public List<string> GenericChirps { get; set; } = new List<string>();

        public string ChirpSender { get; set; }

        public List<CalendarDayEntry> Days { get; set; } = new List<CalendarDayEntry>();
    }

    public sealed class CalendarDayEntry
    {
        public int Day { get; set; }

        public string UnlockAt { get; set; }

        public bool Released { get; set; }

        public string Title { get; set; }

        public string Creator { get; set; }

        public string Type { get; set; }

        public int? PdxModId { get; set; }

        public string ImageUrl { get; set; }

        public string Description { get; set; }

        public List<string> AnnouncementChirps { get; set; } = new List<string>();

        [JsonProperty("chirps")]
        public List<string> LegacyChirps { get; set; }

        /// <summary>
        /// Optional extra chirps by the creator. Once the door is open they rotate every <see cref="Setting.DayChirpMinutes"/>
        /// until the end of the door's calendar day in Pacific time, see <see cref="CalendarSchedule"/>.
        /// </summary>
        public List<string> DayChirps { get; set; } = new List<string>();
    }
}
