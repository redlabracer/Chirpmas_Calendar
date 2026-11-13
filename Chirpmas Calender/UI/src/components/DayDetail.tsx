import { useState } from "react";
// Only import components the game actually exports at runtime: cs2/ui typings also list InfoSection/InfoRow, but they are undefined in-game.
import { Button, Icon, Panel } from "cs2/ui";
import { CalendarDay } from "bindings";
import { chirpyForDay } from "images";
import { useText } from "localization";
import { openModPage } from "pdx";

interface DayDetailProps {
    day: CalendarDay;
    onClose: () => void;
}

const cssUrl = (url: string) => `url("${url.replace(/"/g, "%22")}")`;

// Game glyphs (served by the game UI, no need to ship them).
const ICON_LOCK = "Media/Glyphs/Lock.svg";
const ICON_PDX = "Media/Glyphs/ParadoxMods.svg";
const ICON_BACK = "Media/Glyphs/ArrowLeft.svg";

// Passing a theme replaces the variant's styling completely, so these buttons only carry our CS2 dialog-button classes.
const POSITIVE_BUTTON = { button: "cc-cs2-button cc-cs2-button-positive" };
const NEGATIVE_BUTTON = { button: "cc-cs2-button cc-cs2-button-negative" };

/** Game-styled panel shown on top of the calendar after clicking a door. */
export const DayDetail = ({ day, onClose }: DayDetailProps) => {
    const text = useText();
    const locked = day.state === "locked";
    const pending = day.state === "pending";
    const hasPdxLink = day.state === "open" && day.pdxModId > 0;

    return (
        <div className="cc-detail-backdrop" onClick={onClose}>
            <div onClick={(e) => e.stopPropagation()}>
                <Panel
                    className={locked ? "cc-detail-panel cc-detail-panel-locked" : "cc-detail-panel"}
                    contentClassName="cc-detail-content"
                    header={<div className="cc-header-title">{text("Day", { DAY: day.day })}</div>}
                    onClose={onClose}
                >
                    {locked ? (
                        <div className="cc-locked">
                            <div className="cc-locked-badge">
                                <Icon src={ICON_LOCK} tinted className="cc-locked-icon" />
                            </div>
                            <div className="cc-locked-title">{text("LockedTitle")}</div>
                            <div className="cc-locked-text">{text("Locked", { DAY: day.day })}</div>
                        </div>
                    ) : (
                        <div className="cc-detail-row">
                            <DayCover day={day} />

                            <div className="cc-detail-body">
                                {day.title && <div className="cc-detail-title">{day.title}</div>}

                                <div className="cc-info-section">
                                    {day.creator && <InfoLine label={text("Creator")} value={day.creator} last={!day.type} />}
                                    {day.type && <InfoLine label={text("Type")} value={day.type} last />}
                                </div>

                                {pending ? (
                                    <div className="cc-detail-pending">
                                        <div className="cc-detail-pending-title">{text("PendingTitle")}</div>
                                        <div className="cc-detail-text">{text("Pending")}</div>
                                    </div>
                                ) : (
                                    day.description && <div className="cc-detail-text cc-detail-description">{day.description}</div>
                                )}
                            </div>
                        </div>
                    )}

                    <div className="cc-detail-actions">
                        {hasPdxLink && (
                            <Button theme={POSITIVE_BUTTON} onSelect={() => openModPage(day.pdxModId)}>
                                <Icon src={ICON_PDX} tinted className="cc-cs2-button-icon" />
                                <span className="cc-cs2-button-label">{text("OpenOnPdx")}</span>
                            </Button>
                        )}
                        <Button theme={NEGATIVE_BUTTON} onSelect={onClose}>
                            <Icon src={ICON_BACK} tinted className="cc-cs2-button-icon" />
                            <span className="cc-cs2-button-label">{text("Back")}</span>
                        </Button>
                    </div>

                    {hasPdxLink && <div className="cc-detail-hint">{text("RestartHint")}</div>}
                </Panel>
            </div>
        </div>
    );
};

/** Label/value row mimicking the game's selected-info panel rows. */
const InfoLine = ({ label, value, last }: { label: string; value: string; last?: boolean }) => (
    <div className={last ? "cc-info-row cc-info-row-last" : "cc-info-row"}>
        <div className="cc-info-label">{label}</div>
        <div className="cc-info-value">{value}</div>
    </div>
);

/** Asset cover (Paradox Mods image), falling back to a festive Chirpy if it is missing or fails to load. */
const DayCover = ({ day }: { day: CalendarDay }) => {
    const [failedImage, setFailedImage] = useState<string | null>(null);
    const remoteImage = day.imageUrl && day.imageUrl !== failedImage ? day.imageUrl : null;

    return (
        <div className="cc-detail-media">
            {remoteImage ? (
                <>
                    <div className="cc-detail-cover" style={{ backgroundImage: cssUrl(remoteImage) }} />
                    {/* Gameface has no object-fit, so the visible image is a background; this probe only detects load failures. */}
                    <img
                        key={remoteImage}
                        className="cc-detail-probe"
                        src={remoteImage}
                        onError={() => {
                            console.warn(`[ChirpmasCalendar] image failed, using fallback: ${remoteImage}`);
                            setFailedImage(remoteImage);
                        }}
                    />
                </>
            ) : (
                <div className="cc-detail-chirpy" style={{ backgroundImage: cssUrl(chirpyForDay(day.day)) }} />
            )}
        </div>
    );
};
