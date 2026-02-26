import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, NavigationEnd, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { signal } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { TopbarComponent } from './topbar.component';
import { NavigationService } from '../../services/navigation.service';
import { AuthService } from '../../../../core/services/auth.service';

describe('TopbarComponent', () => {
  let component: TopbarComponent;
  let fixture: ComponentFixture<TopbarComponent>;
  let routerEvents$: Subject<any>;

  const mockCurrentUser = signal(null);
  const mockAuthService = {
    currentUser: mockCurrentUser.asReadonly(),
    logout: jasmine.createSpy('logout'),
  };

  const mockNavigationService = {
    getLabelForRoute: jasmine.createSpy('getLabelForRoute').and.returnValue(null),
    getParentLabelForRoute: jasmine.createSpy('getParentLabelForRoute').and.returnValue(null),
  };

  beforeEach(async () => {
    routerEvents$ = new Subject<any>();

    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: [
        provideAnimationsAsync(),
        providePrimeNG({ theme: { preset: Aura } }),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: NavigationService, useValue: mockNavigationService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TopbarComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('collapsed', false);
    fixture.componentRef.setInput('isMobile', false);
    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should render the toggle button', () => {
      const toggleBtn = fixture.nativeElement.querySelector('.topbar__toggle');
      expect(toggleBtn).toBeTruthy();
    });

    it('should render the notification bell', () => {
      const bell = fixture.nativeElement.querySelector('.topbar__action-btn');
      expect(bell).toBeTruthy();
    });

    it('should render the user menu', () => {
      const userMenu = fixture.nativeElement.querySelector('app-user-menu');
      expect(userMenu).toBeTruthy();
    });
  });

  describe('Toggle button', () => {
    it('should emit toggleSidebar event when toggle button is clicked', () => {
      const emitted = jasmine.createSpy('toggleSidebar');
      component.toggleSidebar.subscribe(emitted);

      const toggleBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.topbar__toggle');
      toggleBtn.click();

      expect(emitted).toHaveBeenCalled();
    });

    it('should have correct aria-label when collapsed', () => {
      fixture.componentRef.setInput('collapsed', true);
      fixture.detectChanges();

      const toggleBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.topbar__toggle');
      expect(toggleBtn.getAttribute('aria-label')).toBe('Expandir menú lateral');
    });

    it('should have correct aria-label when expanded', () => {
      fixture.componentRef.setInput('collapsed', false);
      fixture.detectChanges();

      const toggleBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.topbar__toggle');
      expect(toggleBtn.getAttribute('aria-label')).toBe('Colapsar menú lateral');
    });
  });

  describe('Breadcrumb', () => {
    it('should show breadcrumb when there are items', () => {
      component.breadcrumbs.set([{ label: 'Dashboard' }]);
      fixture.detectChanges();

      const breadcrumb = fixture.nativeElement.querySelector('.topbar__breadcrumb');
      expect(breadcrumb).toBeTruthy();
    });

    it('should display current breadcrumb label', () => {
      component.breadcrumbs.set([{ label: 'Ciclos' }, { label: 'Ciclo Activo' }]);
      fixture.detectChanges();

      const breadcrumbEl = fixture.nativeElement.querySelector('.topbar__breadcrumb');
      expect(breadcrumbEl.textContent).toContain('Ciclos');
      expect(breadcrumbEl.textContent).toContain('Ciclo Activo');
    });

    it('should show separator between breadcrumb items', () => {
      component.breadcrumbs.set([{ label: 'Ciclos' }, { label: 'Ciclo Activo' }]);
      fixture.detectChanges();

      const separators = fixture.nativeElement.querySelectorAll('.topbar__breadcrumb-separator');
      expect(separators.length).toBe(1);
    });
  });

  describe('buildBreadcrumbs (private)', () => {
    it('should use label from NavigationService when route is known', () => {
      mockNavigationService.getLabelForRoute.and.returnValue('Dashboard');
      mockNavigationService.getParentLabelForRoute.and.returnValue(null);

      const crumbs = (component as any).buildBreadcrumbs('/admin/dashboard');
      expect(crumbs.length).toBe(1);
      expect(crumbs[0].label).toBe('Dashboard');
    });

    it('should include parent label when child route is active', () => {
      mockNavigationService.getLabelForRoute.and.returnValue('Ciclo Activo');
      mockNavigationService.getParentLabelForRoute.and.returnValue('Ciclos');

      const crumbs = (component as any).buildBreadcrumbs('/admin/cycles/active');
      expect(crumbs.length).toBe(2);
      expect(crumbs[0].label).toBe('Ciclos');
      expect(crumbs[1].label).toBe('Ciclo Activo');
    });

    it('should humanize unknown route segments', () => {
      mockNavigationService.getLabelForRoute.and.returnValue(null);
      mockNavigationService.getParentLabelForRoute.and.returnValue(null);

      const crumbs = (component as any).buildBreadcrumbs('/admin/some-unknown');
      expect(crumbs[0].label).toBe('Some Unknown');
    });
  });
});
