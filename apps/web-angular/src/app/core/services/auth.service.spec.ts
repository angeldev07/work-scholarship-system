import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import {
  ApiResponse,
  AuthProvider,
  LoginResponse,
  UserDto,
  UserRole,
} from '../models/auth.models';

const mockUser: UserDto = {
  id: '1',
  email: 'test@universidad.edu',
  firstName: 'Juan',
  lastName: 'Perez',
  fullName: 'Juan Perez',
  role: UserRole.BECA,
  photoUrl: null,
  isActive: true,
  lastLogin: '2026-02-17T10:00:00Z',
  authProvider: AuthProvider.Local,
};

const mockLoginResponse: ApiResponse<LoginResponse> = {
  success: true,
  data: {
    accessToken: 'test-access-token',
    expiresIn: 86400,
    tokenType: 'Bearer',
    user: mockUser,
  },
  message: 'Login exitoso',
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Initial state', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should start unauthenticated', () => {
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('should start with null user', () => {
      expect(service.currentUser()).toBeNull();
    });

    it('should start with null access token', () => {
      expect(service.getAccessToken()).toBeNull();
    });

    it('should start not loading', () => {
      expect(service.isLoading()).toBeFalse();
    });

    it('should start with null error', () => {
      expect(service.error()).toBeNull();
    });
  });

  describe('login()', () => {
    it('should set access token and user on successful login', () => {
      service.login({ email: 'test@universidad.edu', password: 'Pass123!' }).subscribe({
        next: (user) => {
          expect(user).toEqual(mockUser);
          expect(service.getAccessToken()).toBe('test-access-token');
          expect(service.currentUser()).toEqual(mockUser);
          expect(service.isAuthenticated()).toBeTrue();
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/login');
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBeTrue();
      req.flush(mockLoginResponse);
    });

    it('should set loading to true during request', () => {
      service.login({ email: 'test@universidad.edu', password: 'Pass123!' }).subscribe();

      expect(service.isLoading()).toBeTrue();

      const req = httpMock.expectOne('https://localhost:7001/api/auth/login');
      req.flush(mockLoginResponse);

      expect(service.isLoading()).toBeFalse();
    });

    it('should set error signal on login failure', () => {
      service.login({ email: 'test@universidad.edu', password: 'wrong' }).subscribe({
        error: (err) => {
          expect(err.code).toBe('INVALID_CREDENTIALS');
          expect(service.error()?.code).toBe('INVALID_CREDENTIALS');
          expect(service.isAuthenticated()).toBeFalse();
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/login');
      req.flush(
        {
          success: false,
          error: { code: 'INVALID_CREDENTIALS', message: 'Email o contraseña incorrectos' },
        },
        { status: 401, statusText: 'Unauthorized' },
      );
    });

    it('should handle network error during login', () => {
      service.login({ email: 'test@universidad.edu', password: 'Pass123!' }).subscribe({
        error: (err) => {
          expect(err.code).toBe('NETWORK_ERROR');
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/login');
      req.error(new ProgressEvent('error'));
    });
  });

  describe('logout()', () => {
    beforeEach(() => {
      // Set up logged-in state
      service.setAccessToken('test-token');
    });

    it('should clear auth state after logout', () => {
      service.logout();

      const req = httpMock.expectOne('https://localhost:7001/api/auth/logout');
      expect(req.request.method).toBe('POST');
      req.flush({ success: true });

      expect(service.getAccessToken()).toBeNull();
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('should clear auth state even if logout API call fails', () => {
      service.logout();

      const req = httpMock.expectOne('https://localhost:7001/api/auth/logout');
      req.error(new ProgressEvent('error'));

      expect(service.getAccessToken()).toBeNull();
    });
  });

  describe('forgotPassword()', () => {
    it('should call forgot password endpoint', () => {
      service.forgotPassword({ email: 'test@universidad.edu' }).subscribe({
        next: () => {
          expect(service.isLoading()).toBeFalse();
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/password/forgot');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@universidad.edu' });
      req.flush({ success: true, message: 'Instrucciones enviadas' });
    });
  });

  describe('resetPassword()', () => {
    it('should call reset password endpoint with token', () => {
      service
        .resetPassword({
          token: 'reset-token-123',
          newPassword: 'NewPass123!',
          confirmPassword: 'NewPass123!',
        })
        .subscribe({
          next: () => {
            expect(service.isLoading()).toBeFalse();
          },
        });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/password/reset');
      expect(req.request.method).toBe('POST');
      expect(req.request.body.token).toBe('reset-token-123');
      req.flush({ success: true });
    });
  });

  describe('refreshToken()', () => {
    it('should update access token on successful refresh', () => {
      service.refreshToken().subscribe({
        next: (response) => {
          expect(response.accessToken).toBe('new-access-token');
          expect(service.getAccessToken()).toBe('new-access-token');
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/refresh');
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBeTrue();
      req.flush({
        success: true,
        data: { accessToken: 'new-access-token', expiresIn: 86400, tokenType: 'Bearer' },
      });
    });

    it('should clear auth and redirect on refresh failure', () => {
      service.refreshToken().subscribe({
        error: () => {
          expect(service.getAccessToken()).toBeNull();
          expect(service.isAuthenticated()).toBeFalse();
        },
      });

      const req = httpMock.expectOne('https://localhost:7001/api/auth/refresh');
      req.flush(
        { success: false, error: { code: 'SESSION_EXPIRED', message: 'Sesión expirada' } },
        { status: 401, statusText: 'Unauthorized' },
      );
    });
  });

  describe('token management', () => {
    it('should set and get access token', () => {
      service.setAccessToken('my-token');
      expect(service.getAccessToken()).toBe('my-token');
    });

    it('should clear auth state', () => {
      service.setAccessToken('my-token');
      service.clearAuth();
      expect(service.getAccessToken()).toBeNull();
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('role computed signals', () => {
    it('should correctly identify ADMIN role', () => {
      const adminUser: UserDto = { ...mockUser, role: UserRole.ADMIN };
      // Simulate login state
      service.login({ email: 'admin@universidad.edu', password: 'Admin123!' }).subscribe();
      httpMock
        .expectOne('https://localhost:7001/api/auth/login')
        .flush({ success: true, data: { ...mockLoginResponse.data, user: adminUser } });

      expect(service.isAdmin()).toBeTrue();
      expect(service.isSupervisor()).toBeFalse();
      expect(service.isBeca()).toBeFalse();
    });

    it('should correctly identify SUPERVISOR role', () => {
      const supervisorUser: UserDto = { ...mockUser, role: UserRole.SUPERVISOR };
      service.login({ email: 'sup@universidad.edu', password: 'Sup123!' }).subscribe();
      httpMock
        .expectOne('https://localhost:7001/api/auth/login')
        .flush({ success: true, data: { ...mockLoginResponse.data, user: supervisorUser } });

      expect(service.isSupervisor()).toBeTrue();
      expect(service.isAdmin()).toBeFalse();
    });

    it('should correctly identify BECA role', () => {
      service.login({ email: 'beca@universidad.edu', password: 'Beca123!' }).subscribe();
      httpMock
        .expectOne('https://localhost:7001/api/auth/login')
        .flush(mockLoginResponse);

      expect(service.isBeca()).toBeTrue();
    });
  });

  describe('getSafeReturnUrl()', () => {
    it('should return default url for null returnUrl', () => {
      const result = service.getSafeReturnUrl(null);
      expect(result).toBeTruthy();
    });

    it('should reject external URLs', () => {
      const result = service.getSafeReturnUrl('https://evil.com/steal');
      expect(result).not.toBe('https://evil.com/steal');
    });

    it('should reject URLs without leading slash', () => {
      const result = service.getSafeReturnUrl('evil.com');
      expect(result).not.toBe('evil.com');
    });

    it('should accept valid internal paths', () => {
      const result = service.getSafeReturnUrl('/admin/dashboard');
      expect(result).toBe('/admin/dashboard');
    });
  });
});
