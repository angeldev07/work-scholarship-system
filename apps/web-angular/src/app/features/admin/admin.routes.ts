import { Routes } from '@angular/router';

export const adminRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard (RF-046)
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
  },

  // Gestión de Ciclos (RF-006 a RF-012)
  {
    path: 'cycles',
    children: [
      { path: '', redirectTo: 'active', pathMatch: 'full' },
      {
        path: 'new',
        loadComponent: () =>
          import('./cycles/create-cycle/create-cycle.component').then(
            (m) => m.CreateCycleComponent,
          ),
      },
      {
        path: 'active',
        loadComponent: () =>
          import('./cycles/active-cycle/active-cycle.component').then(
            (m) => m.ActiveCycleComponent,
          ),
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./cycles/cycle-history/cycle-history.component').then(
            (m) => m.CycleHistoryComponent,
          ),
      },
      {
        path: ':id/extend-dates',
        loadComponent: () =>
          import('./cycles/extend-dates-dialog/extend-dates-dialog.component').then(
            (m) => m.ExtendDatesDialogComponent,
          ),
      },
      {
        path: ':id/configure',
        loadComponent: () =>
          import('./cycles/cycle-wizard/cycle-wizard.component').then(
            (m) => m.CycleWizardComponent,
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./cycles/cycle-detail/cycle-detail.component').then(
            (m) => m.CycleDetailComponent,
          ),
      },
    ],
  },

  // Proceso de Selección (RF-013 a RF-022)
  {
    path: 'selection',
    children: [
      { path: '', redirectTo: 'applicants', pathMatch: 'full' },
      {
        path: 'applicants',
        loadComponent: () =>
          import('./selection/applicants/applicants.component').then((m) => m.ApplicantsComponent),
      },
      {
        path: 'applicants/:id',
        loadComponent: () =>
          import('./selection/applicant-detail/applicant-detail.component').then(
            (m) => m.ApplicantDetailComponent,
          ),
      },
      {
        path: 'assignment',
        loadComponent: () =>
          import('./selection/assignment/assignment.component').then((m) => m.AssignmentComponent),
      },
      {
        path: 'renewals',
        loadComponent: () =>
          import('./selection/renewals/renewals.component').then((m) => m.RenewalsComponent),
      },
    ],
  },

  // Gestión de Ubicaciones (RF-023 a RF-028)
  {
    path: 'locations',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./locations/locations-list/locations-list.component').then(
            (m) => m.LocationsListComponent,
          ),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./locations/location-form/location-form.component').then(
            (m) => m.LocationFormComponent,
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./locations/location-detail/location-detail.component').then(
            (m) => m.LocationDetailComponent,
          ),
      },
      {
        path: ':id/edit',
        loadComponent: () =>
          import('./locations/location-form/location-form.component').then(
            (m) => m.LocationFormComponent,
          ),
      },
    ],
  },

  // Tracking / Jornadas (RF-032 a RF-034)
  {
    path: 'shifts',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      {
        path: 'pending',
        loadComponent: () =>
          import('./shifts/shifts-pending/shifts-pending.component').then(
            (m) => m.ShiftsPendingComponent,
          ),
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./shifts/shifts-history/shifts-history.component').then(
            (m) => m.ShiftsHistoryComponent,
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./shifts/shift-detail/shift-detail.component').then(
            (m) => m.ShiftDetailComponent,
          ),
      },
    ],
  },

  // Ausencias (RF-036 a RF-039)
  {
    path: 'absences',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      {
        path: 'pending',
        loadComponent: () =>
          import('./absences/absences-pending/absences-pending.component').then(
            (m) => m.AbsencesPendingComponent,
          ),
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./absences/absences-history/absences-history.component').then(
            (m) => m.AbsencesHistoryComponent,
          ),
      },
    ],
  },

  // Documentos (RF-040 a RF-042)
  {
    path: 'documents',
    children: [
      { path: '', redirectTo: 'badges', pathMatch: 'full' },
      {
        path: 'badges',
        loadComponent: () =>
          import('./documents/badges/badges.component').then((m) => m.BadgesComponent),
      },
      {
        path: 'logs',
        loadComponent: () =>
          import('./documents/logbooks/logbooks.component').then((m) => m.LogbooksComponent),
      },
      {
        path: 'export',
        loadComponent: () =>
          import('./documents/export/export.component').then((m) => m.ExportComponent),
      },
    ],
  },

  // Reportes (RF-046, RF-049 a RF-051)
  {
    path: 'reports',
    loadComponent: () =>
      import('./reports/reports.component').then((m) => m.ReportsComponent),
  },

  // Notificaciones (RF-043 a RF-045)
  {
    path: 'notifications',
    loadComponent: () =>
      import('./notifications/notifications.component').then((m) => m.NotificationsComponent),
  },

  // Auditoría (RF-052 a RF-054)
  {
    path: 'audit',
    loadComponent: () =>
      import('./audit/audit.component').then((m) => m.AuditComponent),
  },

  // Gestión de Usuarios (RF-003)
  {
    path: 'users',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./users/users-list/users-list.component').then((m) => m.UsersListComponent),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./users/user-detail/user-detail.component').then((m) => m.UserDetailComponent),
      },
    ],
  },
];
