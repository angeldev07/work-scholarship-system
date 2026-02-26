import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { NavigationService } from './navigation.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/auth.models';
import { UserDto } from '../../../core/models/auth.models';

const mockAdmin: UserDto = {
  id: '1',
  email: 'admin@test.com',
  firstName: 'Admin',
  lastName: 'User',
  fullName: 'Admin User',
  role: UserRole.ADMIN,
  photoUrl: null,
  isActive: true,
  lastLogin: null,
  authProvider: 'Local' as any,
};

const mockSupervisor: UserDto = { ...mockAdmin, id: '2', role: UserRole.SUPERVISOR };
const mockBeca: UserDto = { ...mockAdmin, id: '3', role: UserRole.BECA };

describe('NavigationService', () => {
  let service: NavigationService;
  let currentUserSignal: ReturnType<typeof signal<UserDto | null>>;

  function setupWithUser(user: UserDto | null): void {
    currentUserSignal.set(user);
    TestBed.flushEffects();
  }

  beforeEach(() => {
    currentUserSignal = signal<UserDto | null>(null);

    const mockAuthService = {
      currentUser: currentUserSignal.asReadonly(),
    };

    TestBed.configureTestingModule({
      providers: [
        NavigationService,
        { provide: AuthService, useValue: mockAuthService },
      ],
    });

    service = TestBed.inject(NavigationService);
  });

  describe('navItems computed signal', () => {
    it('should return empty config when user is null', () => {
      setupWithUser(null);
      const config = service.navItems();
      const totalItems = config.flatMap((g) => g.items).length;
      expect(totalItems).toBe(0);
    });

    it('should include ADMIN items for ADMIN role', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).toContain('admin-dashboard');
      expect(ids).toContain('admin-cycles');
      expect(ids).toContain('admin-selection');
      expect(ids).toContain('admin-locations');
      expect(ids).toContain('admin-shifts');
      expect(ids).toContain('admin-documents');
      expect(ids).toContain('admin-reports');
      expect(ids).toContain('admin-notifications');
      expect(ids).toContain('admin-users');
      expect(ids).toContain('admin-audit');
    });

    it('should NOT include SUPERVISOR items for ADMIN role', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).not.toContain('supervisor-dashboard');
      expect(ids).not.toContain('supervisor-shifts');
      expect(ids).not.toContain('supervisor-scholars');
    });

    it('should NOT include BECA items for ADMIN role', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).not.toContain('scholar-dashboard');
      expect(ids).not.toContain('scholar-shift');
    });

    it('should include SUPERVISOR items for SUPERVISOR role', () => {
      setupWithUser(mockSupervisor);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).toContain('supervisor-dashboard');
      expect(ids).toContain('supervisor-shifts');
      expect(ids).toContain('supervisor-absences');
      expect(ids).toContain('supervisor-scholars');
      expect(ids).toContain('supervisor-interviews');
      expect(ids).toContain('supervisor-logbook');
    });

    it('should NOT include ADMIN items for SUPERVISOR role', () => {
      setupWithUser(mockSupervisor);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).not.toContain('admin-dashboard');
      expect(ids).not.toContain('admin-cycles');
      expect(ids).not.toContain('admin-audit');
      expect(ids).not.toContain('admin-users');
    });

    it('should include BECA items for BECA role', () => {
      setupWithUser(mockBeca);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).toContain('scholar-dashboard');
      expect(ids).toContain('scholar-shift');
      expect(ids).toContain('scholar-hours');
      expect(ids).toContain('scholar-absences');
      expect(ids).toContain('scholar-extra-hours');
      expect(ids).toContain('scholar-profile');
      expect(ids).toContain('scholar-application');
    });

    it('should NOT include ADMIN or SUPERVISOR items for BECA role', () => {
      setupWithUser(mockBeca);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const ids = allItems.map((i) => i.id);

      expect(ids).not.toContain('admin-dashboard');
      expect(ids).not.toContain('supervisor-dashboard');
    });

    it('should preserve children within filtered items', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const cyclesItem = allItems.find((i) => i.id === 'admin-cycles');

      expect(cyclesItem).toBeTruthy();
      expect(cyclesItem!.children?.length).toBeGreaterThan(0);
    });

    it('should filter children based on role', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      const allItems = config.flatMap((g) => g.items);
      const cyclesItem = allItems.find((i) => i.id === 'admin-cycles');
      const childIds = cyclesItem!.children!.map((c) => c.id);

      expect(childIds).toContain('admin-cycles-active');
      expect(childIds).toContain('admin-cycles-history');
      // Supervisor-only children should NOT appear
      expect(childIds).not.toContain('supervisor-shifts-pending');
    });

    it('should emit config group for admin config section', () => {
      setupWithUser(mockAdmin);
      const config = service.navItems();
      // There should be at least 2 groups (main + config)
      expect(config.length).toBeGreaterThanOrEqual(2);
    });

    it('should reactively update when user changes', () => {
      setupWithUser(mockAdmin);
      let config = service.navItems();
      let allItems = config.flatMap((g) => g.items);
      expect(allItems.some((i) => i.id === 'admin-dashboard')).toBeTrue();

      setupWithUser(mockSupervisor);
      config = service.navItems();
      allItems = config.flatMap((g) => g.items);
      expect(allItems.some((i) => i.id === 'supervisor-dashboard')).toBeTrue();
      expect(allItems.some((i) => i.id === 'admin-dashboard')).toBeFalse();
    });
  });

  describe('pendingCounts computed signal', () => {
    it('should return all zeros initially', () => {
      const counts = service.pendingCounts();
      expect(counts.shifts).toBe(0);
      expect(counts.absences).toBe(0);
      expect(counts.applicants).toBe(0);
    });
  });

  describe('getLabelForRoute', () => {
    it('should return the label for a known direct route', () => {
      const label = service.getLabelForRoute('/admin/dashboard');
      expect(label).toBe('Dashboard');
    });

    it('should return the label for a known child route', () => {
      const label = service.getLabelForRoute('/admin/cycles/active');
      expect(label).toBe('Ciclo Activo');
    });

    it('should return null for an unknown route', () => {
      const label = service.getLabelForRoute('/unknown/route');
      expect(label).toBeNull();
    });
  });

  describe('getParentLabelForRoute', () => {
    it('should return the parent label for a child route', () => {
      const parent = service.getParentLabelForRoute('/admin/cycles/active');
      expect(parent).toBe('Ciclos');
    });

    it('should return null for a top-level route', () => {
      const parent = service.getParentLabelForRoute('/admin/dashboard');
      expect(parent).toBeNull();
    });
  });
});
