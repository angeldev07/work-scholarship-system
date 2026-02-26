import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { UserRole } from './core/models/auth.models';

export const routes: Routes = [
  // Root redirect — authenticated users are redirected by the login component
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'auth/login',
  },

  // Auth feature (lazy loaded) — guarded by guestGuard internally
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.authRoutes),
  },

  // ADMIN area — requires ADMIN role, uses shared ShellComponent as layout
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.ADMIN] },
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    loadChildren: () =>
      import('./features/admin/admin.routes').then((m) => m.adminRoutes),
  },

  // SUPERVISOR area — requires SUPERVISOR role
  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.SUPERVISOR] },
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    loadChildren: () =>
      import('./features/supervisor/supervisor.routes').then((m) => m.supervisorRoutes),
  },

  // SCHOLAR area — requires BECA role
  {
    path: 'scholar',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.BECA] },
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    loadChildren: () =>
      import('./features/scholar/scholar.routes').then((m) => m.scholarRoutes),
  },

  // Error pages
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./shared/components/forbidden/forbidden.component').then(
        (m) => m.ForbiddenComponent,
      ),
  },

  // Wildcard — 404
  {
    path: '**',
    loadComponent: () =>
      import('./shared/components/not-found/not-found.component').then(
        (m) => m.NotFoundComponent,
      ),
  },
];
