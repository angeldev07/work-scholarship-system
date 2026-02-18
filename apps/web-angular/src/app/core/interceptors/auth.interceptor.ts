import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { RefreshTokenResponse } from '../models/auth.models';

// Module-level state for concurrent refresh prevention
let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<ReturnType<HttpHandlerFn> extends Observable<infer T> ? T : never> => {
  const authService = inject(AuthService);

  // Skip auth endpoints to avoid circular refresh loops
  const isAuthEndpoint =
    req.url.includes('/api/auth/login') ||
    req.url.includes('/api/auth/refresh') ||
    req.url.includes('/api/auth/password/forgot') ||
    req.url.includes('/api/auth/password/reset');

  // Always send withCredentials for cookie-based refresh token
  const requestWithCredentials = req.clone({ withCredentials: true });

  // Attach access token if available and not an auth endpoint
  const token = authService.getAccessToken();
  const authenticatedReq =
    token && !isAuthEndpoint
      ? addBearerToken(requestWithCredentials, token)
      : requestWithCredentials;

  return next(authenticatedReq).pipe(
    catchError((error) => {
      if (error?.status === 401 && !isAuthEndpoint) {
        return handle401Error(authenticatedReq, next, authService);
      }
      return throwError(() => error);
    }),
  ) as ReturnType<typeof next>;
};

function addBearerToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
}

function handle401Error(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
): Observable<unknown> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((response: RefreshTokenResponse) => {
        isRefreshing = false;
        refreshTokenSubject.next(response.accessToken);
        return next(addBearerToken(req, response.accessToken));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshTokenSubject.next(null);
        authService.clearAuth();
        return throwError(() => err);
      }),
    );
  }

  // Queue concurrent requests until refresh completes
  return refreshTokenSubject.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(addBearerToken(req, token))),
  );
}
