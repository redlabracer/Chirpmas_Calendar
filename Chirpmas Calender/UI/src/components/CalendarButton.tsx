import { useValue } from "cs2/api";
import { Button } from "cs2/ui";
import { isOpen$, newDoor$, showButton$, toggleCalendar } from "bindings";
import { BUTTON_ICON } from "images";
import { useText } from "localization";

/** Top-left toggle button. Shows a red dot until the newest open door has been looked at (remembered across restarts). */
export const CalendarButton = () => {
    const show = useValue(showButton$);
    const open = useValue(isOpen$);
    const newDoor = useValue(newDoor$);
    const text = useText();

    if (!show) return null;

    return (
        <div className="cc-button-wrapper">
            <Button
                variant="floating"
                src={BUTTON_ICON}
                tinted={false}
                selected={open}
                tooltipLabel={text("ButtonTooltip")}
                onSelect={toggleCalendar}
            />
            {newDoor && !open && <div className="cc-badge" />}
        </div>
    );
};
