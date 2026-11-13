using System;
using System.Collections.Generic;
using System.Globalization;
using ChirpmasCalendar.Data;
using ChirpmasCalendar.Diagnostics;
using ChirpmasCalendar.Integrations;
using Colossal.Serialization.Entities;
using Game;
using Game.SceneFlow;

namespace ChirpmasCalendar.Systems
{
    /// <summary>
    /// Posts calendar chirps into the running city through Custom Chirps (if installed):
    /// <list type="bullet">
    /// <item>once per door, when the newest door is open and released: its "announcementChirps" (the first one large, with the cover image);</item>
    /// <item>every <see cref="Setting.DayChirpMinutes"/>: the next "dayChirps" entry of the open doors whose day (Pacific time, see <see cref="CalendarSchedule"/>) is still running;</item>
    /// <item>every <see cref="Setting.GenericChirpMinutes"/>: a random entry of "genericChirps".</item>
    /// </list>
    /// Runs in the simulation phase, so nothing is posted while the game is paused.
    /// </summary>
    public partial class CalendarChirpSystem : GameSystemBase
    {
        private const float kFirstChirpDelay = 20f;
        private const float kChirpSpacing = 30f;
        private const int kMaxRememberedDoors = 64;

        private const float kMaxActiveStep = 10f;

        private static volatile bool s_ResetRequested;

        private readonly List<PendingChirp> m_Queue = new List<PendingChirp>();
        private readonly HashSet<string> m_AnnouncedThisSession = new HashSet<string>();
        private readonly System.Random m_Random = new System.Random();

        // Next dayChirps index per door starts at a random entry so restarting the game doesn't always replay the first ones.
        private readonly Dictionary<int, int> m_DayChirpCursor = new Dictionary<int, int>();

        private bool m_InGame;
        private bool m_LoggedMissing;
        private float m_ActiveTime;
        private float m_LastRealTime = -1f;
        private float m_ReadyAt = -1f;
        private float m_NextAnnouncementAt;
        private float m_NextDailyAt;
        private float m_NextGenericAt;
        private int m_LastGenericIndex = -1;
        private int m_LastDayChirpDoor;

        public static void RequestReset() => s_ResetRequested = true;

        internal static void CancelReset() => s_ResetRequested = false;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_InGame = mode.IsGame();
            if (m_InGame)
                CustomChirpsBridge.RetryIfMissing();

            // Doors whose first chirp was never posted are announced again in the next city.
            foreach (PendingChirp chirp in m_Queue)
            {
                if (chirp.DoorKey != null)
                    m_AnnouncedThisSession.Remove(chirp.DoorKey);
            }

            m_Queue.Clear();
            m_ReadyAt = -1f;
            m_LastRealTime = -1f;
        }

        protected override void OnUpdate()
        {
            using var profile = ModProfiler.ChirpUpdate.Auto();
            Setting settings = Mod.Settings;
            CalendarService service = Mod.Calendar;
            if (!m_InGame || settings == null || service == null || !settings.EnableChirps)
                return;

            if (!CustomChirpsBridge.IsAvailable)
            {
                if (!m_LoggedMissing)
                {
                    m_LoggedMissing = true;
                    Mod.Log.Info($"Custom Chirps (Paradox Mods {CustomChirpsBridge.kPdxModId}) is not installed; calendar chirps are disabled.");
                }

                return;
            }

            float realTime = UnityEngine.Time.unscaledTime;
            float gap = realTime - m_LastRealTime;
            if (m_LastRealTime >= 0f && gap > 0f && gap <= kMaxActiveStep)
                m_ActiveTime += gap;
            m_LastRealTime = realTime;

            float now = m_ActiveTime;
            if (m_ReadyAt < 0f)
            {
                m_ReadyAt = now + kFirstChirpDelay;
                m_NextAnnouncementAt = m_ReadyAt;
                m_NextDailyAt = m_ReadyAt + MinutesToSeconds(settings.DayChirpMinutes);
                m_NextGenericAt = m_ReadyAt + MinutesToSeconds(settings.GenericChirpMinutes);
            }

            if (s_ResetRequested)
            {
                s_ResetRequested = false;
                m_AnnouncedThisSession.Clear();
                m_Queue.Clear();
                m_DayChirpCursor.Clear();
                settings.AnnouncedDoors = string.Empty;
                settings.ApplyAndSave();
                m_NextAnnouncementAt = now;
                m_NextDailyAt = now + kChirpSpacing;
                m_NextGenericAt = now + kChirpSpacing;
                Mod.Log.Info("Chirp history reset");
            }

            if (now < m_ReadyAt)
                return;

            CalendarView view = service.GetView(settings.EffectiveSimulatedDay);
            if (!view.EventActive || view.Today < 1)
                return;

            string sender = string.IsNullOrWhiteSpace(view.ChirpSender)
                ? Translate(ChirpKeys.Sender, "Chirpmas Calendar")
                : view.ChirpSender;

            QueueNewestDoor(view, settings);

            if (m_Queue.Count > 0)
            {
                if (now >= m_NextAnnouncementAt)
                {
                    PendingChirp next = m_Queue[0];
                    m_Queue.RemoveAt(0);
                    Post(next, sender);
                    if (next.DoorKey != null)
                        RememberAnnounced(settings, next.DoorKey);

                    m_NextAnnouncementAt = now + kChirpSpacing;
                    m_NextDailyAt = Math.Max(m_NextDailyAt, now + kChirpSpacing);
                    m_NextGenericAt = Math.Max(m_NextGenericAt, now + kChirpSpacing);
                }

                return;
            }

            if (IsDue(ref m_NextDailyAt, MinutesToSeconds(settings.DayChirpMinutes), now))
            {
                PendingChirp? daily = PickDayChirp(view);
                if (daily.HasValue)
                {
                    Post(daily.Value, sender);
                    m_NextGenericAt = Math.Max(m_NextGenericAt, now + kChirpSpacing);
                    return;
                }
            }

            if (IsDue(ref m_NextGenericAt, MinutesToSeconds(settings.GenericChirpMinutes), now))
            {
                PendingChirp? generic = PickGenericChirp(view);
                if (generic.HasValue)
                {
                    Post(generic.Value, sender);
                    m_NextDailyAt = Math.Max(m_NextDailyAt, now + kChirpSpacing);
                }
            }
        }

        private void QueueNewestDoor(CalendarView view, Setting settings)
        {
            using var profile = ModProfiler.ChirpQueueDoor.Auto();
            CalendarDayView day = view.Days[view.Today - 1];
            string key = day.AnnounceKey;
            if (day.State != DayState.Open || key == null || !m_AnnouncedThisSession.Add(key))
                return;

            // Simulated days only count for this session so testing doesn't use up the real announcements.
            if (!view.Simulated && SplitAnnounced(settings.AnnouncedDoors).Contains(key))
                return;

            List<string> texts = day.AnnouncementChirps;
            if (texts.Count == 0)
            {
                texts = new List<string>
                {
                    string.IsNullOrWhiteSpace(day.Creator)
                        ? Translate(ChirpKeys.DoorOpen, "Door {DAY} of the Chirpmas calendar is open: {TITLE}! #Chirpmas")
                        : Translate(ChirpKeys.DoorOpenWithCreator, "Door {DAY} of the Chirpmas calendar is open: {TITLE} by {CREATOR}! #Chirpmas"),
                };
            }

            // The door is stored as announced only when its first chirp is actually posted (see OnUpdate), so leaving the
            // city before that doesn't lose the announcement.
            for (int i = 0; i < texts.Count; i++)
            {
                bool first = i == 0;
                m_Queue.Add(new PendingChirp(
                    FillPlaceholders(texts[i], day),
                    first ? day.ImageUrl : null,
                    first && !view.Simulated ? key : null));
            }

            Mod.Log.Info($"Queued {texts.Count} chirp(s) for door {day.Day}{(view.Simulated ? " (simulated)" : string.Empty)}");
        }

        private static void RememberAnnounced(Setting settings, string key)
        {
            List<string> stored = SplitAnnounced(settings.AnnouncedDoors);
            if (stored.Contains(key))
                return;

            stored.Add(key);
            if (stored.Count > kMaxRememberedDoors)
                stored.RemoveRange(0, stored.Count - kMaxRememberedDoors);

            settings.AnnouncedDoors = string.Join(",", stored);
            settings.ApplyAndSave();
        }

        private static void Post(PendingChirp chirp, string sender)
        {
            using var profile = ModProfiler.ChirpPost.Auto();
            try
            {
                if (chirp.ImageUrl != null)
                    CustomChirpsBridge.PostChirpWithImage(chirp.Text, chirp.ImageUrl, sender);
                else
                    CustomChirpsBridge.PostChirp(chirp.Text, sender);
            }
            catch (Exception e)
            {
                Mod.Log.Warn(e, "Could not post chirp");
            }
        }

        // Advances nextAt when due an interval of 0 disables the timer.
        private static bool IsDue(ref float nextAt, float interval, float now)
        {
            if (interval <= 0f)
                return false;

            // The slider was lowered: don't wait out the old, longer interval.
            if (nextAt > now + interval)
                nextAt = now + interval;

            if (now < nextAt)
                return false;

            nextAt = now + interval;
            return true;
        }

        // Doors take turns (several can be active while an older door's Pacific-time day is still running) within a door the
        // chirps rotate in their listed order
        private PendingChirp? PickDayChirp(CalendarView view)
        {
            CalendarDayView door = null;
            List<string> texts = null;
            CalendarDayView firstDoor = null;
            List<string> firstTexts = null;
            foreach (CalendarDayView day in view.Days)
            {
                if (!day.DayChirpsActive || day.State != DayState.Open || day.DayChirps.Count == 0)
                    continue;

                List<string> usable = day.DayChirps;

                if (firstDoor == null)
                {
                    firstDoor = day;
                    firstTexts = usable;
                }

                if (day.Day > m_LastDayChirpDoor)
                {
                    door = day;
                    texts = usable;
                    break;
                }
            }

            if (door == null)
            {
                door = firstDoor;
                texts = firstTexts;
            }

            if (door == null)
                return null;

            if (!m_DayChirpCursor.TryGetValue(door.Day, out int cursor))
                cursor = m_Random.Next(texts.Count);

            cursor %= texts.Count;
            m_DayChirpCursor[door.Day] = (cursor + 1) % texts.Count;
            m_LastDayChirpDoor = door.Day;
            return new PendingChirp(FillPlaceholders(texts[cursor], door), null);
        }

        private PendingChirp? PickGenericChirp(CalendarView view)
        {
            List<string> usable = view.GenericChirps;
            if (usable == null || usable.Count == 0)
                return null;

            int index = m_Random.Next(usable.Count);
            if (usable.Count > 1 && index == m_LastGenericIndex)
                index = (index + 1) % usable.Count;

            m_LastGenericIndex = index;
            return new PendingChirp(usable[index], null);
        }

        private static float MinutesToSeconds(int minutes) => Math.Max(0, minutes) * 60f;

        private static List<string> SplitAnnounced(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new List<string>()
                : new List<string>(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        // Supported in chirp texts: {DAY}, {TITLE}, {CREATOR}, {TYPE}
        private static string FillPlaceholders(string text, CalendarDayView day)
        {
            return text
                .Replace("{DAY}", day.Day.ToString(CultureInfo.InvariantCulture))
                .Replace("{TITLE}", day.Title ?? string.Empty)
                .Replace("{CREATOR}", day.Creator ?? string.Empty)
                .Replace("{TYPE}", day.Type ?? string.Empty);
        }

        private static string Translate(string key, string fallback)
        {
            try
            {
                if (GameManager.instance?.localizationManager?.activeDictionary != null
                    && GameManager.instance.localizationManager.activeDictionary.TryGetValue(key, out string value))
                    return value;
            }
            catch (Exception e)
            {
                Mod.Log.Debug($"Could not translate '{key}': {e.Message}");
            }

            return fallback;
        }

        private readonly struct PendingChirp
        {
            public PendingChirp(string text, string imageUrl, string doorKey = null)
            {
                Text = text;
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl;
                DoorKey = doorKey;
            }

            public string Text { get; }

            public string ImageUrl { get; }

            /// <summary>Set on a real (not simulated) door's first announcement chirp: posting it marks the door as announced.</summary>
            public string DoorKey { get; }
        }
    }
}
