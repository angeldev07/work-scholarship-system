import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { PasswordModule } from 'primeng/password';
import { AuthService } from '../../../../core/services/auth.service';
import { AUTH_ERROR_CODES } from '../../../../core/models/auth.models';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ButtonModule,
    PasswordModule,
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  readonly isLoading = this.authService.isLoading;
  readonly resetToken = signal('');
  readonly invalidToken = signal(false);
  readonly success = signal(false);
  private redirectCountdown = 3;

  readonly form = this.fb.group(
    {
      newPassword: [
        '',
        [Validators.required, Validators.minLength(8), this.passwordStrengthValidator()],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: this.passwordMatchValidator() },
  );

  get newPasswordControl() {
    return this.form.controls.newPassword;
  }

  get confirmPasswordControl() {
    return this.form.controls.confirmPassword;
  }

  readonly passwordValue = computed(() => this.newPasswordControl.value ?? '');

  readonly requirements = computed(() => {
    const v = this.passwordValue();
    return {
      isLongEnough: v.length >= 8,
      hasUpperCase: /[A-Z]/.test(v),
      hasLowerCase: /[a-z]/.test(v),
      hasNumber: /\d/.test(v),
    };
  });

  get newPasswordError(): string {
    const ctrl = this.newPasswordControl;
    if (!ctrl.dirty && !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'La contraseña es requerida';
    if (ctrl.hasError('minlength')) return 'La contraseña debe tener al menos 8 caracteres';
    if (ctrl.hasError('passwordStrength'))
      return 'La contraseña no cumple los requisitos de seguridad';
    return '';
  }

  get confirmPasswordError(): string {
    const ctrl = this.confirmPasswordControl;
    if (!ctrl.dirty && !ctrl.touched) return '';
    if (ctrl.hasError('required')) return 'Confirma tu contraseña';
    if (this.form.hasError('passwordMismatch')) return 'Las contraseñas no coinciden';
    return '';
  }

  ngOnInit(): void {
    const token = this.route.snapshot.queryParams['token'];
    if (!token) {
      this.invalidToken.set(true);
    } else {
      this.resetToken.set(token);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (this.isLoading()) return;

    this.authService
      .resetPassword({
        token: this.resetToken(),
        newPassword: this.newPasswordControl.value!,
        confirmPassword: this.confirmPasswordControl.value!,
      })
      .subscribe({
        next: () => {
          this.success.set(true);
          const interval = setInterval(() => {
            this.redirectCountdown--;
            if (this.redirectCountdown <= 0) {
              clearInterval(interval);
              this.router.navigate(['/auth/login']);
            }
          }, 1000);
        },
        error: (err) => {
          if (err.code === AUTH_ERROR_CODES.INVALID_TOKEN) {
            this.invalidToken.set(true);
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err.message || 'Error al restablecer contraseña',
              life: 5000,
            });
          }
        },
      });
  }

  private passwordStrengthValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value as string;
      if (!value) return null;

      const hasUpperCase = /[A-Z]/.test(value);
      const hasLowerCase = /[a-z]/.test(value);
      const hasNumber = /\d/.test(value);
      const isLongEnough = value.length >= 8;

      const valid = hasUpperCase && hasLowerCase && hasNumber && isLongEnough;
      return valid ? null : { passwordStrength: { hasUpperCase, hasLowerCase, hasNumber, isLongEnough } };
    };
  }

  private passwordMatchValidator(): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
      const password = formGroup.get('newPassword')?.value as string;
      const confirm = formGroup.get('confirmPassword')?.value as string;
      return password === confirm ? null : { passwordMismatch: true };
    };
  }
}
