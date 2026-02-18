import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule, ProgressSpinnerModule],
  template: `
    <div class="oauth-callback" role="status" aria-live="polite" aria-label="Procesando autenticación con Google">
      @if (error()) {
        <div class="oauth-callback__error">
          <i class="pi pi-times-circle" aria-hidden="true"></i>
          <p>{{ error() }}</p>
          <a href="/auth/login" class="oauth-callback__link">Volver al inicio de sesión</a>
        </div>
      } @else {
        <p-progressSpinner
          strokeWidth="4"
          fill="transparent"
          animationDuration=".7s"
          [style]="{ width: '48px', height: '48px' }"
        />
        <p class="oauth-callback__text">Procesando autenticación...</p>
      }
    </div>
  `,
  styles: [`
    .oauth-callback {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 100dvh;
      gap: 1.5rem;
      padding: 2rem;
    }
    .oauth-callback__text {
      color: #64748b;
      font-size: 1rem;
    }
    .oauth-callback__error {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      text-align: center;
      color: #dc2626;
      i { font-size: 2.5rem; }
      p { font-size: 1rem; color: #64748b; }
    }
    .oauth-callback__link {
      color: #4f46e5;
      text-decoration: underline;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OAuthCallbackComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // Extract token from URL fragment (hash)
    const fragment = window.location.hash.substring(1);
    const params = new URLSearchParams(fragment);
    const accessToken = params.get('access_token');
    const oauthError = params.get('error');

    if (accessToken) {
      if (window.opener) {
        // Popup mode: send token to parent window
        window.opener.postMessage(
          { type: 'GOOGLE_AUTH_SUCCESS', accessToken },
          window.location.origin,
        );
        window.close();
      } else {
        // Full-page redirect mode: handle directly
        this.authService.setAccessToken(accessToken);
        this.authService.getCurrentUser().subscribe({
          next: (user) => {
            this.authService.navigateToDashboard(user.role);
          },
          error: () => {
            this.error.set('Error al obtener datos del usuario. Por favor intenta nuevamente.');
          },
        });
      }
    } else if (oauthError) {
      if (window.opener) {
        window.opener.postMessage(
          { type: 'GOOGLE_AUTH_ERROR', error: oauthError },
          window.location.origin,
        );
        window.close();
      } else {
        this.router.navigate(['/auth/login'], {
          queryParams: { error: oauthError },
        });
      }
    } else {
      // No token, no error — malformed URL
      this.router.navigate(['/auth/login']);
    }
  }
}
