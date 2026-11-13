using System;
using System.Collections.Generic;
using ChirpmasCalendar.Diagnostics;
using Newtonsoft.Json;

namespace ChirpmasCalendar.Data
{
    public static class CalendarParser
    {
        /// <summary>Version 2 is what the server's admin form writes; its layout is identical to version 1.</summary>
        public const int kSupportedSchemaVersion = 2;
        public const int kMaxJsonLength = 512 * 1024;

        private static readonly JsonSerializerSettings s_Settings = new JsonSerializerSettings
        {
            // Keep ISO dates as raw strings; we parse them ourselves with their offsets intact.
            DateParseHandling = DateParseHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static CalendarDocument Parse(string json)
        {
            using var profile = ModProfiler.Parse.Auto();
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("calendar.json is empty");
            if (json.Length > kMaxJsonLength)
                throw new FormatException($"calendar.json is larger than {kMaxJsonLength / 1024} KB");

            CalendarDocument document = JsonConvert.DeserializeObject<CalendarDocument>(json, s_Settings)
                ?? throw new FormatException("calendar.json has no content");

            if (document.SchemaVersion < 1 || document.SchemaVersion > kSupportedSchemaVersion)
                throw new FormatException($"Unsupported schemaVersion {document.SchemaVersion} (this mod version understands up to {kSupportedSchemaVersion}). Please update the mod.");

            if (document.EventStart != null && !CalendarSchedule.TryParseEventDate(document.EventStart, out _))
                throw new FormatException($"eventStart '{document.EventStart}' is not a valid ISO 8601 date");

            document.Days = document.Days ?? new List<CalendarDayEntry>();
            document.GenericChirps = document.GenericChirps ?? new List<string>();

            var seen = new HashSet<int>();
            foreach (CalendarDayEntry day in document.Days)
            {
                if (day == null)
                    throw new FormatException("days contains an empty entry");
                if (day.Day < 1 || day.Day > CalendarService.kDayCount)
                    throw new FormatException($"day {day.Day} is outside 1-{CalendarService.kDayCount}");
                if (!seen.Add(day.Day))
                    throw new FormatException($"day {day.Day} appears more than once");
                if (day.UnlockAt != null && !CalendarService.TryParseDate(day.UnlockAt, out _))
                    throw new FormatException($"day {day.Day}: unlockAt '{day.UnlockAt}' is not a valid ISO 8601 date");
                if (day.PdxModId.HasValue && day.PdxModId.Value < 0)
                    throw new FormatException($"day {day.Day}: pdxModId must be a positive number");

                if ((day.AnnouncementChirps == null || day.AnnouncementChirps.Count == 0) && day.LegacyChirps != null)
                    day.AnnouncementChirps = day.LegacyChirps;

                day.AnnouncementChirps = day.AnnouncementChirps ?? new List<string>();
                day.LegacyChirps = null;
                day.DayChirps = day.DayChirps ?? new List<string>();
            }

            return document;
        }
    }
}
