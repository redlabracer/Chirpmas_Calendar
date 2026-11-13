using System.Collections.Generic;
using Colossal;

namespace ChirpmasCalendar
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Countdown to Chirpmas" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kChirpsGroup), "Chirps" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDataGroup), "Calendar data" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDebugGroup), "Debug" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ShowButton)), "Show calendar button" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ShowButton)), "Show the Chirpmas calendar button in the top-left corner while playing a city." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableChirps)), "Chirpmas chirps" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableChirps)), "Post a chirp when a new door opens and festive chirps while you play. Requires the \"Custom Chirps\" mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GenericChirpMinutes)), "Minutes between festive chirps" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GenericChirpMinutes)), "Real-time minutes between the generic winter chirps. 0 = off." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DayChirpMinutes)), "Minutes between daily chirps" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DayChirpMinutes)), "Real-time minutes between the creators' chirps of the current door. They rotate from the moment the door opens until the end of that day (Pacific time, UTC-8). 0 = off." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResendChirps)), "Send chirps again" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResendChirps)), "Testing only. Forgets which doors were already announced, so the newest door's chirps are posted again in the running city." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetWelcome)), "Show welcome again" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetWelcome)), "Testing only. The welcome pop-up appears again the next time the Chirpmas button is pressed." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableProfiling)), "Performance profiling" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableProfiling)), "For performance analysis. Measures how long the mod's work takes (calls, average and max time) and writes a report to the mod log (Logs/ChirpmasCalendar.log) every minute. The profiler markers are also visible in the Unity Profiler when the game runs as a development build." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogProfilerReport)), "Write profiler report" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogProfilerReport)), "Writes the current profiler statistics to the mod log now." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetProfiler)), "Reset profiler" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetProfiler)), "Clears the collected profiler statistics, e.g. before measuring a specific situation." },

                { ChirpKeys.Sender, "Chirpmas Calendar" },
                { ChirpKeys.DoorOpen, "Door {DAY} of the Chirpmas calendar is open: {TITLE}! #Chirpmas" },
                { ChirpKeys.DoorOpenWithCreator, "Door {DAY} of the Chirpmas calendar is open: {TITLE} by {CREATOR}! #Chirpmas" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CalendarUrlOverride)), "Calendar URL (override)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CalendarUrlOverride)), "Testing only. Leave empty to use the official Chirpmas server. Must start with https://. Press \"Reload calendar\" after changing it." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UseLocalFile)), "Use local calendar file" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UseLocalFile)), "For creators/testing. Ignore the server and read calendar.json from the data folder instead. Press \"Reload calendar\" after changing it." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReloadCalendar)), "Reload calendar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReloadCalendar)), "Download the calendar again (or re-read the local file)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDataFolder)), "Open data folder" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDataFolder)), "Opens ModsData/ChirpmasCalendar. It holds the last downloaded calendar and, with \"Use local calendar file\", your own calendar.json." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SimulatedDay)), "Simulate event day" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SimulatedDay)), "Testing only. 0 uses the real date; 1-24 pretends it is that day of the event; 25 is after the event." },

                { UIKeys.Title, "Countdown to Chirpmas" },
                { UIKeys.Subtitle, "a community collaboration" },
                { UIKeys.ButtonTooltip, "Chirpmas Advent Calendar" },
                { UIKeys.Loading, "Loading the calendar…" },
                { UIKeys.LoadError, "The calendar could not be loaded." },
                { UIKeys.Locked, "This door is still locked. Come back on December {DAY}!" },
                { UIKeys.LockedTitle, "Door locked" },
                { UIKeys.Pending, "This gift is still being wrapped. Check back later today!" },
                { UIKeys.PendingTitle, "Coming soon" },
                { UIKeys.OpenOnPdx, "Open on Paradox Mods" },
                { UIKeys.SubscribePack, "Get the whole collection" },
                { UIKeys.Back, "Back to calendar" },
                { UIKeys.Creator, "Creator" },
                { UIKeys.Type, "Type" },
                { UIKeys.Day, "December {DAY}" },
                { UIKeys.RestartHint, "New assets are loaded the next time you start the game." },
                { UIKeys.SimulatedHint, "Simulated date active (Options > Countdown to Chirpmas)" },
                { UIKeys.Reload, "Try again" },
                { UIKeys.OfflineHint, "Offline: showing the last saved calendar." },

                { UIKeys.WelcomeTitle, "Welcome to Countdown to Chirpmas!" },
                { UIKeys.WelcomeIntro, "Countdown to Chirpmas is a community collaboration. Creators from the Cities: Skylines II community have prepared an advent calendar full of free festive content. Christmas assets, maps and maybe code mods. One door for every day from December 1st to Christmas Eve." },
                { UIKeys.WelcomeHowTitle, "How does it work?" },
                { UIKeys.WelcomeStep1, "A new door opens every day at midnight Pacific time (09:00 CET). A red dot on the Chirpmas button (top left of the screen) tells you when one is waiting." },
                { UIKeys.WelcomeStep2, "Click an open door to see the day's gift and who made it, then subscribe to your favourites on Paradox Mods." },
                { UIKeys.WelcomeStep3, "With the \"Custom Chirps\" mod installed, Chirper announces every new door and shares the creators' festive chirps." },
                { UIKeys.WelcomeCreatorsTitle, "Who's involved?" },
                { UIKeys.WelcomeDisclaimer, "This community-made project is not affiliated, endorsed, or sponsored by Paradox Interactive or Iceflake Studios." },
                { UIKeys.WelcomeStart, "Open the calendar" },
                { UIKeys.About, "About Chirpmas" },
            };
        }

        public void Unload()
        {
        }
    }

    public static class UIKeys
    {
        private const string kPrefix = Mod.Id + ".UI";

        public const string Title = kPrefix + "[Title]";
        public const string Subtitle = kPrefix + "[Subtitle]";
        public const string ButtonTooltip = kPrefix + "[ButtonTooltip]";
        public const string Loading = kPrefix + "[Loading]";
        public const string LoadError = kPrefix + "[LoadError]";
        public const string Locked = kPrefix + "[Locked]";
        public const string LockedTitle = kPrefix + "[LockedTitle]";
        public const string Pending = kPrefix + "[Pending]";
        public const string PendingTitle = kPrefix + "[PendingTitle]";
        public const string OpenOnPdx = kPrefix + "[OpenOnPdx]";
        public const string SubscribePack = kPrefix + "[SubscribePack]";
        public const string Back = kPrefix + "[Back]";
        public const string Creator = kPrefix + "[Creator]";
        public const string Type = kPrefix + "[Type]";
        public const string Day = kPrefix + "[Day]";
        public const string RestartHint = kPrefix + "[RestartHint]";
        public const string SimulatedHint = kPrefix + "[SimulatedHint]";
        public const string Reload = kPrefix + "[Reload]";
        public const string OfflineHint = kPrefix + "[OfflineHint]";
        public const string WelcomeTitle = kPrefix + "[WelcomeTitle]";
        public const string WelcomeIntro = kPrefix + "[WelcomeIntro]";
        public const string WelcomeHowTitle = kPrefix + "[WelcomeHowTitle]";
        public const string WelcomeStep1 = kPrefix + "[WelcomeStep1]";
        public const string WelcomeStep2 = kPrefix + "[WelcomeStep2]";
        public const string WelcomeStep3 = kPrefix + "[WelcomeStep3]";
        public const string WelcomeCreatorsTitle = kPrefix + "[WelcomeCreatorsTitle]";
        public const string WelcomeDisclaimer = kPrefix + "[WelcomeDisclaimer]";
        public const string WelcomeStart = kPrefix + "[WelcomeStart]";
        public const string About = kPrefix + "[About]";
    }

    public static class ChirpKeys
    {
        private const string kPrefix = Mod.Id + ".Chirp";

        public const string Sender = kPrefix + "[Sender]";
        public const string DoorOpen = kPrefix + "[DoorOpen]";
        public const string DoorOpenWithCreator = kPrefix + "[DoorOpenWithCreator]";
    }
}
