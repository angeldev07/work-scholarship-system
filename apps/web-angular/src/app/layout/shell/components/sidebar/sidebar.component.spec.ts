import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { SidebarComponent } from './sidebar.component';
import { NavConfig, PendingCounts } from '../../models/navigation.models';
import { UserRole } from '../../../../core/models/auth.models';

const mockNavConfig: NavConfig = [
  {
    items: [
      {
        id: 'dashboard',
        label: 'Dashboard',
        icon: 'chart-pie',
        route: '/admin/dashboard',
        roles: [UserRole.ADMIN],
      },
      {
        id: 'cycles',
        label: 'Ciclos',
        icon: 'calendar',
        roles: [UserRole.ADMIN],
        children: [
          {
            id: 'cycles-active',
            label: 'Ciclo Activo',
            icon: 'circle-fill',
            route: '/admin/cycles/active',
            roles: [UserRole.ADMIN],
          },
          {
            id: 'cycles-history',
            label: 'Historial',
            icon: 'history',
            route: '/admin/cycles/history',
            roles: [UserRole.ADMIN],
          },
        ],
      },
      {
        id: 'shifts',
        label: 'Jornadas',
        icon: 'clock',
        route: '/admin/shifts/pending',
        roles: [UserRole.ADMIN],
        badgeKey: 'shifts',
      },
    ],
  },
];

const mockPendingCounts: PendingCounts = {
  shifts: 3,
  absences: 1,
  applicants: 0,
};

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SidebarComponent, RouterTestingModule],
      providers: [
        provideAnimationsAsync(),
        providePrimeNG({ theme: { preset: Aura } }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('navConfig', mockNavConfig);
    fixture.componentRef.setInput('pendingCounts', mockPendingCounts);
    fixture.componentRef.setInput('collapsed', false);

    fixture.detectChanges();
  });

  describe('Rendering', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should render nav items from config', () => {
      const links = fixture.nativeElement.querySelectorAll('.sidebar__link, .sidebar__sublink');
      expect(links.length).toBeGreaterThan(0);
    });

    it('should render item labels when not collapsed', () => {
      const labels = fixture.nativeElement.querySelectorAll('.sidebar__label');
      expect(labels.length).toBeGreaterThan(0);
      const labelTexts = Array.from(labels).map((el: any) => el.textContent.trim());
      expect(labelTexts).toContain('Dashboard');
    });

    it('should show brand logo', () => {
      const brand = fixture.nativeElement.querySelector('.sidebar__brand');
      expect(brand).toBeTruthy();
    });

    it('should show brand name when not collapsed', () => {
      const brandName = fixture.nativeElement.querySelector('.sidebar__brand-name');
      expect(brandName).toBeTruthy();
    });

    it('should display badge when count > 0', () => {
      const badges = fixture.nativeElement.querySelectorAll('.sidebar__badge');
      expect(badges.length).toBeGreaterThan(0);
    });
  });

  describe('Collapsed mode', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('collapsed', true);
      fixture.detectChanges();
    });

    it('should hide labels in collapsed mode', () => {
      const labels = fixture.nativeElement.querySelectorAll('.sidebar__label');
      expect(labels.length).toBe(0);
    });

    it('should apply collapsed CSS class', () => {
      const sidebar = fixture.nativeElement.querySelector('.sidebar');
      expect(sidebar.classList).toContain('sidebar--collapsed');
    });

    it('should not expand submenu when clicking parent in collapsed mode', () => {
      const parentBtn = fixture.nativeElement.querySelector('[aria-expanded]');
      if (parentBtn) {
        parentBtn.click();
        fixture.detectChanges();
        const submenu = fixture.nativeElement.querySelector('.sidebar__submenu');
        expect(submenu).toBeNull();
      }
    });
  });

  describe('Accordion (sub-menu)', () => {
    it('should start with no submenus open', () => {
      const submenus = fixture.nativeElement.querySelectorAll('.sidebar__submenu');
      expect(submenus.length).toBe(0);
    });

    it('should expand submenu when clicking a parent item', () => {
      const parentBtn: HTMLButtonElement = fixture.nativeElement.querySelector('[aria-expanded]');
      expect(parentBtn).toBeTruthy();

      parentBtn.click();
      fixture.detectChanges();

      const submenu = fixture.nativeElement.querySelector('.sidebar__submenu');
      expect(submenu).toBeTruthy();
    });

    it('should collapse submenu when clicking expanded parent again', () => {
      const parentBtn: HTMLButtonElement = fixture.nativeElement.querySelector('[aria-expanded]');
      parentBtn.click();
      fixture.detectChanges();

      parentBtn.click();
      fixture.detectChanges();

      const submenu = fixture.nativeElement.querySelector('.sidebar__submenu');
      expect(submenu).toBeNull();
    });

    it('should show child items when submenu is expanded', () => {
      const parentBtn: HTMLButtonElement = fixture.nativeElement.querySelector('[aria-expanded]');
      parentBtn.click();
      fixture.detectChanges();

      const sublinks = fixture.nativeElement.querySelectorAll('.sidebar__sublink');
      expect(sublinks.length).toBeGreaterThan(0);
    });
  });

  describe('getBadgeCount', () => {
    it('should return correct badge count for item with badgeKey', () => {
      const shiftsItem = mockNavConfig[0].items[2]; // shifts item with badgeKey: 'shifts'
      const count = component.getBadgeCount(shiftsItem);
      expect(count).toBe(3);
    });

    it('should return 0 for item without badgeKey', () => {
      const dashboardItem = mockNavConfig[0].items[0];
      const count = component.getBadgeCount(dashboardItem);
      expect(count).toBe(0);
    });
  });

  describe('itemClicked output', () => {
    it('should emit itemClicked when a direct link item is clicked', () => {
      const emitted = jasmine.createSpy('itemClicked');
      component.itemClicked.subscribe(emitted);

      const directLink: HTMLAnchorElement = fixture.nativeElement.querySelector('a.sidebar__link');
      if (directLink) {
        directLink.click();
        expect(emitted).toHaveBeenCalled();
      }
    });
  });

  describe('isExpanded', () => {
    it('should return false for non-expanded items initially', () => {
      expect(component.isExpanded('cycles')).toBeFalse();
    });

    it('should return true after toggling an item', () => {
      component.toggleExpand('cycles');
      expect(component.isExpanded('cycles')).toBeTrue();
    });

    it('should return false after toggling twice', () => {
      component.toggleExpand('cycles');
      component.toggleExpand('cycles');
      expect(component.isExpanded('cycles')).toBeFalse();
    });
  });
});
