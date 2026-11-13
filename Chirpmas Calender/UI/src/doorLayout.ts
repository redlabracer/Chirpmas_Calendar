/**
 * Painted window interiors of ChirpmasCalendar_Calendar.png, measured in image pixels of the 1920x1080 original by flood-filling each
 * window's fill colour: centre, size of the area inside the painted frame, and the frame's hand-drawn tilt.
 * Doors are placed in percent of these dimensions, so the shipped file can be any 16:9 size (it is downscaled to save UI memory).
 */
export const IMAGE_WIDTH = 1920;
export const IMAGE_HEIGHT = 1080;

export interface DoorPosition {
	x: number;
	y: number;
	w: number;
	h: number;
	/** Clockwise tilt in degrees. */
	rotate?: number;
	shape?: "round" | "arch";
}

export const DOOR_LAYOUT: Record<number, DoorPosition> = {
	1: { x: 721.5, y: 534.0, w: 68.0, h: 81.0, rotate: 0.19 },
	2: { x: 288.9, y: 711.1, w: 82.8, h: 100.8, rotate: 0.61 },
	3: { x: 1066.6, y: 601.6, w: 78.2, h: 74.2, rotate: 1.04 },
	4: { x: 1325.3, y: 526.0, w: 71.5, h: 88.7, rotate: -2.48 },
	5: { x: 1484.9, y: 711.9, w: 78.7, h: 95.1, rotate: -0.58 },
	6: { x: 960.6, y: 603.5, w: 76.3, h: 78.3, rotate: 0.86 },
	7: { x: 659.0, y: 370.0, w: 63.0, h: 57.0, shape: "arch" },
	8: { x: 851.4, y: 726.7, w: 72.2, h: 74.7, rotate: -4.45 },
	9: { x: 292.1, y: 549.7, w: 82.6, h: 102.6, rotate: 1.83 },
	10: { x: 594.7, y: 525.4, w: 72.8, h: 83.9, rotate: 1.24 },
	11: { x: 1198.5, y: 682.9, w: 67.9, h: 83.1, rotate: -0.51 },
	12: { x: 1484.6, y: 561.7, w: 76.1, h: 94.7, rotate: -2.21 },
	13: { x: 851.5, y: 604.7, w: 73.8, h: 80.6, rotate: 1.28 },
	14: { x: 594.0, y: 681.0, w: 75.0, h: 86.9, rotate: 0.26 },
	15: { x: 1198.3, y: 532.6, w: 67.1, h: 81.9, rotate: -2.51 },
	16: { x: 1631.6, y: 550.3, w: 92.1, h: 105.2, rotate: -3.17 },
	17: { x: 959.5, y: 443.0, w: 76.0, h: 71.0, shape: "arch" },
	18: { x: 437.8, y: 561.9, w: 72.8, h: 92.0, rotate: 1.55 },
	19: { x: 1630.5, y: 712.1, w: 94.0, h: 103.1, rotate: 0.46 },
	20: { x: 1069.1, y: 726.0, w: 77.0, h: 82.9, rotate: -0.7 },
	21: { x: 720.7, y: 682.2, w: 69.0, h: 82.3, rotate: 1.7 },
	22: { x: 1326.0, y: 680.8, w: 72.9, h: 86.5, rotate: -1.79 },
	23: { x: 436.5, y: 712.0, w: 72.0, h: 91.0, rotate: 0.08 },
	24: { x: 962.0, y: 277.0, w: 89.0, h: 83.0, shape: "round" },
};
