import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HasRoleDirective } from './has-role.directive';
import { AuthService } from '../../core/services/auth.service';
import { UserRole, UserDto } from '../../core/models/auth.models';

// Test host component that uses the directive
@Component({
  standalone: true,
  imports: [HasRoleDirective],
  template: `
    <div *appHasRole="[UserRole.ADMIN]" class="admin-only">Admin content</div>
    <div *appHasRole="[UserRole.ADMIN, UserRole.SUPERVISOR]" class="admin-or-supervisor">
      Admin or Supervisor content
    </div>
    <div *appHasRole="[UserRole.BECA]" class="beca-only">Beca content</div>
  `,
})
class TestHostComponent {
  readonly UserRole = UserRole;
}

const mockAdminUser: UserDto = {
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

describe('HasRoleDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let currentUserSignal: ReturnType<typeof signal<UserDto | null>>;

  beforeEach(async () => {
    currentUserSignal = signal<UserDto | null>(null);

    const mockAuthService = {
      currentUser: currentUserSignal.asReadonly(),
    };

    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should create the directive without errors', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  describe('when user is null (not authenticated)', () => {
    beforeEach(() => {
      currentUserSignal.set(null);
      TestBed.flushEffects();
      fixture.detectChanges();
    });

    it('should NOT render any role-gated content', () => {
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();
      expect(fixture.nativeElement.querySelector('.admin-or-supervisor')).toBeNull();
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeNull();
    });
  });

  describe('when user is ADMIN', () => {
    beforeEach(() => {
      currentUserSignal.set({ ...mockAdminUser, role: UserRole.ADMIN });
      TestBed.flushEffects();
      fixture.detectChanges();
    });

    it('should render content for ADMIN-only sections', () => {
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeTruthy();
    });

    it('should render content for ADMIN+SUPERVISOR sections', () => {
      expect(fixture.nativeElement.querySelector('.admin-or-supervisor')).toBeTruthy();
    });

    it('should NOT render BECA-only content', () => {
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeNull();
    });
  });

  describe('when user is SUPERVISOR', () => {
    beforeEach(() => {
      currentUserSignal.set({ ...mockAdminUser, role: UserRole.SUPERVISOR });
      TestBed.flushEffects();
      fixture.detectChanges();
    });

    it('should NOT render ADMIN-only content', () => {
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();
    });

    it('should render ADMIN+SUPERVISOR content', () => {
      expect(fixture.nativeElement.querySelector('.admin-or-supervisor')).toBeTruthy();
    });

    it('should NOT render BECA-only content', () => {
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeNull();
    });
  });

  describe('when user is BECA', () => {
    beforeEach(() => {
      currentUserSignal.set({ ...mockAdminUser, role: UserRole.BECA });
      TestBed.flushEffects();
      fixture.detectChanges();
    });

    it('should NOT render ADMIN-only content', () => {
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();
    });

    it('should NOT render ADMIN+SUPERVISOR content', () => {
      expect(fixture.nativeElement.querySelector('.admin-or-supervisor')).toBeNull();
    });

    it('should render BECA-only content', () => {
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeTruthy();
    });
  });

  describe('Reactive updates', () => {
    it('should show content after user logs in as ADMIN', () => {
      currentUserSignal.set(null);
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();

      currentUserSignal.set({ ...mockAdminUser, role: UserRole.ADMIN });
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeTruthy();
    });

    it('should hide content after user logs out', () => {
      currentUserSignal.set({ ...mockAdminUser, role: UserRole.ADMIN });
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeTruthy();

      currentUserSignal.set(null);
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();
    });

    it('should swap content when role changes', () => {
      currentUserSignal.set({ ...mockAdminUser, role: UserRole.ADMIN });
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeNull();

      currentUserSignal.set({ ...mockAdminUser, role: UserRole.BECA });
      TestBed.flushEffects();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.admin-only')).toBeNull();
      expect(fixture.nativeElement.querySelector('.beca-only')).toBeTruthy();
    });
  });
});
