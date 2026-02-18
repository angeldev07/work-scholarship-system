import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router } from '@angular/router';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { roleGuard } from './role.guard';
import { AuthService } from '../services/auth.service';
import { UserDto, UserRole, AuthProvider } from '../models/auth.models';

const createMockUser = (role: UserRole): UserDto => ({
  id: '1',
  email: 'test@universidad.edu',
  firstName: 'Test',
  lastName: 'User',
  fullName: 'Test User',
  role,
  photoUrl: null,
  isActive: true,
  lastLogin: null,
  authProvider: AuthProvider.Local,
});

describe('roleGuard', () => {
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['currentUser', 'navigateToDashboard'], {
      currentUser: jasmine.createSpy().and.returnValue(null),
    });

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        provideRouter([]),
      ],
    });

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  const createMockRoute = (roles?: UserRole[]): ActivatedRouteSnapshot => {
    const route = new ActivatedRouteSnapshot();
    (route as unknown as Record<string, unknown>)['data'] = roles ? { roles } : {};
    return route;
  };

  const runGuard = (user: UserDto | null, roles?: UserRole[]): boolean => {
    (mockAuthService.currentUser as jasmine.Spy).and.returnValue(user);
    return TestBed.runInInjectionContext(() =>
      roleGuard(createMockRoute(roles), {} as never) as boolean,
    );
  };

  it('should redirect to login if no user', () => {
    const result = runGuard(null, [UserRole.ADMIN]);
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });

  it('should allow access when no roles specified', () => {
    const result = runGuard(createMockUser(UserRole.BECA));
    expect(result).toBeTrue();
  });

  it('should allow ADMIN to access ADMIN route', () => {
    const result = runGuard(createMockUser(UserRole.ADMIN), [UserRole.ADMIN]);
    expect(result).toBeTrue();
  });

  it('should deny BECA from accessing ADMIN route', () => {
    const result = runGuard(createMockUser(UserRole.BECA), [UserRole.ADMIN]);
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/forbidden']);
  });

  it('should allow SUPERVISOR to access SUPERVISOR route', () => {
    const result = runGuard(createMockUser(UserRole.SUPERVISOR), [UserRole.SUPERVISOR]);
    expect(result).toBeTrue();
  });

  it('should deny ADMIN from accessing SUPERVISOR route', () => {
    const result = runGuard(createMockUser(UserRole.ADMIN), [UserRole.SUPERVISOR]);
    expect(result).toBeFalse();
  });

  it('should allow BECA to access BECA route', () => {
    const result = runGuard(createMockUser(UserRole.BECA), [UserRole.BECA]);
    expect(result).toBeTrue();
  });

  it('should allow access when user role is in multi-role list', () => {
    const result = runGuard(createMockUser(UserRole.ADMIN), [UserRole.ADMIN, UserRole.SUPERVISOR]);
    expect(result).toBeTrue();
  });
});
