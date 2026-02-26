import { Routes } from '@angular/router';

export const supervisorRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard (RF-047)
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/supervisor-dashboard.component').then(
        (m) => m.SupervisorDashboardComponent,
      ),
  },

  // Mis Becas (RF-050, RF-053)
  {
    path: 'scholars',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./scholars/scholars-list/scholars-list.component').then(
            (m) => m.ScholarsListComponent,
          ),
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./scholars/scholar-detail/scholar-detail.component').then(
            (m) => m.ScholarDetailComponent,
          ),
      },
    ],
  },

  // Jornadas — Aprobar (RF-032, RF-033)
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
          import('./shifts/shift-review/shift-review.component').then(
            (m) => m.ShiftReviewComponent,
          ),
      },
    ],
  },

  // Ausencias — Revisar (RF-036, RF-037)
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

  // Entrevistas (RF-018)
  {
    path: 'interviews',
    loadComponent: () =>
      import('./interviews/interviews.component').then((m) => m.InterviewsComponent),
  },

  // Bitácora (RF-041)
  {
    path: 'logbook',
    loadComponent: () =>
      import('./logbook/logbook.component').then((m) => m.LogbookComponent),
  },
];
