import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { UserRole } from './core/models/auth.models';

export const routes: Routes = [
  // Root redirect
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'auth/login',
  },

  // Auth feature (lazy loaded)
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.authRoutes),
  },

  // Admin area — requires ADMIN role
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.ADMIN] },
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.adminRoutes),
  },

  // Supervisor area — requires SUPERVISOR role
  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.SUPERVISOR] },
    loadChildren: () =>
      import('./features/supervisor/supervisor.routes').then((m) => m.supervisorRoutes),
  },

  // Scholar area — requires BECA role
  {
    path: 'scholar',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.BECA] },
    loadChildren: () =>
      import('./features/scholar/scholar.routes').then((m) => m.scholarRoutes),
  },

  // Forbidden page
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./shared/components/forbidden/forbidden.component').then(
        (m) => m.ForbiddenComponent,
      ),
  },

  // Wildcard
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component').then(
        (m) => m.NotFoundComponent,
      ),
  },
];
