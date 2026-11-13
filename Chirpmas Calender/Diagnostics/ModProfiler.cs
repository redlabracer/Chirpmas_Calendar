using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace ChirpmasCalendar.Diagnostics
{
    public static class ModProfiler
    {
        public const double kReportIntervalSeconds = 60;

        private static readonly List<ProfilerPoint> s_Points = new List<ProfilerPoint>();

        public static readonly ProfilerPoint ModLoad = Create("Mod.OnLoad");

        public static readonly ProfilerPoint UIUpdate = Create("UISystem.OnUpdate");
        public static readonly ProfilerPoint UIWriteCalendar = Create("UISystem.WriteCalendar");
        public static readonly ProfilerPoint UISetOpen = Create("UISystem.SetOpen");

        // Measured in the React UI (click until the link is handed to the game) and reported through the "modPageOpened" trigger.
        public static readonly ProfilerPoint UIOpenModPage = Create("UI.OpenModPage");

        public static readonly ProfilerPoint SettingsSave = Create("Settings.ApplyAndSave");

        public static readonly ProfilerPoint ChirpUpdate = Create("ChirpSystem.OnUpdate");
        public static readonly ProfilerPoint ChirpQueueDoor = Create("ChirpSystem.QueueNewestDoor");
        public static readonly ProfilerPoint ChirpPost = Create("ChirpSystem.Post");
        public static readonly ProfilerPoint CustomChirpsResolve = Create("CustomChirps.Resolve");

        public static readonly ProfilerPoint GetView = Create("Calendar.GetView");
        public static readonly ProfilerPoint BuildView = Create("Calendar.BuildView");
        public static readonly ProfilerPoint Load = Create("Calendar.Load");
        public static readonly ProfilerPoint Parse = Create("Calendar.Parse");

        public static readonly ProfilerPoint Download = Create("Remote.Download");
        public static readonly ProfilerPoint Fallback = Create("Remote.Fallback");
        public static readonly ProfilerPoint CacheRead = Create("Remote.ReadCache");
        public static readonly ProfilerPoint CacheWrite = Create("Remote.WriteCache");
        public static readonly ProfilerPoint LocalRead = Create("Local.ReadFile");

        private static volatile bool s_Enabled;
        private static long s_SinceTimestamp = Stopwatch.GetTimestamp();
        private static long s_NextReportTimestamp;

        public static IReadOnlyList<ProfilerPoint> Points => s_Points;

        public static bool Enabled
        {
            get => s_Enabled;
            set
            {
                if (s_Enabled == value)
                    return;

                if (value)
                    Reset();
                else
                    LogReport();

                s_Enabled = value;
                Mod.Log.Info($"Profiling {(value ? "enabled" : "disabled")}");
            }
        }

        public static void Reset()
        {
            foreach (ProfilerPoint point in s_Points)
                point.Reset();

            long now = Stopwatch.GetTimestamp();
            Interlocked.Exchange(ref s_SinceTimestamp, now);
            Interlocked.Exchange(ref s_NextReportTimestamp, now + SecondsToTicks(kReportIntervalSeconds));
        }

        // Call once per frame from the main threa writes the periodic report while profiling is on
        public static void Update()
        {
            if (!s_Enabled)
                return;

            long now = Stopwatch.GetTimestamp();
            if (now < Interlocked.Read(ref s_NextReportTimestamp))
                return;

            Interlocked.Exchange(ref s_NextReportTimestamp, now + SecondsToTicks(kReportIntervalSeconds));
            LogReport();
        }

        public static void LogReport()
        {
            Mod.Log.Info(BuildReport());
        }

        public static string BuildReport()
        {
            double seconds = (Stopwatch.GetTimestamp() - Interlocked.Read(ref s_SinceTimestamp)) / (double)Stopwatch.Frequency;

            var report = new StringBuilder(128 + s_Points.Count * 96);
            report.AppendFormat(CultureInfo.InvariantCulture, "Profiler report ({0:0.0} s since reset{1})", seconds, s_Enabled ? string.Empty : ", timing off").AppendLine();
            report.AppendFormat(CultureInfo.InvariantCulture, "  {0,-46} {1,9} {2,8} {3,12} {4,10} {5,10}", "Point", "Calls", "Calls/s", "Total ms", "Avg ms", "Max ms").AppendLine();

            foreach (ProfilerPoint point in s_Points)
            {
                long count = point.Count;
                report.AppendFormat(CultureInfo.InvariantCulture, "  {0,-46} {1,9} {2,8:0.0} {3,12:0.000} {4,10:0.0000} {5,10:0.000}",
                        point.Name, count, seconds > 0 ? count / seconds : 0, point.TotalMilliseconds, point.AverageMilliseconds, point.MaxMilliseconds)
                    .AppendLine();
            }

            return report.ToString();
        }

        private static ProfilerPoint Create(string name)
        {
            var point = new ProfilerPoint(Mod.Id + "." + name);
            s_Points.Add(point);
            return point;
        }

        private static long SecondsToTicks(double seconds) => (long)(seconds * Stopwatch.Frequency);
    }
}
