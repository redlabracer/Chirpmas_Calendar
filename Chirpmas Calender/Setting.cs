using System;
using System.IO;
using ChirpmasCalendar.Data;
using ChirpmasCalendar.Diagnostics;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using UnityEngine;

namespace ChirpmasCalendar
{
    [FileLocation("ModsSettings/" + Mod.Id + "/" + Mod.Id)]
    [SettingsUIGroupOrder(kGeneralGroup, kChirpsGroup, kDataGroup, kDebugGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kChirpsGroup, kDataGroup, kDebugGroup)]
    public class Setting : ModSetting
    {
        // Master switch for the testing/debug options (simulated day, chirp reset, profiler, calendar source overrides).
        // false = they are hidden in the Options UI and their saved values are ignored, so the calendar strictly follows the real
        // Pacific date (see CalendarSchedule) and the official server. Tied to the build configuration so a Release build can
        // never ship with them enabled.
#if DEBUG
        public const bool EnableDebugSettings = true;
#else
        public const bool EnableDebugSettings = false;
#endif

        public const string kSection = "Main";

        public const string kGeneralGroup = "General";
        public const string kChirpsGroup = "Chirps";
        public const string kDataGroup = "Data";
        public const string kDebugGroup = "Debug";

        private bool m_EnableProfiling;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // Non-const wrapper so code using it doesn't get "unreachable code" warnings.
        public static bool DebugSettingsEnabled => EnableDebugSettings;

        /// <summary>Simulated event day (0 = real date). Always 0 unless <see cref="EnableDebugSettings"/> is on.</summary>
        [SettingsUIHidden]
        public int EffectiveSimulatedDay => DebugSettingsEnabled ? Mathf.Clamp(SimulatedDay, 0, CalendarService.kDayCount + 1) : 0;

        [SettingsUIHidden]
        public bool EffectiveUseLocalFile => DebugSettingsEnabled && UseLocalFile;

        [SettingsUIHidden]
        public string EffectiveCalendarUrl => DebugSettingsEnabled && !string.IsNullOrWhiteSpace(CalendarUrlOverride) ? CalendarUrlOverride.Trim() : Mod.kDefaultCalendarUrl;

        // Checked every frame by the Options UI through [SettingsUIHideByCondition].
        private static bool HideDebugSettings() => !EnableDebugSettings;

        [SettingsUISection(kSection, kGeneralGroup)]
        public bool ShowButton { get; set; }

        [SettingsUISection(kSection, kChirpsGroup)]
        public bool EnableChirps { get; set; }

        [SettingsUISlider(min = 0, max = 60, step = 5, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kChirpsGroup)]
        public int GenericChirpMinutes { get; set; }

        [SettingsUISlider(min = 0, max = 60, step = 5, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kChirpsGroup)]
        public int DayChirpMinutes { get; set; }

        [SettingsUIHidden]
        public string AnnouncedDoors { get; set; }

        /// <summary>The welcome pop-up was shown once (first click on the Chirpmas button); it can be reopened from the calendar.</summary>
        [SettingsUIHidden]
        public bool WelcomeSeen { get; set; }

        /// <summary>AnnounceKey of the newest door the player has looked at; the button badge stays hidden until a newer door opens.</summary>
        [SettingsUIHidden]
        public string SeenDoor { get; set; }

        [SettingsUITextInput]
        [SettingsUISection(kSection, kDataGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public string CalendarUrlOverride { get; set; }

        [SettingsUISection(kSection, kDataGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool UseLocalFile { get; set; }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDataGroup)]
        public bool ReloadCalendar
        {
            set { Mod.Calendar?.Reload(); }
        }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDataGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool OpenDataFolder
        {
            set
            {
                if (!DebugSettingsEnabled)
                    return;

                try
                {
                    LocalFileCalendarSource local = Mod.Calendar?.Source as LocalFileCalendarSource
                        ?? (Mod.Calendar?.Source as RemoteCalendarSource)?.Local;
                    string folder = local != null
                        ? local.EnsureEditableCopy()
                        : Directory.CreateDirectory(LocalFileCalendarSource.UserDirectory).FullName;
                    Application.OpenURL(new Uri(folder + Path.DirectorySeparatorChar).AbsoluteUri);
                }
                catch (Exception e)
                {
                    Mod.Log.Error(e, "Could not open the calendar data folder");
                }
            }
        }

        [SettingsUISlider(min = 0, max = 25, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public int SimulatedDay { get; set; }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool ResendChirps
        {
            set
            {
                if (DebugSettingsEnabled)
                    Systems.CalendarChirpSystem.RequestReset();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool ResetWelcome
        {
            set
            {
                if (!DebugSettingsEnabled)
                    return;

                WelcomeSeen = false;
                ApplyAndSave();
            }
        }

        // Collects timings of the mod's profiler points and writes a report to the mod log every minute.
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool EnableProfiling
        {
            get => m_EnableProfiling;
            set
            {
                m_EnableProfiling = value;

                if (ReferenceEquals(Mod.Settings, this))
                    ModProfiler.Enabled = value && DebugSettingsEnabled;
            }
        }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool LogProfilerReport
        {
            set
            {
                if (DebugSettingsEnabled)
                    ModProfiler.LogReport();
            }
        }

        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(HideDebugSettings))]
        public bool ResetProfiler
        {
            set
            {
                if (!DebugSettingsEnabled)
                    return;

                ModProfiler.Reset();
                Mod.Log.Info("Profiler statistics reset");
            }
        }

        public override void ApplyAndSave()
        {
            using var profile = ModProfiler.SettingsSave.Auto();
            base.ApplyAndSave();
        }

        public override void SetDefaults()
        {
            ShowButton = true;
            EnableChirps = true;
            GenericChirpMinutes = 15;
            DayChirpMinutes = 10;
            AnnouncedDoors = string.Empty;
            WelcomeSeen = false;
            SeenDoor = string.Empty;
            CalendarUrlOverride = string.Empty;
            UseLocalFile = false;
            SimulatedDay = 0;
            EnableProfiling = false;
        }
    }
}
