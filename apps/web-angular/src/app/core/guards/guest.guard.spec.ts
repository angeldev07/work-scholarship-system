import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { guestGuard } from './guest.guard';
import { AuthService } from '../services/auth.service';
import { UserDto, UserRole, AuthProvider } from '../models/auth.models';

const mockAdminUser: UserDto = {
  id: '1',
  email: 'admin@universidad.edu',
  firstName: 'Admin',
  lastName: 'User',
  fullName: 'Admin User',
  role: UserRole.ADMIN,
  photoUrl: null,
  isActive: true,
  lastLogin: null,
  authProvider: AuthProvider.Local,
};

describe('guestGuard', () => {
  let mockAuthService: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    mockAuthService = jasmine.createSpyObj('AuthService', [
      'isAuthenticated',
      'currentUser',
      'navigateToDashboard',
    ]);
    (mockAuthService.isAuthenticated as jasmine.Spy).and.returnValue(false);
    (mockAuthService.currentUser as jasmine.Spy).and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        provideRouter([]),
      ],
    });
  });

  const runGuard = (): boolean => {
    return TestBed.runInInjectionContext(() =>
      guestGuard({} as never, {} as never) as boolean,
    );
  };

  it('should allow access to guest routes when not authenticated', () => {
    (mockAuthService.isAuthenticated as jasmine.Spy).and.returnValue(false);
    const result = runGuard();
    expect(result).toBeTrue();
    expect(mockAuthService.navigateToDashboard).not.toHaveBeenCalled();
  });

  it('should redirect to dashboard when already authenticated', () => {
    (mockAuthService.isAuthenticated as jasmine.Spy).and.returnValue(true);
    (mockAuthService.currentUser as jasmine.Spy).and.returnValue(mockAdminUser);

    const result = runGuard();
    expect(result).toBeFalse();
    expect(mockAuthService.navigateToDashboard).toHaveBeenCalledWith(UserRole.ADMIN);
  });
});
