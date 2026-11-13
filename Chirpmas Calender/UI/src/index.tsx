import { ModRegistrar } from "cs2/modding";
import { CalendarButton } from "components/CalendarButton";
import { CalendarPanel } from "components/CalendarPanel";
import { withErrorBoundary } from "components/ErrorBoundary";
import "styles/calendar.scss";

const register: ModRegistrar = (moduleRegistry) => {
    // The game calls every mod's registrar in one unguarded loop: a throw here would break other mods' UI too.
    try {
        moduleRegistry.append("GameTopLeft", withErrorBoundary("CalendarButton", CalendarButton));
        moduleRegistry.append("Game", withErrorBoundary("CalendarPanel", CalendarPanel));
    } catch (error) {
        console.error("[ChirpmasCalendar] UI registration failed", error);
    }
};

export default register;
