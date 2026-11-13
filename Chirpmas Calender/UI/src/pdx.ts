import { call, trigger } from "cs2/api";
import { reportModPageOpened } from "bindings";

const PDX_MODS_WEB = "https://mods.paradoxplaza.com/mods/";

const now = () => (typeof performance !== "undefined" && performance.now ? performance.now() : Date.now());

/**
 * Opens a mod's Paradox Mods page using the game's own "paradox" bindings:
 * the in-game mod detail view if available, otherwise the browser.
 * The time until the page was handed to the game is reported to the mod profiler.
 */
export async function openModPage(modId: number): Promise<void> {
    if (!Number.isInteger(modId) || modId <= 0) return;

    const started = now();
    const prerequisite = `Mod:${modId}`;
    try {
        if (await call<boolean>("paradox", "canClickPrerequisite", prerequisite)) {
            trigger("paradox", "prerequisiteClicked", prerequisite);
            reportModPageOpened(modId, true, now() - started);
            return;
        }
    } catch (error) {
        console.warn("[ChirpmasCalendar] canClickPrerequisite failed", error);
    }

    trigger("paradox", "showLink", `${PDX_MODS_WEB}${modId}/Windows`);
    reportModPageOpened(modId, false, now() - started);
}
