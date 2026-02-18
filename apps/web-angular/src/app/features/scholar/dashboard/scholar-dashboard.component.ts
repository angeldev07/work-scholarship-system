import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-scholar-dashboard',
  standalone: true,
  imports: [ButtonModule],
  template: `
    <div class="dashboard-placeholder">
      <div class="dashboard-placeholder__content">
        <h1>Mi Panel</h1>
        <p>Bienvenido, <strong>{{ authService.currentUser()?.fullName }}</strong></p>
        <p class="dashboard-placeholder__role-badge">ROL: BECA</p>
        <p class="dashboard-placeholder__note">
          Este es un componente placeholder. El dashboard completo con check-in/out y seguimiento de horas se implementará en la siguiente fase.
        </p>
        <button pButton type="button" label="Cerrar sesión" icon="pi pi-sign-out"
                class="p-button-secondary" (click)="authService.logout()">
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100dvh;
      padding: 2rem;
      background: #f8fafc;
    }
    .dashboard-placeholder__content {
      text-align: center;
      max-width: 480px;
      display: flex;
      flex-direction: column;
      gap: 1rem;
      h1 { font-size: 2rem; color: #1e293b; }
      p { color: #64748b; }
    }
    .dashboard-placeholder__role-badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      background: #15803d;
      color: white;
      border-radius: 9999px;
      font-size: 0.75rem;
      font-weight: 600;
      letter-spacing: 0.05em;
    }
    .dashboard-placeholder__note {
      font-size: 0.875rem;
      color: #94a3b8;
      font-style: italic;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarDashboardComponent {
  readonly authService = inject(AuthService);
}
