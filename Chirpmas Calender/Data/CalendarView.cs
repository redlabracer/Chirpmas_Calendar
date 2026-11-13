using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Colossal.UI.Binding;

namespace ChirpmasCalendar.Data
{
    public enum DayState
    {
        Locked,
        Pending,
        Open,
    }

    public sealed class CalendarDayView
    {
        public int Day;
        public DayState State;
        public bool IsToday;
        public DateTimeOffset UnlockAt;
        public string Title;
        public string Creator;
        public string Type;
        public string Description;
        public string ImageUrl;
        public int PdxModId;

        /// <summary>Identifies this door's opening for CalendarChirpSystem's "already announced" list. Only set on the newest open door.</summary>
        public string AnnounceKey;

        /// <summary>Non-blank entries only (filtered once in <see cref="CalendarView.Build"/>).</summary>
        public List<string> AnnouncementChirps = new List<string>();

        /// <summary>Non-blank entries only (filtered once in <see cref="CalendarView.Build"/>).</summary>
        public List<string> DayChirps = new List<string>();

        public DateTimeOffset DayChirpsUntil;

        public bool DayChirpsActive;
    }

    /// What the UI is allowed to see at a given moment. Locked days carry no content, mirroring what the server should do later (never send future days).
    public sealed class CalendarView : IJsonWritable
    {
        public static readonly CalendarView Empty = Build(null, CalendarStatus.Loading, null, false, DateTimeOffset.UtcNow, 0);

        public CalendarStatus Status { get; private set; }

        public string Error { get; private set; }

        public bool Offline { get; private set; }

        public bool EventActive { get; private set; } = true;

        public int Today { get; private set; }

        public bool Simulated { get; private set; }

        public int PackModId { get; private set; }

        public string Announcement { get; private set; }

        public List<CalendarDayView> Days { get; } = new List<CalendarDayView>(CalendarService.kDayCount);

        public List<string> GenericChirps { get; private set; } = new List<string>();

        public string ChirpSender { get; private set; }

        public string StateKey { get; private set; }

        /// <summary>Next moment a door opens or a door's day ends; null when nothing changes anymore or the date is simulated.</summary>
        public DateTimeOffset? NextChangeAt { get; private set; }

        /// <summary><see cref="CalendarService.Version"/> of the data this view was built from.</summary>
        public int DataVersion { get; internal set; } = -1;

        public static CalendarView Build(CalendarDocument document, CalendarStatus status, string error, bool offline, DateTimeOffset utcNow, int simulatedDay)
        {
            var view = new CalendarView
            {
                Status = status,
                Error = error,
                Offline = offline,
                Simulated = simulatedDay > 0,
            };

            // calendar.json's "eventStart" date decides the schedule; CalendarSchedule.kEventStart is only the fallback.
            DateTime eventStart = CalendarSchedule.ResolveEventStart(document, out _);
            DateTimeOffset now = simulatedDay > 0 ? CalendarSchedule.GetUnlockTime(eventStart, simulatedDay).AddHours(12) : utcNow;

            if (document != null)
            {
                view.EventActive = document.EventActive;
                view.PackModId = document.PackModId ?? 0;
                view.Announcement = document.Announcement;
                view.GenericChirps = NonBlank(document.GenericChirps);
                view.ChirpSender = document.ChirpSender;
            }

            Dictionary<int, CalendarDayEntry> entries = document?.Days.ToDictionary(d => d.Day) ?? new Dictionary<int, CalendarDayEntry>();

            for (int day = 1; day <= CalendarService.kDayCount; day++)
            {
                entries.TryGetValue(day, out CalendarDayEntry entry);

                DateTimeOffset unlockAt = CalendarSchedule.GetUnlockTime(eventStart, day);
                if (entry?.UnlockAt != null && CalendarService.TryParseDate(entry.UnlockAt, out DateTimeOffset customUnlock))
                    unlockAt = customUnlock;

                var dayView = new CalendarDayView
                {
                    Day = day,
                    UnlockAt = unlockAt,
                    DayChirpsUntil = DayChirpsEnd(eventStart, day, unlockAt),
                };

                if (simulatedDay <= 0)
                {
                    view.NextChangeAt = Earliest(view.NextChangeAt, unlockAt, now);
                    view.NextChangeAt = Earliest(view.NextChangeAt, dayView.DayChirpsUntil, now);
                }

                if (now < unlockAt)
                {
                    dayView.State = DayState.Locked;
                }
                else
                {
                    view.Today = Math.Max(view.Today, day);
                    dayView.IsToday = now < unlockAt.AddDays(1);

                    if (entry != null)
                    {
                        dayView.Title = entry.Title;
                        dayView.Creator = entry.Creator;
                        dayView.Type = entry.Type;
                    }

                    if (entry != null && entry.Released)
                    {
                        dayView.State = DayState.Open;
                        dayView.Description = entry.Description;
                        dayView.ImageUrl = IsAllowedImageUrl(entry.ImageUrl) ? entry.ImageUrl : null;
                        dayView.PdxModId = entry.PdxModId ?? 0;
                        dayView.AnnouncementChirps = NonBlank(entry.AnnouncementChirps);
                        dayView.DayChirps = NonBlank(entry.DayChirps);
                        dayView.DayChirpsActive = now < dayView.DayChirpsUntil;
                    }
                    else
                    {
                        dayView.State = DayState.Pending;
                    }
                }

                view.Days.Add(dayView);
            }

            // Only the most recently unlocked door counts as today
            foreach (CalendarDayView dayView in view.Days)
                dayView.IsToday &= dayView.Day == view.Today;

            if (view.Today > 0)
            {
                CalendarDayView newest = view.Days[view.Today - 1];
                if (newest.State == DayState.Open)
                    newest.AnnounceKey = newest.UnlockAt.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
            }

            var key = new StringBuilder(64);
            key.Append(view.Today).Append('|').Append(view.EventActive ? 1 : 0).Append(view.Simulated ? 'S' : 'R').Append('|');
            foreach (CalendarDayView dayView in view.Days)
                key.Append((int)dayView.State).Append(dayView.IsToday ? 'T' : '-');
            view.StateKey = key.ToString();

            return view;
        }

        // Door N belongs to the Nth event date; its day ends at the following midnight Pacific time. A custom unlockAt later than that still gets the rest of its own Pacific day.
        private static DateTimeOffset DayChirpsEnd(DateTime eventStart, int day, DateTimeOffset unlockAt)
        {
            DateTimeOffset endOfDoorDate = CalendarSchedule.GetEndOfDate(eventStart, day);
            DateTimeOffset endOfUnlockDay = CalendarSchedule.GetEndOfPacificDay(unlockAt);
            return endOfDoorDate > endOfUnlockDay ? endOfDoorDate : endOfUnlockDay;
        }

        private static DateTimeOffset? Earliest(DateTimeOffset? current, DateTimeOffset candidate, DateTimeOffset now)
        {
            if (candidate <= now)
                return current;

            return current.HasValue && current.Value <= candidate ? current : candidate;
        }

        private static List<string> NonBlank(List<string> chirps)
        {
            var result = new List<string>(chirps?.Count ?? 0);
            if (chirps != null)
            {
                foreach (string chirp in chirps)
                {
                    if (!string.IsNullOrWhiteSpace(chirp))
                        result.Add(chirp);
                }
            }

            return result;
        }

        private static bool IsAllowedImageUrl(string url)
        {
            return !string.IsNullOrEmpty(url)
                && (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("coui://", StringComparison.OrdinalIgnoreCase));
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin("ChirpmasCalendar.Calendar");
            writer.PropertyName("status");
            writer.Write(Status.ToString().ToLowerInvariant());
            writer.PropertyName("error");
            WriteNullable(writer, Error);
            writer.PropertyName("offline");
            writer.Write(Offline);
            writer.PropertyName("eventActive");
            writer.Write(EventActive);
            writer.PropertyName("today");
            writer.Write(Today);
            writer.PropertyName("simulated");
            writer.Write(Simulated);
            writer.PropertyName("packModId");
            writer.Write(PackModId);
            writer.PropertyName("announcement");
            WriteNullable(writer, Announcement);
            writer.PropertyName("days");
            writer.ArrayBegin((uint)Days.Count);
            foreach (CalendarDayView day in Days)
                WriteDay(writer, day);
            writer.ArrayEnd();
            writer.TypeEnd();
        }

        private static void WriteDay(IJsonWriter writer, CalendarDayView day)
        {
            writer.TypeBegin("ChirpmasCalendar.Day");
            writer.PropertyName("day");
            writer.Write(day.Day);
            writer.PropertyName("state");
            writer.Write(day.State.ToString().ToLowerInvariant());
            writer.PropertyName("isToday");
            writer.Write(day.IsToday);
            writer.PropertyName("title");
            WriteNullable(writer, day.Title);
            writer.PropertyName("creator");
            WriteNullable(writer, day.Creator);
            writer.PropertyName("type");
            WriteNullable(writer, day.Type);
            writer.PropertyName("description");
            WriteNullable(writer, day.Description);
            writer.PropertyName("imageUrl");
            WriteNullable(writer, day.ImageUrl);
            writer.PropertyName("pdxModId");
            writer.Write(day.PdxModId);
            writer.TypeEnd();
        }

        private static void WriteNullable(IJsonWriter writer, string value)
        {
            if (value == null)
                writer.WriteNull();
            else
                writer.Write(value);
        }
    }
}
