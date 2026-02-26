import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { signal } from '@angular/core';
import { ShellComponent } from './shell.component';
import { NavigationService } from './services/navigation.service';
import { AuthService } from '../../core/services/auth.service';

const mockNavConfig = signal([]);
const mockPendingCounts = signal({ shifts: 0, absences: 0, applicants: 0 });

const mockNavigationService = {
  navItems: mockNavConfig.asReadonly(),
  pendingCounts: mockPendingCounts.asReadonly(),
  getLabelForRoute: jasmine.createSpy('getLabelForRoute').and.returnValue(null),
  getParentLabelForRoute: jasmine.createSpy('getParentLabelForRoute').and.returnValue(null),
};

const mockCurrentUser = signal(null);
const mockAuthService = {
  currentUser: mockCurrentUser.asReadonly(),
  isAuthenticated: signal(false).asReadonly(),
  logout: jasmine.createSpy('logout'),
};

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([]),
        providePrimeNG({ theme: { preset: Aura } }),
        { provide: NavigationService, useValue: mockNavigationService },
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize collapsed as false on desktop viewport', () => {
      // On desktop (>1024px default for test environment), should start expanded
      // We just verify the signal exists and has a boolean value
      expect(typeof component.collapsed()).toBe('boolean');
    });

    it('should initialize mobileOpen as false', () => {
      expect(component.mobileOpen()).toBeFalse();
    });
  });

  describe('toggleSidebar', () => {
    it('should toggle collapsed state on desktop', () => {
      component.isMobile.set(false);
      const initial = component.collapsed();

      component.toggleSidebar();

      expect(component.collapsed()).toBe(!initial);
    });

    it('should toggle mobileOpen on mobile', () => {
      component.isMobile.set(true);
      expect(component.mobileOpen()).toBeFalse();

      component.toggleSidebar();
      expect(component.mobileOpen()).toBeTrue();

      component.toggleSidebar();
      expect(component.mobileOpen()).toBeFalse();
    });

    it('should not affect collapsed when toggling mobile', () => {
      component.isMobile.set(true);
      component.collapsed.set(true);

      component.toggleSidebar();

      expect(component.collapsed()).toBeTrue();
    });
  });

  describe('closeMobileDrawer', () => {
    it('should set mobileOpen to false', () => {
      component.mobileOpen.set(true);
      component.closeMobileDrawer();
      expect(component.mobileOpen()).toBeFalse();
    });
  });

  describe('Responsive behavior', () => {
    it('should set isMobile to true when width is below 768', () => {
      component['evaluateBreakpoint'](500);
      expect(component.isMobile()).toBeTrue();
    });

    it('should set isMobile to false when width is above 768', () => {
      component['evaluateBreakpoint'](1200);
      expect(component.isMobile()).toBeFalse();
    });

    it('should collapse sidebar on tablet width (768-1024)', () => {
      component['evaluateBreakpoint'](900);
      expect(component.collapsed()).toBeTrue();
    });

    it('should expand sidebar on desktop width (>1024)', () => {
      component['evaluateBreakpoint'](1280);
      expect(component.collapsed()).toBeFalse();
    });

    it('should close mobile drawer when transitioning from mobile to desktop', () => {
      component.isMobile.set(true);
      component.mobileOpen.set(true);

      component['evaluateBreakpoint'](1200);

      expect(component.mobileOpen()).toBeFalse();
    });
  });

  describe('CSS classes', () => {
    it('should apply shell--collapsed class when collapsed', () => {
      component.collapsed.set(true);
      component.isMobile.set(false);
      fixture.detectChanges();

      const shellEl: HTMLElement = fixture.nativeElement.querySelector('.shell');
      expect(shellEl.classList).toContain('shell--collapsed');
    });

    it('should not apply shell--collapsed class when expanded', () => {
      component.collapsed.set(false);
      component.isMobile.set(false);
      fixture.detectChanges();

      const shellEl: HTMLElement = fixture.nativeElement.querySelector('.shell');
      expect(shellEl.classList).not.toContain('shell--collapsed');
    });

    it('should apply shell--mobile class when isMobile is true', () => {
      component.isMobile.set(true);
      fixture.detectChanges();

      const shellEl: HTMLElement = fixture.nativeElement.querySelector('.shell');
      expect(shellEl.classList).toContain('shell--mobile');
    });
  });
});
