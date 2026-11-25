using System;
using System.Globalization;
using Colossal.IO.AssetDatabase;

namespace ChirpmasCalendar.Integrations
{
    /// <summary>
    /// Asks Paradox Mods whether a calendar entry is subscribed and enabled in the active playset.
    /// Checked in game 1.6.5f1: a new subscription counts right away, an entry disabled in the playset stops counting
    /// right away, and assets count the same as code mods. Mods loaded from the local Mods folder never count.
    /// The answer can change at any time, so callers ask again every time instead of caching it.
    /// </summary>
    internal static class PdxModsBridge
    {
        private static bool s_LoggedError;

        /// <returns>true/false, or null when Paradox Mods can't be asked (callers then fall back to the old rule).</returns>
        public static bool? IsActive(int pdxModId)
        {
            if (pdxModId <= 0)
                return null;

            try
            {
                if (!(AssetDatabase<ParadoxMods>.instance?.dataSource is ParadoxModsDataSource source))
                    return null;

                return source.ContainsActiveMod(pdxModId.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                if (!s_LoggedError)
                {
                    s_LoggedError = true;
                    Mod.Log.Warn(e, "Could not ask Paradox Mods which calendar entries are active");
                }

                return null;
            }
        }
    }
}
