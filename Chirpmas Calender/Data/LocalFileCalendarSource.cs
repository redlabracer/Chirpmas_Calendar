using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ChirpmasCalendar.Diagnostics;
using Colossal.PSI.Environment;

namespace ChirpmasCalendar.Data
{
    
    // Reads ModsData/ChirpmasCalendar/calendar.json if it exists, otherwise the calendar.json shipped in the mod's Data folder.
    
    public sealed class LocalFileCalendarSource : ICalendarSource
    {
        public const string kFileName = "calendar.json";

        private readonly string m_BundledPath;

        public LocalFileCalendarSource(string modDirectory)
        {
            m_BundledPath = modDirectory == null ? null : Path.Combine(modDirectory, "Data", kFileName);
        }

        public static string UserDirectory => Path.Combine(EnvPath.kUserDataPath, "ModsData", Mod.Id);

        public static string UserFilePath => Path.Combine(UserDirectory, kFileName);

        public Task<CalendarFetchResult> FetchAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using var profile = ModProfiler.LocalRead.Auto();
                string path = File.Exists(UserFilePath) ? UserFilePath : m_BundledPath;
                if (path == null || !File.Exists(path))
                    throw new FileNotFoundException($"No {kFileName} found (looked in {UserDirectory} and the mod folder)");

                return new CalendarFetchResult(File.ReadAllText(path), path);
            }, cancellationToken);
        }

        public Task<CalendarFetchResult> FetchBundledAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using var profile = ModProfiler.LocalRead.Auto();
                if (m_BundledPath == null || !File.Exists(m_BundledPath))
                    throw new FileNotFoundException($"The {kFileName} shipped with the mod is missing");

                return new CalendarFetchResult(File.ReadAllText(m_BundledPath), m_BundledPath);
            }, cancellationToken);
        }

        public string EnsureEditableCopy()
        {
            Directory.CreateDirectory(UserDirectory);
            if (!File.Exists(UserFilePath) && m_BundledPath != null && File.Exists(m_BundledPath))
                File.Copy(m_BundledPath, UserFilePath);

            return UserDirectory;
        }
    }
}
