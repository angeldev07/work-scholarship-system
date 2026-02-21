import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, map, of, switchMap, tap, throwError } from 'rxjs';
import {
  ApiError,
  ApiResponse,
  AUTH_ERROR_CODES,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
  ResetPasswordRequest,
  UserDto,
  UserRole,
} from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly apiUrl = environment.apiUrl;

  // ─── Private state signals ────────────────────────────────────────────────
  private readonly _accessToken = signal<string | null>(null);
  private readonly _currentUser = signal<UserDto | null>(null);
  private readonly _isLoading = signal(false);
  private readonly _error = signal<ApiError | null>(null);

  // ─── Public computed signals ──────────────────────────────────────────────
  readonly currentUser = this._currentUser.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly userRole = computed(() => this._currentUser()?.role ?? null);

  readonly isAdmin = computed(() => this._currentUser()?.role === UserRole.ADMIN);
  readonly isSupervisor = computed(() => this._currentUser()?.role === UserRole.SUPERVISOR);
  readonly isBeca = computed(() => this._currentUser()?.role === UserRole.BECA);

  // ─── Synchronous getters (for guards/interceptors) ─────────────────────────
  getAccessToken(): string | null {
    return this._accessToken();
  }

  setAccessToken(token: string): void {
    this._accessToken.set(token);
  }

  clearAuth(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
    this._error.set(null);
  }

  // ─── Session restoration (called on app init) ─────────────────────────────
  initializeAuth(): Promise<void> {
    return firstValueFrom(
      this.http
        .post<ApiResponse<RefreshTokenResponse>>(
          `${this.apiUrl}/api/auth/refresh`,
          {},
          { withCredentials: true },
        )
        .pipe(
          switchMap((response) => {
            if (!response.success || !response.data) {
              return of(undefined);
            }
            this._accessToken.set(response.data.accessToken);
            return this.getCurrentUser().pipe(map(() => undefined));
          }),
          catchError(() => of(undefined)),
        ),
    );
  }

  // ─── Login ─────────────────────────────────────────────────────────────────
  login(request: LoginRequest): Observable<UserDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .post<ApiResponse<LoginResponse>>(`${this.apiUrl}/api/auth/login`, request, {
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            throw response.error ?? { code: 'UNKNOWN', message: 'Error al iniciar sesión' };
          }
          this._accessToken.set(response.data.accessToken);
          this._currentUser.set(response.data.user);
          return response.data.user;
        }),
        tap(() => this._isLoading.set(false)),
        catchError((err) => {
          this._isLoading.set(false);
          const apiError = this.extractApiError(err);
          this._error.set(apiError);
          return throwError(() => apiError);
        }),
      );
  }

  // ─── Google OAuth ──────────────────────────────────────────────────────────
  loginWithGoogle(): void {
    const width = 500;
    const height = 600;
    const left = window.screen.width / 2 - width / 2;
    const top = window.screen.height / 2 - height / 2;

    const returnUrl = this.getDefaultDashboardUrl();

    const popup = window.open(
      `${this.apiUrl}/api/auth/google/login?returnUrl=${returnUrl}`,
      'google-login',
      `width=${width},height=${height},left=${left},top=${top}`,
    );

    const messageHandler = (event: MessageEvent) => {
      if (event.origin !== window.location.origin) return;

      if (event.data?.type === 'GOOGLE_AUTH_SUCCESS') {
        window.removeEventListener('message', messageHandler);
        this.handleGoogleAuthSuccess(event.data.accessToken);
        popup?.close();
      } else if (event.data?.type === 'GOOGLE_AUTH_ERROR') {
        window.removeEventListener('message', messageHandler);
        const apiError: ApiError = { code: 'OAUTH_ERROR', message: event.data.error };
        this._error.set(apiError);
        popup?.close();
      }
    };

    window.addEventListener('message', messageHandler);
  }

  handleGoogleAuthSuccess(accessToken: string): void {
    this._accessToken.set(accessToken);
    this.getCurrentUser().subscribe({
      next: (user) => {
        this.navigateToDashboard(user.role);
      },
      error: () => {
        this.clearAuth();
        this.router.navigate(['/auth/login']);
      },
    });
  }

  // ─── Get current user ──────────────────────────────────────────────────────
  getCurrentUser(): Observable<UserDto> {
    return this.http
      .get<ApiResponse<UserDto>>(`${this.apiUrl}/api/auth/me`, {
        withCredentials: true,
      })
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            throw response.error ?? { code: 'UNKNOWN', message: 'Error al obtener usuario' };
          }
          this._currentUser.set(response.data);
          return response.data;
        }),
        catchError((err) => {
          const apiError = this.extractApiError(err);
          return throwError(() => apiError);
        }),
      );
  }

  // ─── Token refresh ─────────────────────────────────────────────────────────
  refreshToken(): Observable<RefreshTokenResponse> {
    return this.http
      .post<ApiResponse<RefreshTokenResponse>>(
        `${this.apiUrl}/api/auth/refresh`,
        {},
        { withCredentials: true },
      )
      .pipe(
        map((response) => {
          if (!response.success || !response.data) {
            throw response.error ?? { code: 'SESSION_EXPIRED', message: 'Sesión expirada' };
          }
          this._accessToken.set(response.data.accessToken);
          return response.data;
        }),
        catchError((err) => {
          this.clearAuth();
          this.router.navigate(['/auth/login']);
          return throwError(() => this.extractApiError(err));
        }),
      );
  }

  // ─── Logout ────────────────────────────────────────────────────────────────
  logout(): void {
    this.http
      .post<ApiResponse<void>>(`${this.apiUrl}/api/auth/logout`, {}, { withCredentials: true })
      .subscribe({
        complete: () => this.performLocalLogout(),
        error: () => this.performLocalLogout(),
      });
  }

  private performLocalLogout(): void {
    this.clearAuth();
    this.router.navigate(['/auth/login']);
  }

  // ─── Forgot password ───────────────────────────────────────────────────────
  forgotPassword(request: ForgotPasswordRequest): Observable<void> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .post<ApiResponse<void>>(`${this.apiUrl}/api/auth/password/forgot`, request)
      .pipe(
        map(() => void 0),
        tap(() => this._isLoading.set(false)),
        catchError((err) => {
          this._isLoading.set(false);
          const apiError = this.extractApiError(err);
          this._error.set(apiError);
          return throwError(() => apiError);
        }),
      );
  }

  // ─── Reset password ────────────────────────────────────────────────────────
  resetPassword(request: ResetPasswordRequest): Observable<void> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .post<ApiResponse<void>>(`${this.apiUrl}/api/auth/password/reset`, request)
      .pipe(
        map(() => void 0),
        tap(() => this._isLoading.set(false)),
        catchError((err) => {
          this._isLoading.set(false);
          const apiError = this.extractApiError(err);
          this._error.set(apiError);
          return throwError(() => apiError);
        }),
      );
  }

  // ─── Change password ───────────────────────────────────────────────────────
  changePassword(request: ChangePasswordRequest): Observable<void> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .put<ApiResponse<{ accessToken: string; expiresIn: number; tokenType: 'Bearer' }>>(
        `${this.apiUrl}/api/auth/password/change`,
        request,
        { withCredentials: true },
      )
      .pipe(
        map((response) => {
          if (response.data?.accessToken) {
            this._accessToken.set(response.data.accessToken);
          }
          return void 0;
        }),
        tap(() => this._isLoading.set(false)),
        catchError((err) => {
          this._isLoading.set(false);
          const apiError = this.extractApiError(err);
          this._error.set(apiError);
          return throwError(() => apiError);
        }),
      );
  }

  // ─── Navigation helpers ────────────────────────────────────────────────────
  navigateToDashboard(role: UserRole): void {
    switch (role) {
      case UserRole.ADMIN:
        this.router.navigate(['/admin/dashboard']);
        break;
      case UserRole.SUPERVISOR:
        this.router.navigate(['/supervisor/dashboard']);
        break;
      case UserRole.BECA:
        this.router.navigate(['/scholar/dashboard']);
        break;
      default:
        this.router.navigate(['/auth/login']);
    }
  }

  getDefaultDashboardUrl(): string {
    const role = this._currentUser()?.role;
    switch (role) {
      case UserRole.ADMIN:
        return '/admin/dashboard';
      case UserRole.SUPERVISOR:
        return '/supervisor/dashboard';
      case UserRole.BECA:
        return '/scholar/dashboard';
      default:
        return '/dashboard';
    }
  }

  getSafeReturnUrl(returnUrl: string | null): string {
    if (!returnUrl) return this.getDefaultDashboardUrl();
    if (returnUrl.startsWith('http://') || returnUrl.startsWith('https://')) {
      return this.getDefaultDashboardUrl();
    }
    if (!returnUrl.startsWith('/')) {
      return this.getDefaultDashboardUrl();
    }
    return returnUrl;
  }

  // ─── Error extraction ──────────────────────────────────────────────────────
  private extractApiError(err: unknown): ApiError {
    if (err && typeof err === 'object' && 'code' in err) {
      return err as ApiError;
    }
    if (err instanceof HttpErrorResponse) {
      if (err.error?.error) {
        return err.error.error as ApiError;
      }
      if (err.status === 0) {
        return {
          code: 'NETWORK_ERROR',
          message: 'No se pudo conectar con el servidor. Verifica tu conexión a internet.',
        };
      }
      if (err.status >= 500) {
        return {
          code: 'SERVER_ERROR',
          message: 'Error del servidor. Por favor intenta más tarde.',
        };
      }
      return {
        code: AUTH_ERROR_CODES.UNAUTHORIZED,
        message: err.message || 'Ocurrió un error inesperado.',
      };
    }
    return { code: 'UNKNOWN', message: 'Ocurrió un error inesperado.' };
  }
}
