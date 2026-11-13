import chirpy01 from "images/ChirpmasCalendar_Chirpy01.png";
import chirpy02 from "images/ChirpmasCalendar_Chirpy02.png";
import chirpy03 from "images/ChirpmasCalendar_Chirpy03.png";
import chirpy04 from "images/ChirpmasCalendar_Chirpy04.png";
import chirpy05 from "images/ChirpmasCalendar_Chirpy05.png";
import chirpy06 from "images/ChirpmasCalendar_Chirpy06.png";
import chirpy07 from "images/ChirpmasCalendar_Chirpy07.png";
import chirpy08 from "images/ChirpmasCalendar_Chirpy08.png";
import calendarImage from "images/ChirpmasCalendar_Calendar.png";
import welcomeBanner from "images/ChirpmasCalendar_WelcomeBanner.png";

// File names are prefixed with the mod id because every mod's images share the coui://ui-mods/images/ folder.
export const CALENDAR_IMAGE: string = calendarImage;
export const WELCOME_BANNER: string = welcomeBanner;
export const BUTTON_ICON: string = chirpy01;

const CHIRPIES: string[] = [chirpy01, chirpy02, chirpy03, chirpy04, chirpy05, chirpy06, chirpy07, chirpy08];

export const chirpyForDay = (day: number): string => CHIRPIES[(Math.max(day, 1) - 1) % CHIRPIES.length];
