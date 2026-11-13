import { useLocalization } from "cs2/l10n";
import mod from "mod.json";

// Keys and English text are defined in C# (Localization/LocaleEN.cs); the fallbacks here only show if that source is missing.
const FALLBACKS = {
    Title: "Countdown to Chirpmas",
    Subtitle: "a community collaboration",
    ButtonTooltip: "Chirpmas Advent Calendar",
    Loading: "Loading the calendar…",
    LoadError: "The calendar could not be loaded.",
    Locked: "This door is still locked. Come back on December {DAY}!",
    LockedTitle: "Door locked",
    Pending: "This gift is still being wrapped. Check back later today!",
    PendingTitle: "Coming soon",
    OpenOnPdx: "Open on Paradox Mods",
    SubscribePack: "Get the whole collection",
    Back: "Back to calendar",
    Creator: "Creator",
    Type: "Type",
    Day: "December {DAY}",
    RestartHint: "New assets are loaded the next time you start the game.",
    SimulatedHint: "Simulated date active (Options > Countdown to Chirpmas)",
    Reload: "Try again",
    OfflineHint: "Offline: showing the last saved calendar.",
    WelcomeTitle: "Welcome to Countdown to Chirpmas!",
    WelcomeIntro:
        "Countdown to Chirpmas is a community collaboration. Creators from the Cities: Skylines II community have prepared an advent calendar full of free festive content. Christmas assets, maps and maybe code mods. One door for every day from December 1st to Christmas Eve.",
    WelcomeHowTitle: "How does it work?",
    WelcomeStep1: "A new door opens every day at midnight Pacific time (09:00 CET). A red dot on the Chirpmas button (top left of the screen) tells you when one is waiting.",
    WelcomeStep2: "Click an open door to see the day's gift and who made it, then subscribe to your favourites on Paradox Mods.",
    WelcomeStep3: 'With the "Custom Chirps" mod installed, Chirper announces every new door and shares the creators\' festive chirps.',
    WelcomeCreatorsTitle: "Who's involved?",
    WelcomeDisclaimer: "This community-made project is not affiliated, endorsed, or sponsored by Paradox Interactive or Iceflake Studios.",
    WelcomeStart: "Open the calendar",
    About: "About Chirpmas",
};

export type TextKey = keyof typeof FALLBACKS;

export function useText() {
    const { translate } = useLocalization();
    return (key: TextKey, args?: Record<string, string | number>) => {
        let text = translate(`${mod.id}.UI[${key}]`, FALLBACKS[key]) ?? FALLBACKS[key];
        if (args) {
            for (const name of Object.keys(args)) {
                text = text.split(`{${name}}`).join(String(args[name]));
            }
        }
        return text;
    };
}
