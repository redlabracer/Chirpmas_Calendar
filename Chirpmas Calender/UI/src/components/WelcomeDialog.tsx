import { Button, Panel } from "cs2/ui";
import { CREATORS } from "creators";
import { WELCOME_BANNER } from "images";
import { useText } from "localization";

interface WelcomeDialogProps {
    /** "Open the calendar" button. */
    onContinue: () => void;
    /** Panel close button (X). */
    onClose: () => void;
}

const POSITIVE_BUTTON = { button: "cc-cs2-button cc-cs2-button-positive" };

// Gameface has no CSS grid, so the creator names are split into fixed columns.
const CREATOR_COLUMNS = 2;
const creatorColumns = Array.from({ length: CREATOR_COLUMNS }, (_, column) => {
    const perColumn = Math.ceil(CREATORS.length / CREATOR_COLUMNS);
    return CREATORS.slice(column * perColumn, (column + 1) * perColumn);
});

/** Introduction to the mod and the community event, shown the first time the Chirpmas button is pressed. */
export const WelcomeDialog = ({ onContinue, onClose }: WelcomeDialogProps) => {
    const text = useText();

    return (
        <div className="cc-root">
            <Panel
                className="cc-welcome-panel"
                contentClassName="cc-welcome-content"
                header={<div className="cc-header-title">{text("WelcomeTitle")}</div>}
                onClose={onClose}
            >
                <img className="cc-welcome-banner" src={WELCOME_BANNER} />

                <div className="cc-welcome-intro">{text("WelcomeIntro")}</div>

                <div className="cc-welcome-row">
                    <div className="cc-welcome-section cc-welcome-how">
                        <div className="cc-welcome-section-title">{text("WelcomeHowTitle")}</div>
                        <WelcomeStep number={1} text={text("WelcomeStep1")} />
                        <WelcomeStep number={2} text={text("WelcomeStep2")} />
                        <WelcomeStep number={3} text={text("WelcomeStep3")} />
                    </div>

                    <div className="cc-welcome-section cc-welcome-creators">
                        <div className="cc-welcome-section-title">{text("WelcomeCreatorsTitle")}</div>
                        <div className="cc-welcome-creator-columns">
                            {creatorColumns.map((names, column) => (
                                <div key={column} className="cc-welcome-creator-column">
                                    {names.map((name) => (
                                        <div key={name} className="cc-welcome-creator">
                                            {name}
                                        </div>
                                    ))}
                                </div>
                            ))}
                        </div>
                    </div>
                </div>

                <div className="cc-welcome-footer">
                    <div className="cc-welcome-disclaimer">{text("WelcomeDisclaimer")}</div>
                    <Button theme={POSITIVE_BUTTON} onSelect={onContinue}>
                        <span className="cc-cs2-button-label">{text("WelcomeStart")}</span>
                    </Button>
                </div>
            </Panel>
        </div>
    );
};

const WelcomeStep = ({ number, text }: { number: number; text: string }) => (
    <div className="cc-welcome-step">
        <div className="cc-welcome-step-number">{number}</div>
        <div className="cc-welcome-step-text">{text}</div>
    </div>
);
