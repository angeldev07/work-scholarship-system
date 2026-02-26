import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/auth.models';
import { NavConfig, NavGroup, NavItem, PendingCounts } from '../models/navigation.models';

/**
 * NavigationService builds the sidebar menu configuration and filters it
 * based on the currently authenticated user's role.
 *
 * This is the SINGLE source of truth for:
 * - What routes exist in the authenticated area
 * - What labels and icons each route uses
 * - Which roles can see each section
 * - What badge keys map to pending counts
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly authService = inject(AuthService);

  // ─── Static navigation configuration ─────────────────────────────────────
  // All roles see items filtered by their `roles` array.
  // Order of groups and items reflects the sidebar visual order.

  private readonly NAV_CONFIG: NavConfig = [
    // ── Main navigation group ──────────────────────────────────────────────
    {
      items: [
        // Dashboard — each role has its own dashboard
        {
          id: 'admin-dashboard',
          label: 'Dashboard',
          icon: 'chart-pie',
          route: '/admin/dashboard',
          roles: [UserRole.ADMIN],
        },
        {
          id: 'supervisor-dashboard',
          label: 'Dashboard',
          icon: 'chart-pie',
          route: '/supervisor/dashboard',
          roles: [UserRole.SUPERVISOR],
        },
        {
          id: 'scholar-dashboard',
          label: 'Mi Dashboard',
          icon: 'home',
          route: '/scholar/dashboard',
          roles: [UserRole.BECA],
        },

        // ── ADMIN: Ciclos ─────────────────────────────────────────────────
        {
          id: 'admin-cycles',
          label: 'Ciclos',
          icon: 'calendar',
          roles: [UserRole.ADMIN],
          children: [
            {
              id: 'admin-cycles-active',
              label: 'Ciclo Activo',
              icon: 'circle-fill',
              route: '/admin/cycles/active',
              roles: [UserRole.ADMIN],
            },
            {
              id: 'admin-cycles-history',
              label: 'Historial de Ciclos',
              icon: 'history',
              route: '/admin/cycles/history',
              roles: [UserRole.ADMIN],
            },
          ],
        },

        // ── ADMIN: Selección ──────────────────────────────────────────────
        {
          id: 'admin-selection',
          label: 'Selección',
          icon: 'users',
          roles: [UserRole.ADMIN],
          children: [
            {
              id: 'admin-selection-applicants',
              label: 'Postulantes',
              icon: 'user-plus',
              route: '/admin/selection/applicants',
              roles: [UserRole.ADMIN],
              badgeKey: 'applicants',
            },
            {
              id: 'admin-selection-assignment',
              label: 'Asignación',
              icon: 'send',
              route: '/admin/selection/assignment',
              roles: [UserRole.ADMIN],
            },
            {
              id: 'admin-selection-renewals',
              label: 'Renovaciones',
              icon: 'sync',
              route: '/admin/selection/renewals',
              roles: [UserRole.ADMIN],
            },
          ],
        },

        // ── ADMIN: Ubicaciones ────────────────────────────────────────────
        {
          id: 'admin-locations',
          label: 'Ubicaciones',
          icon: 'map-marker',
          route: '/admin/locations',
          roles: [UserRole.ADMIN],
        },

        // ── ADMIN + SUPERVISOR: Jornadas ──────────────────────────────────
        {
          id: 'admin-shifts',
          label: 'Jornadas',
          icon: 'clock',
          roles: [UserRole.ADMIN],
          children: [
            {
              id: 'admin-shifts-pending',
              label: 'Pendientes',
              icon: 'hourglass',
              route: '/admin/shifts/pending',
              roles: [UserRole.ADMIN],
              badgeKey: 'shifts',
            },
            {
              id: 'admin-shifts-history',
              label: 'Historial',
              icon: 'list',
              route: '/admin/shifts/history',
              roles: [UserRole.ADMIN],
            },
          ],
        },
        {
          id: 'supervisor-shifts',
          label: 'Jornadas',
          icon: 'clock',
          roles: [UserRole.SUPERVISOR],
          children: [
            {
              id: 'supervisor-shifts-pending',
              label: 'Pendientes de Aprobar',
              icon: 'hourglass',
              route: '/supervisor/shifts/pending',
              roles: [UserRole.SUPERVISOR],
              badgeKey: 'shifts',
            },
            {
              id: 'supervisor-shifts-history',
              label: 'Historial',
              icon: 'list',
              route: '/supervisor/shifts/history',
              roles: [UserRole.SUPERVISOR],
            },
          ],
        },

        // ── BECA: Mi Jornada ──────────────────────────────────────────────
        {
          id: 'scholar-shift',
          label: 'Mi Jornada',
          icon: 'play-circle',
          route: '/scholar/shift',
          roles: [UserRole.BECA],
        },

        // ── BECA: Mis Horas ───────────────────────────────────────────────
        {
          id: 'scholar-hours',
          label: 'Mis Horas',
          icon: 'clock',
          route: '/scholar/hours',
          roles: [UserRole.BECA],
        },

        // ── ADMIN + SUPERVISOR: Ausencias ─────────────────────────────────
        {
          id: 'admin-absences',
          label: 'Ausencias',
          icon: 'exclamation-circle',
          roles: [UserRole.ADMIN],
          children: [
            {
              id: 'admin-absences-pending',
              label: 'Pendientes',
              icon: 'hourglass',
              route: '/admin/absences/pending',
              roles: [UserRole.ADMIN],
              badgeKey: 'absences',
            },
            {
              id: 'admin-absences-history',
              label: 'Historial',
              icon: 'list',
              route: '/admin/absences/history',
              roles: [UserRole.ADMIN],
            },
          ],
        },
        {
          id: 'supervisor-absences',
          label: 'Ausencias',
          icon: 'exclamation-circle',
          roles: [UserRole.SUPERVISOR],
          children: [
            {
              id: 'supervisor-absences-pending',
              label: 'Pendientes de Revisar',
              icon: 'hourglass',
              route: '/supervisor/absences/pending',
              roles: [UserRole.SUPERVISOR],
              badgeKey: 'absences',
            },
            {
              id: 'supervisor-absences-history',
              label: 'Historial',
              icon: 'list',
              route: '/supervisor/absences/history',
              roles: [UserRole.SUPERVISOR],
            },
          ],
        },

        // ── BECA: Ausencias ───────────────────────────────────────────────
        {
          id: 'scholar-absences',
          label: 'Ausencias',
          icon: 'exclamation-circle',
          roles: [UserRole.BECA],
          children: [
            {
              id: 'scholar-absences-new',
              label: 'Reportar Ausencia',
              icon: 'plus-circle',
              route: '/scholar/absences/new',
              roles: [UserRole.BECA],
            },
            {
              id: 'scholar-absences-list',
              label: 'Mis Solicitudes',
              icon: 'list',
              route: '/scholar/absences',
              roles: [UserRole.BECA],
            },
          ],
        },

        // ── BECA: Adelanto de Horas ───────────────────────────────────────
        {
          id: 'scholar-extra-hours',
          label: 'Adelanto de Horas',
          icon: 'calendar-plus',
          roles: [UserRole.BECA],
          children: [
            {
              id: 'scholar-extra-hours-new',
              label: 'Solicitar',
              icon: 'plus-circle',
              route: '/scholar/extra-hours/new',
              roles: [UserRole.BECA],
            },
            {
              id: 'scholar-extra-hours-list',
              label: 'Mis Solicitudes',
              icon: 'list',
              route: '/scholar/extra-hours',
              roles: [UserRole.BECA],
            },
          ],
        },

        // ── SUPERVISOR: Mis Becas ─────────────────────────────────────────
        {
          id: 'supervisor-scholars',
          label: 'Mis Becas',
          icon: 'users',
          route: '/supervisor/scholars',
          roles: [UserRole.SUPERVISOR],
        },

        // ── SUPERVISOR: Entrevistas ───────────────────────────────────────
        {
          id: 'supervisor-interviews',
          label: 'Entrevistas',
          icon: 'calendar',
          route: '/supervisor/interviews',
          roles: [UserRole.SUPERVISOR],
        },

        // ── SUPERVISOR: Bitácora ──────────────────────────────────────────
        {
          id: 'supervisor-logbook',
          label: 'Bitácora',
          icon: 'file-pdf',
          route: '/supervisor/logbook',
          roles: [UserRole.SUPERVISOR],
        },

        // ── ADMIN: Documentos ─────────────────────────────────────────────
        {
          id: 'admin-documents',
          label: 'Documentos',
          icon: 'file-pdf',
          roles: [UserRole.ADMIN],
          children: [
            {
              id: 'admin-documents-badges',
              label: 'Escarapelas',
              icon: 'id-card',
              route: '/admin/documents/badges',
              roles: [UserRole.ADMIN],
            },
            {
              id: 'admin-documents-logs',
              label: 'Bitácoras',
              icon: 'file-pdf',
              route: '/admin/documents/logs',
              roles: [UserRole.ADMIN],
            },
            {
              id: 'admin-documents-export',
              label: 'Exportar',
              icon: 'download',
              route: '/admin/documents/export',
              roles: [UserRole.ADMIN],
            },
          ],
        },

        // ── ADMIN: Reportes ───────────────────────────────────────────────
        {
          id: 'admin-reports',
          label: 'Reportes',
          icon: 'chart-bar',
          route: '/admin/reports',
          roles: [UserRole.ADMIN],
        },

        // ── ADMIN: Notificaciones ─────────────────────────────────────────
        {
          id: 'admin-notifications',
          label: 'Notificaciones',
          icon: 'envelope',
          route: '/admin/notifications',
          roles: [UserRole.ADMIN],
        },

        // ── BECA: Mi Perfil ───────────────────────────────────────────────
        {
          id: 'scholar-profile',
          label: 'Mi Perfil',
          icon: 'user',
          route: '/scholar/profile',
          roles: [UserRole.BECA],
        },

        // ── BECA: Postulación ─────────────────────────────────────────────
        {
          id: 'scholar-application',
          label: 'Postulación',
          icon: 'send',
          route: '/scholar/application',
          roles: [UserRole.BECA],
        },
      ],
    },

    // ── Config/admin group (separated by divider at the bottom) ────────────
    {
      label: 'Configuración',
      items: [
        {
          id: 'admin-audit',
          label: 'Auditoría',
          icon: 'shield',
          route: '/admin/audit',
          roles: [UserRole.ADMIN],
        },
        {
          id: 'admin-users',
          label: 'Usuarios',
          icon: 'users',
          route: '/admin/users',
          roles: [UserRole.ADMIN],
        },
      ],
    },
  ];

  // ─── Computed signals ──────────────────────────────────────────────────────

  /**
   * The navigation config filtered by the current user's role.
   * Reacts automatically when the user logs in/out or role changes.
   */
  readonly navItems = computed<NavConfig>(() => {
    const role = this.authService.currentUser()?.role ?? UserRole.NONE;
    return this.filterByRole(this.NAV_CONFIG, role);
  });

  /**
   * Pending counts for badge display on sidebar items.
   * Currently all zero — will be connected to real APIs in future phases.
   */
  readonly pendingCounts = computed<PendingCounts>(() => ({
    shifts: 0,
    absences: 0,
    applicants: 0,
  }));

  // ─── Private helpers ───────────────────────────────────────────────────────

  private filterByRole(config: NavConfig, role: UserRole): NavConfig {
    const filtered: NavConfig = [];

    for (const group of config) {
      const filteredItems = this.filterItems(group.items, role);
      if (filteredItems.length > 0) {
        filtered.push({ ...group, items: filteredItems });
      }
    }

    return filtered;
  }

  private filterItems(items: NavItem[], role: UserRole): NavItem[] {
    return items
      .filter((item) => item.roles.includes(role))
      .map((item) => {
        if (item.children) {
          const filteredChildren = this.filterItems(item.children, role);
          return { ...item, children: filteredChildren };
        }
        return item;
      });
  }

  /**
   * Resolves the label of a nav item by its route path.
   * Used by TopbarComponent to build the breadcrumb.
   */
  getLabelForRoute(routePath: string): string | null {
    for (const group of this.NAV_CONFIG) {
      for (const item of group.items) {
        if (item.route === routePath) return item.label;
        if (item.children) {
          for (const child of item.children) {
            if (child.route === routePath) return child.label;
          }
        }
      }
    }
    return null;
  }

  /**
   * Resolves the parent label of a child route.
   * Used to build multi-level breadcrumbs.
   */
  getParentLabelForRoute(routePath: string): string | null {
    for (const group of this.NAV_CONFIG) {
      for (const item of group.items) {
        if (item.children) {
          const found = item.children.find((c) => c.route === routePath);
          if (found) return item.label;
        }
      }
    }
    return null;
  }
}
