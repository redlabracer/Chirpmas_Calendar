using ChirpmasCalendar.Data;
using ChirpmasCalendar.Diagnostics;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game;
using Game.UI;
using UnityEngine;

namespace ChirpmasCalendar.Systems
{
    // Bridge between CalendarService and the React UI. Bindings live in the group Mod.Id
    // values isOpen, showButton, welcomeSeen, calendar, newDoor; triggers toggle, setOpen, reload, markWelcomeSeen, modPageOpened
    public partial class CalendarUISystem : UISystemBase
    {
        private static readonly System.TimeSpan kRefreshOnOpenAfter = System.TimeSpan.FromMinutes(5);

        private ValueBinding<bool> m_IsOpen;
        private ValueBinding<bool> m_ShowButton;
        private ValueBinding<bool> m_WelcomeSeen;
        private ValueBinding<bool> m_NewDoor;
        private RawValueBinding m_Calendar;

        private CalendarView m_View = CalendarView.Empty;
        private CalendarView m_LastChecked;
        private string m_NewestDoorKey;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            AddBinding(m_IsOpen = new ValueBinding<bool>(Mod.Id, "isOpen", false));
            AddBinding(m_ShowButton = new ValueBinding<bool>(Mod.Id, "showButton", false));
            AddBinding(m_WelcomeSeen = new ValueBinding<bool>(Mod.Id, "welcomeSeen", Mod.Settings?.WelcomeSeen ?? false));
            AddBinding(m_Calendar = new RawValueBinding(Mod.Id, "calendar", WriteCalendar));
            // Badge on the always-visible button; persisted via Setting.SeenDoor so it survives game restarts.
            AddBinding(m_NewDoor = new ValueBinding<bool>(Mod.Id, "newDoor", false));

            AddBinding(new TriggerBinding(Mod.Id, "toggle", () => SetOpen(!m_IsOpen.value)));
            AddBinding(new TriggerBinding<bool>(Mod.Id, "setOpen", SetOpen));
            AddBinding(new TriggerBinding(Mod.Id, "reload", () => Mod.Calendar?.Reload()));
            AddBinding(new TriggerBinding(Mod.Id, "markWelcomeSeen", MarkWelcomeSeen));
            AddBinding(new TriggerBinding<int, bool, double>(Mod.Id, "modPageOpened", OnModPageOpened));
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // Don't carry an open panel over into the next city.
            m_IsOpen.Update(false);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            ModProfiler.Update();
            using var profile = ModProfiler.UIUpdate.Auto();

            Setting settings = Mod.Settings;
            CalendarService service = Mod.Calendar;
            if (settings == null || service == null)
                return;

            // Cached by the service: the clock is only re-read every few seconds or right when the next door opens.
            CalendarView view = service.GetView(settings.EffectiveSimulatedDay);
            if (!ReferenceEquals(view, m_LastChecked))
            {
                m_LastChecked = view;
                if (view.DataVersion != m_View.DataVersion || view.StateKey != m_View.StateKey)
                {
                    if (!view.Simulated && !m_View.Simulated && view.DataVersion == m_View.DataVersion && view.Today > m_View.Today)
                        Mod.Log.Info($"Door {view.Today} unlocked ({CalendarSchedule.UtcToPacific(service.Clock.UtcNow):yyyy-MM-dd HH:mm:ss} {CalendarSchedule.kTimeZoneId})");

                    m_View = view;
                    m_NewestDoorKey = view.Today > 0 && view.Today <= view.Days.Count ? view.Days[view.Today - 1].AnnounceKey : null;
                    m_Calendar.Update();
                }
            }

            bool showButton = settings.ShowButton && m_View.EventActive;
            m_ShowButton.Update(showButton);
            m_WelcomeSeen.Update(settings.WelcomeSeen);
            if (!showButton && m_IsOpen.value)
                m_IsOpen.Update(false);

            UpdateNewDoor(settings);
        }

        private void UpdateNewDoor(Setting settings)
        {
            string key = m_NewestDoorKey;
            if (key == null)
            {
                m_NewDoor.Update(false);
                return;
            }

            if (m_IsOpen.value && key != settings.SeenDoor)
            {
                settings.SeenDoor = key;
                settings.ApplyAndSave();
            }

            m_NewDoor.Update(key != settings.SeenDoor);
        }

        private void WriteCalendar(IJsonWriter writer)
        {
            using var profile = ModProfiler.UIWriteCalendar.Auto();
            m_View.Write(writer);
        }

        private void SetOpen(bool open)
        {
            using var profile = ModProfiler.UISetOpen.Auto();
            if (open && !m_IsOpen.value)
                Mod.Calendar?.ReloadIfOlderThan(kRefreshOnOpenAfter);

            m_IsOpen.Update(open);
        }

        // Sent by the UI after the "Open on Paradox Mods" button handed the page to the game (in-game mod view or browser).
        private void OnModPageOpened(int modId, bool inGame, double elapsedMilliseconds)
        {
            ModProfiler.UIOpenModPage.RecordMilliseconds(elapsedMilliseconds);
            Mod.Log.Info($"Opened Paradox Mods page {modId} ({(inGame ? "in-game" : "browser")}, {elapsedMilliseconds:0.0} ms)");
        }

        private void MarkWelcomeSeen()
        {
            Setting settings = Mod.Settings;
            if (settings == null || settings.WelcomeSeen)
                return;

            settings.WelcomeSeen = true;
            settings.ApplyAndSave();
            m_WelcomeSeen.Update(true);
        }
    }
}
