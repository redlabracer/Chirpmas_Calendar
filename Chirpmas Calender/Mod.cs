using System.IO;
using ChirpmasCalendar.Data;
using ChirpmasCalendar.Diagnostics;
using ChirpmasCalendar.Systems;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace ChirpmasCalendar
{
    public class Mod : IMod
    {
        public const string Id = "ChirpmasCalendar";

        // Public calendar JSON on the VPS by winter gets edited through https://winterxo.com/chirpmas/admin/.
        // Downloaded on mod load and via "Reload calendar"; its HTTP Date header is the trusted clock for opening doors.
        public const string kDefaultCalendarUrl = "https://winterxo.com/chirpmas/chirpmas.json";

        public static ILog Log { get; } = LogManager.GetLogger(Id).SetShowsErrorsInUI(false);

        public static Setting Settings { get; private set; }

        public static CalendarService Calendar { get; private set; }

        private LocaleEN m_Locale;

        public void OnLoad(UpdateSystem updateSystem)
        {
            using var profile = ModProfiler.ModLoad.Auto();
            Log.Info(nameof(OnLoad));

            string modDirectory = null;
            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                modDirectory = Path.GetDirectoryName(asset.path);
                Log.Info($"Mod directory: {modDirectory}");
            }

            Settings = new Setting(this);
            Settings.RegisterInOptionsUI();
            m_Locale = new LocaleEN(Settings);
            GameManager.instance.localizationManager.AddSource("en-US", m_Locale);
            AssetDatabase.global.LoadSettings(Id, Settings, new Setting(this));

            ModProfiler.Enabled = Setting.DebugSettingsEnabled && Settings.EnableProfiling;

            var local = new LocalFileCalendarSource(modDirectory);
            var remote = new RemoteCalendarSource(local, () => Settings?.EffectiveCalendarUrl, () => Settings?.EffectiveUseLocalFile ?? false);
            Calendar = new CalendarService(remote, new CalendarClock());
            Calendar.Reload();

            if (CalendarSchedule.UsesFallbackOffset)
                Log.Warn($"Time zone '{CalendarSchedule.kTimeZoneId}' is not available on this system; using a fixed UTC-8 offset (no daylight saving time).");
            if (Setting.DebugSettingsEnabled)
                Log.Warn("Debug settings are enabled (Setting.EnableDebugSettings). Do not publish this build.");

            updateSystem.UpdateAt<CalendarUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CalendarChirpSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            if (m_Locale != null)
            {
                GameManager.instance?.localizationManager?.RemoveSource("en-US", m_Locale);
                m_Locale = null;
            }

            if (Settings != null)
            {
                Settings.UnregisterInOptionsUI();
                Settings = null;
            }

            CalendarChirpSystem.CancelReset();
            ModProfiler.Enabled = false;
            Calendar = null;
        }
    }
}
