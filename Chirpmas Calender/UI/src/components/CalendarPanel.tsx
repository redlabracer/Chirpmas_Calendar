import { useValue } from "cs2/api";
import { Button, Icon, Panel } from "cs2/ui";
import { useEffect, useState } from "react";
import { calendar$, isOpen$, markWelcomeSeen, reloadCalendar, setCalendarOpen, welcomeSeen$ } from "bindings";
import { CALENDAR_IMAGE } from "images";
import { useText } from "localization";
import { openModPage } from "pdx";
import { CalendarDoor } from "components/CalendarDoor";
import { DayDetail } from "components/DayDetail";
import { ErrorBoundary } from "components/ErrorBoundary";
import { WelcomeDialog } from "components/WelcomeDialog";

const ICON_INFO = "Media/Glyphs/Info.svg";
const ICON_PDX = "Media/Glyphs/ParadoxMods.svg";
// Same vanilla green dialog button as "Open on Paradox Mods" in DayDetail.
const POSITIVE_BUTTON = { button: "cc-cs2-button cc-cs2-button-positive" };

export const CalendarPanel = () => {
    const open = useValue(isOpen$);
    const calendar = useValue(calendar$);
    const welcomeSeen = useValue(welcomeSeen$);
    const text = useText();
    const [selectedDay, setSelectedDay] = useState<number | null>(null);
    // "About Chirpmas" reopens the welcome; dismissed hides it right away while the saved flag round-trips through C#.
    const [showAbout, setShowAbout] = useState(false);
    const [dismissed, setDismissed] = useState(false);

    useEffect(() => {
        if (!open) {
            setSelectedDay(null);
            setShowAbout(false);
        }
    }, [open]);

    useEffect(() => {
        if (welcomeSeen) setDismissed(false);
    }, [welcomeSeen]);

    if (!open) return null;

    if (showAbout || (!welcomeSeen && !dismissed)) {
        const dismiss = () => {
            setShowAbout(false);
            if (!welcomeSeen) {
                setDismissed(true);
                markWelcomeSeen();
            }
        };
        return (
            <ErrorBoundary name="WelcomeDialog">
                <WelcomeDialog
                    onContinue={dismiss}
                    onClose={() => {
                        dismiss();
                        setCalendarOpen(false);
                    }}
                />
            </ErrorBoundary>
        );
    }

    const selected = selectedDay !== null ? calendar.days.find((d) => d.day === selectedDay) ?? null : null;
    const hasDays = calendar.days.length > 0;

    return (
        <div className="cc-root">
            <Panel
                className="cc-panel"
                contentClassName="cc-panel-content"
                header={<div className="cc-header-title">{text("Title")}</div>}
                onClose={() => setCalendarOpen(false)}
            >
                <div className="cc-stage">
                    <img className="cc-stage-image" src={CALENDAR_IMAGE} />

                    {calendar.days.map((day) => (
                        <CalendarDoor key={day.day} day={day} onSelect={setSelectedDay} />
                    ))}

                    {!hasDays && (
                        <div className="cc-status">
                            <div className="cc-status-text">{calendar.status === "error" ? text("LoadError") : text("Loading")}</div>
                            {calendar.error && <div className="cc-status-error">{calendar.error}</div>}
                        </div>
                    )}

                    {selected && (
                        <ErrorBoundary key={selected.day} name={`DayDetail ${selected.day}`}>
                            <DayDetail day={selected} onClose={() => setSelectedDay(null)} />
                        </ErrorBoundary>
                    )}
                </div>

                <div className="cc-footer">
                    <div className="cc-footer-text">
                        {calendar.simulated
                            ? text("SimulatedHint")
                            : calendar.offline
                              ? text("OfflineHint")
                              : calendar.announcement ?? text("Subtitle")}
                    </div>
                    <Button theme={POSITIVE_BUTTON} onSelect={() => setShowAbout(true)}>
                        <Icon src={ICON_INFO} tinted className="cc-cs2-button-icon" />
                        <span className="cc-cs2-button-label">{text("About")}</span>
                    </Button>
                    {(calendar.status === "error" || calendar.offline) && (
                        <Button variant="flat" className="cc-action-button" onSelect={reloadCalendar}>
                            {text("Reload")}
                        </Button>
                    )}
                    {calendar.packModId > 0 && (
                        <Button theme={POSITIVE_BUTTON} onSelect={() => openModPage(calendar.packModId)}>
                            <Icon src={ICON_PDX} tinted className="cc-cs2-button-icon" />
                            <span className="cc-cs2-button-label">{text("SubscribePack")}</span>
                        </Button>
                    )}
                </div>
            </Panel>
        </div>
    );
};
