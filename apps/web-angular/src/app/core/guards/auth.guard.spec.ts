import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { UserDto, UserRole, AuthProvider } from '../models/auth.models';

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

describe('authGuard', () => {
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'currentUser'], {
      isAuthenticated: jasmine.createSpy().and.returnValue(false),
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

  const createMockState = (url: string): RouterStateSnapshot => {
    return { url } as RouterStateSnapshot;
  };

  const runGuard = (authenticated: boolean, url = '/dashboard'): boolean => {
    (mockAuthService.isAuthenticated as jasmine.Spy).and.returnValue(authenticated);
    return TestBed.runInInjectionContext(() =>
      authGuard(new ActivatedRouteSnapshot(), createMockState(url)) as boolean,
    );
  };

  it('should allow access when user is authenticated', () => {
    const result = runGuard(true);
    expect(result).toBeTrue();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should deny access and redirect to login when not authenticated', () => {
    const result = runGuard(false, '/admin/dashboard');
    expect(result).toBeFalse();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: '/admin/dashboard' },
    });
  });

  it('should include returnUrl in redirect', () => {
    const protectedUrl = '/supervisor/approvals';
    runGuard(false, protectedUrl);
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: protectedUrl },
    });
  });
});
