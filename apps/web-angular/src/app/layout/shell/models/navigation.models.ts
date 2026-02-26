import { UserRole } from '../../../core/models/auth.models';

/**
 * Represents a single navigable item in the sidebar menu.
 * Can be a top-level item or a child item (level 2).
 */
export interface NavItem {
  /** Unique identifier — used for tracking, ARIA, and badge lookups */
  id: string;
  /** Visible label text */
  label: string;
  /** PrimeIcons icon name WITHOUT the 'pi pi-' prefix (e.g. 'chart-pie') */
  icon: string;
  /** Angular route path. Absent if the item is a parent with children only */
  route?: string;
  /** Child nav items (level 2, rendered as accordion) */
  children?: NavItem[];
  /** Roles allowed to see this item — used to filter the menu */
  roles: UserRole[];
  /** Key into the `pendingCounts` signal for badge display */
  badgeKey?: string;
  /** Programmatic visibility override (e.g. hide "Postulación" once selected) */
  isVisible?: boolean;
}

/**
 * A logical group of nav items displayed together in the sidebar.
 * Groups can have an optional label separator.
 */
export interface NavGroup {
  /** Optional label shown above the group (uppercase, letter-spacing) */
  label?: string;
  /** Items belonging to this group */
  items: NavItem[];
}

/**
 * Complete navigation configuration — an ordered array of NavGroups.
 * This is the single source of truth for which routes and labels exist.
 */
export type NavConfig = NavGroup[];

/**
 * Shape of the pending counts record used to display numeric badges.
 * Keys match the `badgeKey` field of NavItem.
 */
export interface PendingCounts {
  /** Jornadas pendientes de aprobación */
  shifts: number;
  /** Ausencias pendientes de revisión */
  absences: number;
  /** Postulantes pendientes de revisión */
  applicants: number;
}
