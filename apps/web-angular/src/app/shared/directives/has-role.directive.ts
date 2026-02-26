import {
  Directive,
  InputSignal,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { UserRole } from '../../core/models/auth.models';

/**
 * Structural directive that conditionally renders its host element
 * based on the current user's role.
 *
 * Use this ONLY when a single view is shared across roles and a specific
 * section must be conditionally shown. Prefer role-separated routes over this
 * directive for full page/section separation.
 *
 * @example
 * ```html
 * <button *appHasRole="[UserRole.ADMIN, UserRole.SUPERVISOR]">
 *   Aprobar en lote
 * </button>
 * ```
 */
@Directive({
  selector: '[appHasRole]',
  standalone: true,
})
export class HasRoleDirective {
  /**
   * Array of UserRole values that are allowed to see the host element.
   * The element is rendered if the current user's role is in this list.
   */
  readonly appHasRole: InputSignal<UserRole[]> = input.required<UserRole[]>();

  private readonly authService = inject(AuthService);
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);

  private hasView = false;

  constructor() {
    // Use effect to react to role changes reactively via signals
    effect(() => {
      const allowedRoles = this.appHasRole();
      const currentRole = this.authService.currentUser()?.role;

      const shouldShow = currentRole !== undefined && allowedRoles.includes(currentRole);

      if (shouldShow && !this.hasView) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.hasView = true;
      } else if (!shouldShow && this.hasView) {
        this.viewContainer.clear();
        this.hasView = false;
      }
    });
  }
}
