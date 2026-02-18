import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../../core/services/auth.service';
import { MessageService } from 'primeng/api';
import { AuthProvider, UserDto, UserRole } from '../../../../core/models/auth.models';

const mockUser: UserDto = {
  id: '1',
  email: 'test@universidad.edu',
  firstName: 'Juan',
  lastName: 'Perez',
  fullName: 'Juan Perez',
  role: UserRole.BECA,
  photoUrl: null,
  isActive: true,
  lastLogin: null,
  authProvider: AuthProvider.Local,
};

const createMockAuthService = () => ({
  login: jasmine.createSpy().and.returnValue(of(mockUser)),
  loginWithGoogle: jasmine.createSpy(),
  navigateToDashboard: jasmine.createSpy(),
  getSafeReturnUrl: jasmine.createSpy().and.returnValue(''),
  getDefaultDashboardUrl: jasmine.createSpy().and.returnValue('/scholar/dashboard'),
  isLoading: signal(false),
  error: signal(null),
});

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: ReturnType<typeof createMockAuthService>;
  let mockMessageService: jasmine.SpyObj<MessageService>;

  beforeEach(async () => {
    mockAuthService = createMockAuthService();
    mockMessageService = jasmine.createSpyObj('MessageService', ['add']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: MessageService, useValue: mockMessageService },
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParams: {} } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should render the login form', () => {
      const form = fixture.nativeElement.querySelector('form');
      expect(form).toBeTruthy();
    });

    it('should render email input', () => {
      const emailInput = fixture.nativeElement.querySelector('#login-email');
      expect(emailInput).toBeTruthy();
    });

    it('should render the Google login button', () => {
      const googleBtn = fixture.nativeElement.querySelector('.login-card__google-btn');
      expect(googleBtn).toBeTruthy();
    });

    it('should render submit button', () => {
      const submitBtn = fixture.nativeElement.querySelector('button[type="submit"]');
      expect(submitBtn).toBeTruthy();
    });

    it('should NOT show lockout banner initially', () => {
      expect(component.showLockoutBanner()).toBeFalse();
    });

    it('should NOT show Google hint initially', () => {
      expect(component.showGoogleHint()).toBeFalse();
    });
  });

  describe('Form validation', () => {
    it('should have invalid form initially', () => {
      expect(component.form.invalid).toBeTrue();
    });

    it('should show email error when email is empty and touched', () => {
      component.emailControl.markAsTouched();
      component.emailControl.markAsDirty();
      fixture.detectChanges();

      expect(component.emailError).toBe('El email es requerido');
    });

    it('should show email format error for invalid email', () => {
      component.emailControl.setValue('not-an-email');
      component.emailControl.markAsTouched();
      component.emailControl.markAsDirty();
      fixture.detectChanges();

      expect(component.emailError).toBe('Ingresa un email válido');
    });

    it('should show password error when empty and touched', () => {
      component.passwordControl.markAsTouched();
      component.passwordControl.markAsDirty();
      fixture.detectChanges();

      expect(component.passwordError).toBe('La contraseña es requerida');
    });

    it('should be valid with correct email and password', () => {
      component.emailControl.setValue('test@universidad.edu');
      component.passwordControl.setValue('Pass123!');
      expect(component.form.valid).toBeTrue();
    });
  });

  describe('Form submission', () => {
    beforeEach(() => {
      component.emailControl.setValue('test@universidad.edu');
      component.passwordControl.setValue('Pass123!');
    });

    it('should call authService.login with form values', () => {
      component.onSubmit();

      expect(mockAuthService.login).toHaveBeenCalledWith({
        email: 'test@universidad.edu',
        password: 'Pass123!',
      });
    });

    it('should show success toast and navigate on successful login', () => {
      component.onSubmit();

      expect(mockMessageService.add).toHaveBeenCalledWith(
        jasmine.objectContaining({ severity: 'success' }),
      );
      expect(mockAuthService.navigateToDashboard).toHaveBeenCalledWith(UserRole.BECA);
    });

    it('should not submit when form is invalid', () => {
      component.emailControl.setValue('');
      component.onSubmit();

      expect(mockAuthService.login).not.toHaveBeenCalled();
    });

    it('should mark all fields as touched when submitting invalid form', () => {
      component.emailControl.setValue('');
      component.onSubmit();

      expect(component.emailControl.touched).toBeTrue();
      expect(component.passwordControl.touched).toBeTrue();
    });

    it('should show error toast on INVALID_CREDENTIALS error', () => {
      mockAuthService.login.and.returnValue(
        throwError(() => ({ code: 'INVALID_CREDENTIALS', message: 'Credenciales incorrectas' })),
      );

      component.onSubmit();

      expect(mockMessageService.add).toHaveBeenCalledWith(
        jasmine.objectContaining({ severity: 'error' }),
      );
    });

    it('should show lockout banner on ACCOUNT_LOCKED error', () => {
      mockAuthService.login.and.returnValue(
        throwError(() => ({ code: 'ACCOUNT_LOCKED', message: 'Cuenta bloqueada' })),
      );

      component.onSubmit();

      expect(component.showLockoutBanner()).toBeTrue();
    });

    it('should show Google hint on GOOGLE_ACCOUNT error', () => {
      mockAuthService.login.and.returnValue(
        throwError(() => ({ code: 'GOOGLE_ACCOUNT', message: 'Usa Google' })),
      );

      component.onSubmit();

      expect(component.showGoogleHint()).toBeTrue();
    });
  });

  describe('Google OAuth', () => {
    it('should call loginWithGoogle when Google button is clicked', () => {
      const googleBtn: HTMLButtonElement = fixture.nativeElement.querySelector(
        '.login-card__google-btn',
      );
      googleBtn.click();

      expect(mockAuthService.loginWithGoogle).toHaveBeenCalled();
    });
  });

  describe('Lockout countdown', () => {
    it('should format lockout time correctly', () => {
      expect(component.formatLockoutTime(900)).toBe('15:00');
      expect(component.formatLockoutTime(65)).toBe('1:05');
      expect(component.formatLockoutTime(30)).toBe('0:30');
    });
  });
});
