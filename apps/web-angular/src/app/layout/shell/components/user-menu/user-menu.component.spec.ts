import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { UserMenuComponent } from './user-menu.component';
import { AuthService } from '../../../../core/services/auth.service';
import { UserRole, UserDto } from '../../../../core/models/auth.models';

const mockAdmin: UserDto = {
  id: '1',
  email: 'admin@test.com',
  firstName: 'Ana',
  lastName: 'García',
  fullName: 'Ana García',
  role: UserRole.ADMIN,
  photoUrl: null,
  isActive: true,
  lastLogin: null,
  authProvider: 'Local' as any,
};

describe('UserMenuComponent', () => {
  let component: UserMenuComponent;
  let fixture: ComponentFixture<UserMenuComponent>;
  let currentUserSignal: ReturnType<typeof signal<UserDto | null>>;
  let mockLogout: jasmine.Spy;
  let router: Router;

  beforeEach(async () => {
    currentUserSignal = signal<UserDto | null>(mockAdmin);
    mockLogout = jasmine.createSpy('logout');

    const mockAuthService = {
      currentUser: currentUserSignal.asReadonly(),
      logout: mockLogout,
    };

    await TestBed.configureTestingModule({
      imports: [UserMenuComponent],
      providers: [
        provideAnimationsAsync(),
        providePrimeNG({ theme: { preset: Aura } }),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserMenuComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should display user first name', () => {
      const trigger: HTMLElement = fixture.nativeElement.querySelector('.user-menu__trigger');
      expect(trigger.textContent).toContain('Ana');
    });

    it('should display user role label', () => {
      const trigger: HTMLElement = fixture.nativeElement.querySelector('.user-menu__trigger');
      expect(trigger.textContent).toContain('Administrador');
    });
  });

  describe('userInitials computed', () => {
    it('should generate initials from first and last name', () => {
      expect(component.userInitials()).toBe('AG');
    });

    it('should handle single-word name', () => {
      currentUserSignal.set({ ...mockAdmin, fullName: 'Admin' });
      TestBed.flushEffects();
      expect(component.userInitials()).toBe('AD');
    });

    it('should return ? when user is null', () => {
      currentUserSignal.set(null);
      TestBed.flushEffects();
      expect(component.userInitials()).toBe('?');
    });
  });

  describe('roleLabel computed', () => {
    it('should return Administrador for ADMIN role', () => {
      expect(component.roleLabel()).toBe('Administrador');
    });

    it('should return Supervisor for SUPERVISOR role', () => {
      currentUserSignal.set({ ...mockAdmin, role: UserRole.SUPERVISOR });
      TestBed.flushEffects();
      expect(component.roleLabel()).toBe('Supervisor');
    });

    it('should return Estudiante Becado for BECA role', () => {
      currentUserSignal.set({ ...mockAdmin, role: UserRole.BECA });
      TestBed.flushEffects();
      expect(component.roleLabel()).toBe('Estudiante Becado');
    });

    it('should return empty string for unknown role', () => {
      currentUserSignal.set(null);
      TestBed.flushEffects();
      expect(component.roleLabel()).toBe('');
    });
  });

  describe('profileRoute computed', () => {
    it('should return admin route for ADMIN', () => {
      expect(component.profileRoute()).toBe('/admin/dashboard');
    });

    it('should return scholar profile route for BECA', () => {
      currentUserSignal.set({ ...mockAdmin, role: UserRole.BECA });
      TestBed.flushEffects();
      expect(component.profileRoute()).toBe('/scholar/profile');
    });
  });

  describe('logout', () => {
    it('should call authService.logout when logout is triggered', () => {
      component.logout();
      expect(mockLogout).toHaveBeenCalled();
    });
  });

  describe('navigateTo', () => {
    it('should navigate to the given route', () => {
      const navigateSpy = spyOn(router, 'navigate');
      component.navigateTo('/admin/dashboard');
      expect(navigateSpy).toHaveBeenCalledWith(['/admin/dashboard']);
    });
  });

  describe('Accessibility', () => {
    it('should have aria-haspopup on trigger button', () => {
      const trigger: HTMLElement = fixture.nativeElement.querySelector('.user-menu__trigger');
      expect(trigger.getAttribute('aria-haspopup')).toBe('true');
    });

    it('should have aria-label with user name', () => {
      const trigger: HTMLElement = fixture.nativeElement.querySelector('.user-menu__trigger');
      expect(trigger.getAttribute('aria-label')).toContain('Ana García');
    });
  });
});
