import { bindValue, trigger } from "cs2/api";
import mod from "mod.json";

export type DayState = "locked" | "pending" | "open";

export interface CalendarDay {
    day: number;
    state: DayState;
    isToday: boolean;
    title: string | null;
    creator: string | null;
    type: string | null;
    description: string | null;
    imageUrl: string | null;
    /** 0 = no Paradox Mods page yet. */
    pdxModId: number;
}

export interface CalendarData {
    status: "loading" | "ready" | "error";
    error: string | null;
    /** Server unreachable: showing the last download or the bundled file. */
    offline: boolean;
    eventActive: boolean;
    /** Highest unlocked day, 0 before the event. */
    today: number;
    simulated: boolean;
    packModId: number;
    announcement: string | null;
    days: CalendarDay[];
}

const EMPTY_CALENDAR: CalendarData = {
    status: "loading",
    error: null,
    offline: false,
    eventActive: true,
    today: 0,
    simulated: false,
    packModId: 0,
    announcement: null,
    days: [],
};

// Group name = mod id, the same string the C# side uses (Mod.Id).
export const isOpen$ = bindValue<boolean>(mod.id, "isOpen", false);
export const showButton$ = bindValue<boolean>(mod.id, "showButton", false);
export const calendar$ = bindValue<CalendarData>(mod.id, "calendar", EMPTY_CALENDAR);
// True while the newest open door hasn't been looked at yet (persisted in the mod settings).
export const newDoor$ = bindValue<boolean>(mod.id, "newDoor", false);
// Persisted in the mod settings; true once the welcome pop-up was dismissed.
export const welcomeSeen$ = bindValue<boolean>(mod.id, "welcomeSeen", false);

export const toggleCalendar = () => trigger(mod.id, "toggle");
export const setCalendarOpen = (open: boolean) => trigger(mod.id, "setOpen", open);
export const reloadCalendar = () => trigger(mod.id, "reload");
export const markWelcomeSeen = () => trigger(mod.id, "markWelcomeSeen");
/** Profiler/log hook: a Paradox Mods page was handed to the game (in-game view or browser) after `elapsedMs`. */
export const reportModPageOpened = (modId: number, inGame: boolean, elapsedMs: number) =>
    trigger(mod.id, "modPageOpened", modId, inGame, elapsedMs);
