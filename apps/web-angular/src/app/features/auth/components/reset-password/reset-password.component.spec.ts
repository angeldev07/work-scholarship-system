import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ResetPasswordComponent } from './reset-password.component';
import { AuthService } from '../../../../core/services/auth.service';
import { MessageService } from 'primeng/api';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let mockAuthService: jasmine.SpyObj<Partial<AuthService>>;
  let mockMessageService: jasmine.SpyObj<MessageService>;

  const createWithToken = async (token: string | null) => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: MessageService, useValue: mockMessageService },
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams: { token } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  beforeEach(() => {
    mockAuthService = {
      resetPassword: jasmine.createSpy().and.returnValue(of(undefined)),
      isLoading: signal(false) as AuthService['isLoading'],
      error: signal(null) as AuthService['error'],
    };
    mockMessageService = jasmine.createSpyObj('MessageService', ['add']);
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  describe('Invalid token state', () => {
    beforeEach(async () => {
      await createWithToken(null);
    });

    it('should show invalid token message when no token in URL', () => {
      expect(component.invalidToken()).toBeTrue();
    });
  });

  describe('Valid token state', () => {
    beforeEach(async () => {
      await createWithToken('valid-reset-token-123');
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should store the token from query params', () => {
      expect(component.resetToken()).toBe('valid-reset-token-123');
    });

    it('should NOT show invalid token message when token present', () => {
      expect(component.invalidToken()).toBeFalse();
    });

    it('should show form initially', () => {
      expect(component.success()).toBeFalse();
    });

    describe('Password validation', () => {
      it('should require new password', () => {
        component.newPasswordControl.markAsTouched();
        component.newPasswordControl.markAsDirty();
        expect(component.newPasswordError).toContain('requerida');
      });

      it('should fail short passwords', () => {
        component.newPasswordControl.setValue('Aa1');
        component.newPasswordControl.markAsTouched();
        component.newPasswordControl.markAsDirty();
        expect(component.newPasswordError).toContain('8 caracteres');
      });

      it('should show password requirements when dirty', () => {
        component.newPasswordControl.setValue('');
        component.newPasswordControl.markAsDirty();
        fixture.detectChanges();

        const requirements = fixture.nativeElement.querySelector('.reset-card__requirements');
        expect(requirements).toBeTruthy();
      });

      it('should correctly compute password requirements', () => {
        component.newPasswordControl.setValue('abc');
        const req = component.requirements();
        expect(req.isLongEnough).toBeFalse();
        expect(req.hasUpperCase).toBeFalse();
        expect(req.hasLowerCase).toBeTrue();
        expect(req.hasNumber).toBeFalse();
      });

      it('should show all requirements met for valid password', () => {
        component.newPasswordControl.setValue('ValidPass1');
        const req = component.requirements();
        expect(req.isLongEnough).toBeTrue();
        expect(req.hasUpperCase).toBeTrue();
        expect(req.hasLowerCase).toBeTrue();
        expect(req.hasNumber).toBeTrue();
      });

      it('should detect password mismatch', () => {
        component.newPasswordControl.setValue('ValidPass1');
        component.confirmPasswordControl.setValue('DifferentPass1');
        component.confirmPasswordControl.markAsTouched();
        component.confirmPasswordControl.markAsDirty();

        expect(component.form.hasError('passwordMismatch')).toBeTrue();
        expect(component.confirmPasswordError).toContain('no coinciden');
      });

      it('should pass when passwords match', () => {
        component.newPasswordControl.setValue('ValidPass1');
        component.confirmPasswordControl.setValue('ValidPass1');
        expect(component.form.hasError('passwordMismatch')).toBeFalse();
      });
    });

    describe('Form submission', () => {
      beforeEach(() => {
        component.newPasswordControl.setValue('NewPass123!');
        component.confirmPasswordControl.setValue('NewPass123!');
      });

      it('should call resetPassword with token and passwords', () => {
        component.onSubmit();

        expect(mockAuthService.resetPassword).toHaveBeenCalledWith({
          token: 'valid-reset-token-123',
          newPassword: 'NewPass123!',
          confirmPassword: 'NewPass123!',
        });
      });

      it('should show success state on API success', () => {
        component.onSubmit();
        expect(component.success()).toBeTrue();
      });

      it('should show invalid token state on INVALID_TOKEN error', () => {
        (mockAuthService.resetPassword as jasmine.Spy).and.returnValue(
          throwError(() => ({ code: 'INVALID_TOKEN', message: 'Token inválido' })),
        );

        component.onSubmit();
        expect(component.invalidToken()).toBeTrue();
      });

      it('should not submit invalid form', () => {
        component.newPasswordControl.setValue('');
        component.onSubmit();
        expect(mockAuthService.resetPassword).not.toHaveBeenCalled();
      });
    });
  });
});
