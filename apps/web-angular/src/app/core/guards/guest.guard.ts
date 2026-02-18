import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = (): boolean => {
  const authService = inject(AuthService);

  if (authService.isAuthenticated()) {
    const user = authService.currentUser()!;
    authService.navigateToDashboard(user.role);
    return false;
  }

  return true;
};
