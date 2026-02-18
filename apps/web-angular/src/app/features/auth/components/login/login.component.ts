import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DividerModule } from 'primeng/divider';
import { CheckboxModule } from 'primeng/checkbox';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { AuthService } from '../../../../core/services/auth.service';
import { AUTH_ERROR_CODES, ApiError } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    InputTextModule,
    PasswordModule,
    DividerModule,
    CheckboxModule,
    ProgressSpinnerModule,
    TooltipModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  // ─── State ──────────────────────────────────────────────────────────────────
  readonly isLoading = this.authService.isLoading;
  readonly apiError = this.authService.error;

  readonly showGoogleHint = signal(false);
  readonly showLockoutBanner = signal(false);
  readonly lockoutSeconds = signal(0);

  private lockoutInterval: ReturnType<typeof setInterval> | null = null;
  private returnUrl = '';

  // ─── Form ────────────────────────────────────────────────────────────────────
  readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  readonly emailControl = this.form.controls.email;
  readonly passwordControl = this.form.controls.password;

  // ─── Error message getters ────────────────────────────────────────────────────
  get emailError(): string {
    const ctrl = this.emailControl;
    if (!ctrl.dirty && !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'El email es requerido';
    if (ctrl.hasError('email')) return 'Ingresa un email válido';
    return '';
  }

  get passwordError(): string {
    const ctrl = this.passwordControl;
    if (!ctrl.dirty && !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'La contraseña es requerida';
    return '';
  }

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '';

    // Check for OAuth error from query params
    const oauthError = this.route.snapshot.queryParams['error'];
    if (oauthError) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error de autenticación',
        detail: this.getOAuthErrorMessage(oauthError),
        life: 6000,
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.isLoading()) return;

    const { email, password } = this.form.value;

    this.showGoogleHint.set(false);
    this.showLockoutBanner.set(false);

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (user) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Bienvenido',
          detail: `Hola, ${user.firstName}`,
          life: 3000,
        });
        const safeUrl = this.authService.getSafeReturnUrl(this.returnUrl);
        if (safeUrl && safeUrl !== this.authService.getDefaultDashboardUrl()) {
          this.router.navigateByUrl(safeUrl);
        } else {
          this.authService.navigateToDashboard(user.role);
        }
      },
      error: (error: ApiError) => {
        this.handleLoginError(error);
      },
    });
  }

  onGoogleLogin(): void {
    this.authService.loginWithGoogle();
  }

  private handleLoginError(error: ApiError): void {
    switch (error.code) {
      case AUTH_ERROR_CODES.INVALID_CREDENTIALS:
        this.messageService.add({
          severity: 'error',
          summary: 'Credenciales incorrectas',
          detail: 'El email o la contraseña son incorrectos',
          life: 5000,
        });
        // Clear password for security
        this.passwordControl.reset();
        break;

      case AUTH_ERROR_CODES.ACCOUNT_LOCKED:
        this.showLockoutBanner.set(true);
        this.startLockoutCountdown(15 * 60);
        break;

      case AUTH_ERROR_CODES.GOOGLE_ACCOUNT:
        this.showGoogleHint.set(true);
        this.messageService.add({
          severity: 'warn',
          summary: 'Cuenta de Google',
          detail: 'Esta cuenta fue creada con Google. Usa el botón de Google para ingresar.',
          life: 7000,
        });
        break;

      default:
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.message || 'Error al iniciar sesión. Intenta nuevamente.',
          life: 5000,
        });
    }
  }

  private startLockoutCountdown(seconds: number): void {
    this.lockoutSeconds.set(seconds);
    this.lockoutInterval = setInterval(() => {
      const remaining = this.lockoutSeconds() - 1;
      if (remaining <= 0) {
        this.lockoutSeconds.set(0);
        this.showLockoutBanner.set(false);
        if (this.lockoutInterval) {
          clearInterval(this.lockoutInterval);
        }
      } else {
        this.lockoutSeconds.set(remaining);
      }
    }, 1000);
  }

  formatLockoutTime(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  private getOAuthErrorMessage(errorCode: string): string {
    switch (errorCode) {
      case 'oauth_failed':
        return 'Error al autenticar con Google. Por favor intenta nuevamente.';
      case 'invalid_domain':
        return 'Solo se permiten cuentas institucionales.';
      case 'oauth_cancelled':
        return 'Autenticación cancelada.';
      default:
        return 'Error de autenticación.';
    }
  }

  ngOnDestroy(): void {
    if (this.lockoutInterval) {
      clearInterval(this.lockoutInterval);
    }
  }
}
