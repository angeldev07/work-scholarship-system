import { Routes } from '@angular/router';

export const scholarRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/scholar-dashboard.component').then((m) => m.ScholarDashboardComponent),
  },
];
