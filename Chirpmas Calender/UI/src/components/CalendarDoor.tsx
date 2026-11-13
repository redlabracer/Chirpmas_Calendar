import classNames from "classnames";
import { Button } from "cs2/ui";
import { CalendarDay } from "bindings";
import { DOOR_LAYOUT, IMAGE_HEIGHT, IMAGE_WIDTH } from "doorLayout";

interface CalendarDoorProps {
    day: CalendarDay;
    onSelect: (day: number) => void;
}

const percentX = (px: number) => `${(px / IMAGE_WIDTH) * 100}%`;
const percentY = (px: number) => `${(px / IMAGE_HEIGHT) * 100}%`;

/** Overlay that exactly covers the inside of a painted window of the calendar artwork. */
export const CalendarDoor = ({ day, onSelect }: CalendarDoorProps) => {
    const position = DOOR_LAYOUT[day.day];
    if (!position) return null;

    const className = classNames("cc-door", `cc-door-${day.state}`, {
        "cc-door-today": day.isToday,
        "cc-door-round": position.shape === "round",
        "cc-door-arch": position.shape === "arch",
    });

    // A game Button (rendered as a div, themed with only our classes) gives the door the UI click sound and gamepad focus.
    return (
        <Button
            as="div"
            theme={{ button: className }}
            style={{
                left: percentX(position.x - position.w / 2),
                top: percentY(position.y - position.h / 2),
                width: percentX(position.w),
                height: percentY(position.h),
                transform: position.rotate ? `rotate(${position.rotate}deg)` : undefined,
            }}
            onSelect={() => onSelect(day.day)}
        />
    );
};
