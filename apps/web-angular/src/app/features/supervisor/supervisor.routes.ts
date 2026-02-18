import { Routes } from '@angular/router';

export const supervisorRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/supervisor-dashboard.component').then(
        (m) => m.SupervisorDashboardComponent,
      ),
  },
];
