import { Routes } from '@angular/router';

export const scholarRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard personal (RF-048)
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/scholar-dashboard.component').then((m) => m.ScholarDashboardComponent),
  },

  // Mi Jornada — Check-in / Check-out (RF-029, RF-030, RF-031)
  {
    path: 'shift',
    loadComponent: () =>
      import('./shift/shift.component').then((m) => m.ShiftComponent),
  },

  // Mis Horas acumuladas (RF-033)
  {
    path: 'hours',
    loadComponent: () =>
      import('./hours/hours.component').then((m) => m.HoursComponent),
  },

  // Ausencias (RF-035)
  {
    path: 'absences',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./absences/absences-list/absences-list.component').then(
            (m) => m.AbsencesListComponent,
          ),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./absences/absence-form/absence-form.component').then(
            (m) => m.AbsenceFormComponent,
          ),
      },
    ],
  },

  // Adelanto de Horas (RF-038)
  {
    path: 'extra-hours',
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./extra-hours/extra-hours-list/extra-hours-list.component').then(
            (m) => m.ExtraHoursListComponent,
          ),
      },
      {
        path: 'new',
        loadComponent: () =>
          import('./extra-hours/extra-hours-form/extra-hours-form.component').then(
            (m) => m.ExtraHoursFormComponent,
          ),
      },
    ],
  },

  // Mi Perfil — incluye cambio de contraseña (RF-005)
  {
    path: 'profile',
    loadComponent: () =>
      import('./profile/profile.component').then((m) => m.ProfileComponent),
  },

  // Estado de postulación (RF-015, RF-022) — solo visible si postulación activa
  {
    path: 'application',
    loadComponent: () =>
      import('./application/application.component').then((m) => m.ApplicationComponent),
  },
];
