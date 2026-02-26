import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  computed,
  inject,
} from '@angular/core';
import { Router } from '@angular/router';
import { AvatarModule } from 'primeng/avatar';
import { PopoverModule } from 'primeng/popover';
import { DividerModule } from 'primeng/divider';
import { Popover } from 'primeng/popover';
import { AuthService } from '../../../../core/services/auth.service';
import { UserRole } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [AvatarModule, PopoverModule, DividerModule],
  templateUrl: './user-menu.component.html',
  styleUrl: './user-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserMenuComponent {
  @ViewChild('userPopover') userPopover!: Popover;

  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;

  /** User's initials derived from full name */
  readonly userInitials = computed(() => {
    const user = this.currentUser();
    if (!user) return '?';
    const parts = user.fullName.trim().split(' ');
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
    }
    return parts[0].substring(0, 2).toUpperCase();
  });

  /** Human-readable role label */
  readonly roleLabel = computed(() => {
    switch (this.currentUser()?.role) {
      case UserRole.ADMIN:       return 'Administrador';
      case UserRole.SUPERVISOR:  return 'Supervisor';
      case UserRole.BECA:        return 'Estudiante Becado';
      default:                   return '';
    }
  });

  /** Route to the user's own profile */
  readonly profileRoute = computed(() => {
    switch (this.currentUser()?.role) {
      case UserRole.ADMIN:      return '/admin/dashboard'; // admin has no dedicated profile page yet
      case UserRole.SUPERVISOR: return '/supervisor/dashboard';
      case UserRole.BECA:       return '/scholar/profile';
      default:                  return '/';
    }
  });

  togglePopover(event: MouseEvent): void {
    this.userPopover.toggle(event);
  }

  navigateTo(route: string): void {
    this.userPopover.hide();
    this.router.navigate([route]);
  }

  logout(): void {
    this.userPopover.hide();
    this.authService.logout();
  }
}
