import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { ForgotPasswordComponent } from './forgot-password.component';
import { AuthService } from '../../../../core/services/auth.service';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let mockAuthService: jasmine.SpyObj<Partial<AuthService>>;

  beforeEach(async () => {
    mockAuthService = {
      forgotPassword: jasmine.createSpy().and.returnValue(of(undefined)),
      isLoading: signal(false) as AuthService['isLoading'],
      error: signal(null) as AuthService['error'],
    };

    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should show form initially', () => {
      expect(component.submitted()).toBeFalse();
    });

    it('should show email input', () => {
      const emailInput = fixture.nativeElement.querySelector('#forgot-email');
      expect(emailInput).toBeTruthy();
    });

    it('should show success state after submission', () => {
      component.submitted.set(true);
      fixture.detectChanges();

      const successEl = fixture.nativeElement.querySelector('.forgot-card__success');
      expect(successEl).toBeTruthy();
    });
  });

  describe('Form validation', () => {
    it('should require email field', () => {
      component.emailControl.markAsTouched();
      component.emailControl.markAsDirty();
      expect(component.emailError).toBe('El email es requerido');
    });

    it('should validate email format', () => {
      component.emailControl.setValue('invalid-email');
      component.emailControl.markAsTouched();
      component.emailControl.markAsDirty();
      expect(component.emailError).toBe('Ingresa un email válido');
    });

    it('should accept valid email', () => {
      component.emailControl.setValue('user@universidad.edu');
      expect(component.form.valid).toBeTrue();
    });
  });

  describe('Form submission', () => {
    it('should call forgotPassword with email', () => {
      component.emailControl.setValue('user@universidad.edu');
      component.onSubmit();

      expect(mockAuthService.forgotPassword).toHaveBeenCalledWith({
        email: 'user@universidad.edu',
      });
    });

    it('should show success state on API success', () => {
      component.emailControl.setValue('user@universidad.edu');
      component.onSubmit();

      expect(component.submitted()).toBeTrue();
    });

    it('should show success state even on API error (security: prevent email enumeration)', () => {
      (mockAuthService.forgotPassword as jasmine.Spy).and.returnValue(
        throwError(() => ({ code: 'SERVER_ERROR', message: 'Error' })),
      );
      component.emailControl.setValue('user@universidad.edu');
      component.onSubmit();

      expect(component.submitted()).toBeTrue();
    });

    it('should not submit invalid form', () => {
      component.onSubmit();
      expect(mockAuthService.forgotPassword).not.toHaveBeenCalled();
    });
  });
});
