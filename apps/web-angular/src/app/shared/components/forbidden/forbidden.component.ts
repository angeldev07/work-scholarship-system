import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [],
  template: `
    <div class="error-page" role="main" aria-labelledby="forbidden-heading">
      <div class="error-page__content">
        <p class="error-page__code" aria-hidden="true">403</p>
        <h1 class="error-page__title" id="forbidden-heading">Acceso denegado</h1>
        <p class="error-page__text">No tienes permisos para acceder a esta sección.</p>
        <button
          type="button"
          class="error-page__link"
          (click)="goToDashboard()"
        >
          Ir a mi panel
        </button>
      </div>
    </div>
  `,
  styles: [`
    .error-page {
      display: flex; align-items: center; justify-content: center;
      min-height: 100dvh; padding: 2rem; background: #f8fafc;
    }
    .error-page__content { text-align: center; display: flex; flex-direction: column; gap: 1rem; align-items: center; }
    .error-page__code { font-size: 8rem; font-weight: 800; color: #fee2e2; line-height: 1; }
    .error-page__title { font-size: 1.875rem; font-weight: 700; color: #1e293b; }
    .error-page__text { color: #64748b; }
    .error-page__link {
      display: inline-flex; padding: 0.75rem 1.5rem; background: #4f46e5; color: white;
      border-radius: 0.75rem; font-weight: 600; text-decoration: none; cursor: pointer;
      border: none; font-size: 1rem; transition: all 200ms;
      &:hover { background: #4338ca; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForbiddenComponent {
  private readonly authService = inject(AuthService);

  goToDashboard(): void {
    const user = this.authService.currentUser();
    if (user) {
      this.authService.navigateToDashboard(user.role);
    }
  }
}
