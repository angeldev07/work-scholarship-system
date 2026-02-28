// ============================================================================
// ENUMS
// ============================================================================

export enum CycleStatus {
  Configuration = 0,
  ApplicationsOpen = 1,
  ApplicationsClosed = 2,
  Active = 3,
  Closed = 4,
}

export enum PendingActionCode {
  NoLocations = 0,
  NoSupervisors = 1,
  NoActiveCycle = 2,
  CycleNeedsLocations = 3,
  CycleNeedsSupervisors = 4,
  RenewalsPending = 5,
}

// ============================================================================
// CYCLE RESPONSE DTOs
// ============================================================================

export interface CycleDto {
  id: string;
  name: string;
  department: string;
  status: CycleStatus;
  startDate: string;
  endDate: string;
  applicationDeadline: string;
  interviewDate: string;
  selectionDate: string;
  totalScholarshipsAvailable: number;
  totalScholarshipsAssigned: number;
  renewalProcessCompleted: boolean;
  clonedFromCycleId: string | null;
  closedAt: string | null;
  locationsCount: number;
  supervisorsCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CycleDetailDto extends CycleDto {
  closedBy: string | null;
  createdBy: string;
  scholarsCount: number;
}

export interface CycleListItemDto {
  id: string;
  name: string;
  department: string;
  status: CycleStatus;
  startDate: string;
  endDate: string;
  totalScholarshipsAvailable: number;
  totalScholarshipsAssigned: number;
  createdAt: string;
  closedAt: string | null;
}

// ============================================================================
// ADMIN DASHBOARD DTOs
// ============================================================================

export interface PendingActionItem {
  code: PendingActionCode;
  codeString: string;
}

export interface AdminDashboardStateDto {
  hasLocations: boolean;
  locationsCount: number;
  hasSupervisors: boolean;
  supervisorsCount: number;
  activeCycle: CycleDto | null;
  lastClosedCycle: CycleDto | null;
  cycleInConfiguration: CycleDto | null;
  pendingActions: PendingActionItem[];
}

// ============================================================================
// COMMAND REQUEST DTOs
// ============================================================================

export interface CreateCycleRequest {
  name: string;
  department: string;
  startDate: string;
  endDate: string;
  applicationDeadline: string;
  interviewDate: string;
  selectionDate: string;
  totalScholarshipsAvailable: number;
  cloneFromCycleId?: string;
}

export interface ConfigureCycleRequest {
  locations: CycleLocationInput[];
  supervisorAssignments: SupervisorAssignmentInput[];
}

export interface CycleLocationInput {
  locationId: string;
  scholarshipsAvailable: number;
  isActive: boolean;
  scheduleSlots: ScheduleSlotInput[];
}

export interface ScheduleSlotInput {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  requiredScholars: number;
}

export interface SupervisorAssignmentInput {
  supervisorId: string;
  cycleLocationId: string;
}

export interface ExtendCycleDatesRequest {
  newApplicationDeadline?: string;
  newInterviewDate?: string;
  newSelectionDate?: string;
  newEndDate?: string;
}

// ============================================================================
// QUERY PARAMS
// ============================================================================

export interface ListCyclesParams {
  department?: string;
  year?: number;
  status?: CycleStatus;
  page?: number;
  pageSize?: number;
}

// ============================================================================
// STATUS DISPLAY MAP
// ============================================================================

export interface CycleStatusInfo {
  label: string;
  severity: 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast';
  icon: string;
}

export const CYCLE_STATUS_MAP: Record<CycleStatus, CycleStatusInfo> = {
  [CycleStatus.Configuration]: {
    label: 'Configuración',
    severity: 'info',
    icon: 'pi pi-cog',
  },
  [CycleStatus.ApplicationsOpen]: {
    label: 'Postulaciones Abiertas',
    severity: 'success',
    icon: 'pi pi-lock-open',
  },
  [CycleStatus.ApplicationsClosed]: {
    label: 'Postulaciones Cerradas',
    severity: 'warn',
    icon: 'pi pi-lock',
  },
  [CycleStatus.Active]: {
    label: 'Activo',
    severity: 'success',
    icon: 'pi pi-check-circle',
  },
  [CycleStatus.Closed]: {
    label: 'Cerrado',
    severity: 'secondary',
    icon: 'pi pi-ban',
  },
};

// ============================================================================
// ERROR CODES
// ============================================================================

export const CYCLE_ERROR_CODES = {
  DUPLICATE_CYCLE: 'DUPLICATE_CYCLE',
  CYCLE_NOT_FOUND: 'CYCLE_NOT_FOUND',
  INVALID_CLONE_SOURCE: 'INVALID_CLONE_SOURCE',
  NOT_IN_CONFIGURATION: 'NOT_IN_CONFIGURATION',
  INVALID_TRANSITION: 'INVALID_TRANSITION',
  NO_LOCATIONS: 'NO_LOCATIONS',
  NO_SCHOLARSHIPS: 'NO_SCHOLARSHIPS',
  RENEWALS_PENDING: 'RENEWALS_PENDING',
  CYCLE_NOT_ENDED: 'CYCLE_NOT_ENDED',
  PENDING_SHIFTS: 'PENDING_SHIFTS',
  MISSING_LOGBOOKS: 'MISSING_LOGBOOKS',
  CYCLE_CLOSED: 'CYCLE_CLOSED',
  INVALID_DATE: 'INVALID_DATE',
  VALIDATION_ERROR: 'VALIDATION_ERROR',
} as const;

export type CycleErrorCode = (typeof CYCLE_ERROR_CODES)[keyof typeof CYCLE_ERROR_CODES];

// ============================================================================
// PENDING ACTION DISPLAY MAP
// ============================================================================

export const PENDING_ACTION_MAP: Record<string, { label: string; severity: 'info' | 'warn' | 'danger'; icon: string }> = {
  NO_LOCATIONS: {
    label: 'No hay ubicaciones registradas',
    severity: 'danger',
    icon: 'pi pi-map-marker',
  },
  NO_SUPERVISORS: {
    label: 'No hay supervisores disponibles',
    severity: 'danger',
    icon: 'pi pi-users',
  },
  NO_ACTIVE_CYCLE: {
    label: 'No hay ciclo activo',
    severity: 'warn',
    icon: 'pi pi-calendar',
  },
  CYCLE_NEEDS_LOCATIONS: {
    label: 'El ciclo necesita ubicaciones configuradas',
    severity: 'warn',
    icon: 'pi pi-map-marker',
  },
  CYCLE_NEEDS_SUPERVISORS: {
    label: 'El ciclo necesita supervisores asignados',
    severity: 'warn',
    icon: 'pi pi-users',
  },
  RENEWALS_PENDING: {
    label: 'Hay renovaciones pendientes por procesar',
    severity: 'info',
    icon: 'pi pi-refresh',
  },
};
