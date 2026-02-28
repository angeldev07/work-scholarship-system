// ============================================================================
// WIZARD MODELS — internal state shapes for the cycle configuration wizard
// ============================================================================

/** Represents a location row in the locations step. */
export interface LocationRow {
  locationId: string;
  locationName: string;
  building: string;
  isActive: boolean;
  scholarshipsAvailable: number;
}

/** Maps locationId → supervisorId (empty string means unassigned). */
export type SupervisorAssignmentMap = Record<string, string>;

/** A single schedule slot for a location. */
export interface ScheduleSlotRow {
  dayOfWeek: number; // 1 = Monday ... 7 = Sunday
  startTime: Date | null;
  endTime: Date | null;
  requiredScholars: number;
}

/** Maps locationId → array of schedule slots. */
export type LocationScheduleMap = Record<string, ScheduleSlotRow[]>;

/** Day-of-week options for the schedule step. */
export const DAYS_OF_WEEK: { value: number; label: string; abbr: string }[] = [
  { value: 1, label: 'Lunes', abbr: 'L' },
  { value: 2, label: 'Martes', abbr: 'M' },
  { value: 3, label: 'Miércoles', abbr: 'X' },
  { value: 4, label: 'Jueves', abbr: 'J' },
  { value: 5, label: 'Viernes', abbr: 'V' },
];
