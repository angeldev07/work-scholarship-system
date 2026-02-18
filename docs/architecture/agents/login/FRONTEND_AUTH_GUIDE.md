# Frontend Authentication Integration Guide
## Angular 19 Implementation Specification

**Document Version:** 1.0
**Last Updated:** 2026-02-17
**Backend API Version:** .NET 9 with Clean Architecture
**Target Frontend:** Angular 19

---

## Table of Contents

1. [Overview](#overview)
2. [API Contract](#api-contract)
3. [TypeScript Interfaces](#typescript-interfaces)
4. [Authentication Flows](#authentication-flows)
5. [State Management](#state-management)
6. [Route Structure](#route-structure)
7. [Guards and Route Protection](#guards-and-route-protection)
8. [Client-Side Validation](#client-side-validation)
9. [Error Handling](#error-handling)
10. [Security Considerations](#security-considerations)
11. [Backend-Frontend Coordination](#backend-frontend-coordination)

---

## Overview

This guide defines the complete contract between the .NET 9 backend and Angular 20 frontend for the authentication module. It serves as the single source of truth for implementing all auth-related features.

### Key Principles

- **Stateless JWT Authentication**: Access token in memory, refresh token in httpOnly cookie
- **OAuth 2.0 Support**: Google login with institutional domain restriction
- **Role-Based Access Control**: ADMIN, SUPERVISOR, BECA roles
- **Security First**: XSS protection, CSRF tokens, secure storage

### Base API URL

| Environment | URL |
|-------------|-----|
| Development | `https://localhost:7001` |
| Staging | TBD |
| Production | TBD |

---

## API Contract

### Endpoint Summary

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Email/password login | No |
| POST | `/api/auth/refresh` | Renew access token | Refresh token cookie |
| POST | `/api/auth/logout` | Invalidate session | Yes |
| GET | `/api/auth/me` | Get current user | Yes |
| GET | `/api/auth/google/login` | Start Google OAuth | No |
| GET | `/api/auth/google/callback` | OAuth callback | No (handled by backend) |
| POST | `/api/auth/password/forgot` | Request password reset | No |
| POST | `/api/auth/password/reset` | Reset password with token | No |
| PUT | `/api/auth/password/change` | Change password (authenticated) | Yes |

---

### 1. POST `/api/auth/login`

Login with email and password.

**Request:**

```typescript
{
  "email": "usuario@universidad.edu",
  "password": "Pass123!"
}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 86400,
    "tokenType": "Bearer",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "usuario@universidad.edu",
      "firstName": "Juan",
      "lastName": "Pérez",
      "fullName": "Juan Pérez",
      "role": "BECA",
      "photoUrl": "https://storage.example.com/photos/user123.jpg"
    }
  },
  "message": "Login exitoso"
}
```

**Note:** The `refreshToken` is NOT in the response body. It's automatically set by the backend as an httpOnly cookie named `refreshToken`.

**Error Responses:**

| Status | Code | Message | Description |
|--------|------|---------|-------------|
| 400 | `VALIDATION_ERROR` | "Email y contraseña son requeridos" | Missing fields |
| 401 | `INVALID_CREDENTIALS` | "Email o contraseña incorrectos" | Wrong credentials |
| 401 | `ACCOUNT_LOCKED` | "Cuenta bloqueada por intentos fallidos. Intenta en 15 minutos" | 5 failed attempts |
| 403 | `GOOGLE_ACCOUNT` | "Esta cuenta usa Google. Usa 'Iniciar sesión con Google'" | User registered via OAuth |

**Error Response Format:**

```typescript
{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Email o contraseña incorrectos",
    "details": []
  }
}
```

---

### 2. POST `/api/auth/refresh`

Renew access token using the refresh token cookie.

**Request:**

No body required. The `refreshToken` cookie is automatically sent by the browser.

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 86400,
    "tokenType": "Bearer"
  },
  "message": "Token renovado exitosamente"
}
```

**Error Responses:**

| Status | Code | Message | Description |
|--------|------|---------|-------------|
| 401 | `INVALID_REFRESH_TOKEN` | "Token de renovación inválido o expirado" | Cookie missing or invalid |
| 401 | `SESSION_EXPIRED` | "Sesión expirada. Por favor inicia sesión nuevamente" | Refresh token expired |

---

### 3. POST `/api/auth/logout`

Invalidate current session and clear refresh token cookie.

**Request:**

No body required.

**Headers:**

```
Authorization: Bearer {accessToken}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "message": "Sesión cerrada exitosamente"
}
```

**Error Responses:**

| Status | Code | Message |
|--------|------|---------|
| 401 | `UNAUTHORIZED` | "No autorizado" |

---

### 4. GET `/api/auth/me`

Get currently authenticated user information.

**Headers:**

```
Authorization: Bearer {accessToken}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "usuario@universidad.edu",
    "firstName": "Juan",
    "lastName": "Pérez",
    "fullName": "Juan Pérez",
    "role": "BECA",
    "photoUrl": "https://storage.example.com/photos/user123.jpg",
    "isActive": true,
    "lastLogin": "2026-02-17T10:30:00Z",
    "authProvider": "Local"
  }
}
```

**Error Responses:**

| Status | Code | Message |
|--------|------|---------|
| 401 | `UNAUTHORIZED` | "No autorizado" |

---

### 5. GET `/api/auth/google/login`

Initiate Google OAuth 2.0 flow.

**Query Parameters:**

```
?returnUrl=/dashboard
```

**Response:**

Backend responds with HTTP 302 redirect to Google's OAuth consent screen:

```
Location: https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...
```

**Frontend Action:**

```typescript
// Option 1: Full page redirect
window.location.href = 'https://localhost:7001/api/auth/google/login?returnUrl=/dashboard';

// Option 2: Popup window (recommended for better UX)
window.open('https://localhost:7001/api/auth/google/login?returnUrl=/dashboard',
            'google-login',
            'width=500,height=600');
```

---

### 6. GET `/api/auth/google/callback`

Google OAuth callback endpoint (handled by backend, but frontend needs to know the flow).

**Flow:**

1. User authorizes on Google
2. Google redirects to: `https://localhost:7001/api/auth/google/callback?code=...&state=...`
3. Backend exchanges code for user info
4. Backend creates/updates user
5. Backend sets refresh token cookie
6. Backend redirects to frontend with access token in URL fragment:

```
https://localhost:4200/auth/callback#access_token={token}&expires_in=86400&token_type=Bearer
```

**Frontend Callback Handler:**

The frontend must have a route at `/auth/callback` that:

1. Extracts `access_token` from URL fragment
2. Stores it in memory
3. Fetches user data with `GET /api/auth/me`
4. Redirects to `returnUrl` or `/dashboard`

**Error Redirect:**

If OAuth fails, backend redirects to:

```
https://localhost:4200/auth/login?error=oauth_failed&message={encodedMessage}
```

---

### 7. POST `/api/auth/password/forgot`

Request password reset email.

**Request:**

```typescript
{
  "email": "usuario@universidad.edu"
}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "message": "Si el correo existe, recibirás instrucciones para restablecer tu contraseña"
}
```

**Note:** Always returns success even if email doesn't exist (security best practice).

**Error Responses:**

| Status | Code | Message |
|--------|------|---------|
| 400 | `VALIDATION_ERROR` | "Email es requerido" |
| 429 | `RATE_LIMIT_EXCEEDED` | "Demasiadas solicitudes. Intenta en 5 minutos" |

---

### 8. POST `/api/auth/password/reset`

Reset password using token from email.

**Request:**

```typescript
{
  "token": "abc123def456...",
  "newPassword": "NewPass123!",
  "confirmPassword": "NewPass123!"
}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "message": "Contraseña restablecida exitosamente. Puedes iniciar sesión con tu nueva contraseña"
}
```

**Error Responses:**

| Status | Code | Message | Description |
|--------|------|---------|-------------|
| 400 | `VALIDATION_ERROR` | "Todos los campos son requeridos" | Missing fields |
| 400 | `PASSWORD_MISMATCH` | "Las contraseñas no coinciden" | Confirmation mismatch |
| 400 | `WEAK_PASSWORD` | "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número" | Doesn't meet policy |
| 400 | `INVALID_TOKEN` | "Token inválido o expirado" | Token expired (1h) |

---

### 9. PUT `/api/auth/password/change`

Change password for authenticated user.

**Headers:**

```
Authorization: Bearer {accessToken}
```

**Request:**

```typescript
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewPass123!",
  "confirmPassword": "NewPass123!"
}
```

**Success Response (200 OK):**

```typescript
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 86400,
    "tokenType": "Bearer"
  },
  "message": "Contraseña cambiada exitosamente. Tu sesión ha sido renovada"
}
```

**Note:** After password change, all existing sessions are invalidated and a new access token is returned.

**Error Responses:**

| Status | Code | Message |
|--------|------|---------|
| 400 | `VALIDATION_ERROR` | "Todos los campos son requeridos" |
| 400 | `PASSWORD_MISMATCH` | "Las contraseñas no coinciden" |
| 400 | `WEAK_PASSWORD` | "La contraseña debe tener al menos 8 caracteres..." |
| 401 | `INVALID_CURRENT_PASSWORD` | "La contraseña actual es incorrecta" |
| 401 | `UNAUTHORIZED` | "No autorizado" |

---

## TypeScript Interfaces

### Core Types

```typescript
// ============================================================================
// ENUMS
// ============================================================================

export enum UserRole {
  ADMIN = 'ADMIN',
  SUPERVISOR = 'SUPERVISOR',
  BECA = 'BECA'
}

export enum AuthProvider {
  Local = 'Local',
  Google = 'Google'
}

// ============================================================================
// USER TYPES
// ============================================================================

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  photoUrl: string | null;
  isActive: boolean;
  lastLogin: string | null; // ISO 8601 date string
  authProvider: AuthProvider;
}

// ============================================================================
// AUTH REQUEST TYPES
// ============================================================================

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// ============================================================================
// AUTH RESPONSE TYPES
// ============================================================================

export interface TokenResult {
  accessToken: string;
  expiresIn: number; // seconds
  tokenType: 'Bearer';
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
  user: UserDto;
}

export interface RefreshTokenResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
}

// ============================================================================
// API WRAPPER TYPES
// ============================================================================

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  error?: ApiError;
}

export interface ApiError {
  code: string;
  message: string;
  details?: ValidationError[];
}

export interface ValidationError {
  field: string;
  message: string;
}

// ============================================================================
// ERROR CODE CONSTANTS
// ============================================================================

export const AUTH_ERROR_CODES = {
  VALIDATION_ERROR: 'VALIDATION_ERROR',
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  ACCOUNT_LOCKED: 'ACCOUNT_LOCKED',
  GOOGLE_ACCOUNT: 'GOOGLE_ACCOUNT',
  INVALID_REFRESH_TOKEN: 'INVALID_REFRESH_TOKEN',
  SESSION_EXPIRED: 'SESSION_EXPIRED',
  UNAUTHORIZED: 'UNAUTHORIZED',
  PASSWORD_MISMATCH: 'PASSWORD_MISMATCH',
  WEAK_PASSWORD: 'WEAK_PASSWORD',
  INVALID_TOKEN: 'INVALID_TOKEN',
  INVALID_CURRENT_PASSWORD: 'INVALID_CURRENT_PASSWORD',
  RATE_LIMIT_EXCEEDED: 'RATE_LIMIT_EXCEEDED'
} as const;

export type AuthErrorCode = typeof AUTH_ERROR_CODES[keyof typeof AUTH_ERROR_CODES];

// ============================================================================
// AUTH STATE TYPES (for state management)
// ============================================================================

export interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: ApiError | null;
}
```

---

## Authentication Flows

### Flow 1: Login with Email/Password

**User Journey:**

1. User navigates to `/auth/login`
2. Fills email and password
3. Clicks "Iniciar Sesión"
4. Frontend validates inputs
5. Frontend calls `POST /api/auth/login`
6. Backend validates credentials
7. Backend returns access token + user data
8. Backend sets httpOnly cookie with refresh token
9. Frontend stores access token in memory (service variable)
10. Frontend stores user data in state management
11. Frontend redirects to role-specific dashboard

**Error Scenarios:**

| Error Code | User Action | Frontend Behavior |
|------------|-------------|-------------------|
| `INVALID_CREDENTIALS` | Show error message | Display: "Email o contraseña incorrectos" |
| `ACCOUNT_LOCKED` | Show lockout time | Display: "Cuenta bloqueada. Intenta en 15 minutos" + countdown |
| `GOOGLE_ACCOUNT` | Redirect to Google login | Display: "Esta cuenta usa Google" + show Google button |

**Implementation Notes:**

- Store access token in a service-level variable (NOT localStorage)
- Use Angular's HttpInterceptor to attach token to subsequent requests
- Set up automatic token refresh on 401 errors

**Code Example Structure:**

```typescript
async login(email: string, password: string): Promise<UserDto> {
  // 1. Validate inputs
  if (!email || !password) {
    throw new Error('Email y contraseña son requeridos');
  }

  // 2. Call API
  const response = await this.http.post<ApiResponse<LoginResponse>>('/api/auth/login', {
    email,
    password
  }).toPromise();

  // 3. Handle response
  if (response.success && response.data) {
    // Store token in memory
    this.accessToken = response.data.accessToken;

    // Store user in state
    this.currentUser = response.data.user;

    // Update auth state
    this.isAuthenticated = true;

    return response.data.user;
  } else {
    throw new Error(response.error?.message || 'Error al iniciar sesión');
  }
}
```

---

### Flow 2: Login with Google OAuth

**User Journey:**

1. User navigates to `/auth/login`
2. Clicks "Iniciar sesión con Google"
3. Frontend opens popup or redirects to `GET /api/auth/google/login?returnUrl=/dashboard`
4. Backend redirects to Google's OAuth consent screen
5. User authorizes on Google
6. Google redirects back to `GET /api/auth/google/callback?code=...`
7. Backend exchanges code for user data
8. Backend creates/updates user in database
9. Backend sets refresh token cookie
10. Backend redirects to `/auth/callback#access_token=...&expires_in=...`
11. Frontend extracts token from URL fragment
12. Frontend calls `GET /api/auth/me` to get user data
13. Frontend stores token and user
14. Frontend redirects to dashboard

**Popup Implementation (Recommended):**

```typescript
loginWithGoogle(): void {
  const width = 500;
  const height = 600;
  const left = (screen.width / 2) - (width / 2);
  const top = (screen.height / 2) - (height / 2);

  const popup = window.open(
    `${API_BASE_URL}/api/auth/google/login?returnUrl=/dashboard`,
    'google-login',
    `width=${width},height=${height},left=${left},top=${top}`
  );

  // Listen for message from callback page
  window.addEventListener('message', (event) => {
    if (event.origin !== window.location.origin) return;

    if (event.data.type === 'GOOGLE_AUTH_SUCCESS') {
      this.handleGoogleAuthSuccess(event.data.accessToken);
      popup?.close();
    } else if (event.data.type === 'GOOGLE_AUTH_ERROR') {
      this.handleGoogleAuthError(event.data.error);
      popup?.close();
    }
  });
}
```

**Callback Page (`/auth/callback`):**

```typescript
ngOnInit(): void {
  // Extract token from URL fragment
  const fragment = window.location.hash.substring(1);
  const params = new URLSearchParams(fragment);

  const accessToken = params.get('access_token');
  const error = params.get('error');

  if (accessToken) {
    // Send token to parent window
    if (window.opener) {
      window.opener.postMessage({
        type: 'GOOGLE_AUTH_SUCCESS',
        accessToken
      }, window.location.origin);
    } else {
      // Not popup - handle as full redirect
      this.authService.setAccessToken(accessToken);
      this.router.navigate(['/dashboard']);
    }
  } else if (error) {
    if (window.opener) {
      window.opener.postMessage({
        type: 'GOOGLE_AUTH_ERROR',
        error
      }, window.location.origin);
    } else {
      this.router.navigate(['/auth/login'], {
        queryParams: { error }
      });
    }
  }
}
```

**Error Handling:**

| Error | Message | Action |
|-------|---------|--------|
| `oauth_failed` | "Error al autenticar con Google" | Show error on login page |
| `invalid_domain` | "Solo correos @universidad.edu son permitidos" | Show error message |
| `oauth_cancelled` | "Autenticación cancelada" | No error, just return to login |

---

### Flow 3: Automatic Token Refresh

**Scenario:** Access token expires (24h default), user is still active.

**Implementation Strategy:**

Use an HTTP Interceptor to detect 401 errors and automatically refresh the token.

**Flow:**

1. User makes API request (e.g., `GET /api/locations`)
2. Backend returns 401 (token expired)
3. Interceptor catches 401 error
4. Interceptor calls `POST /api/auth/refresh`
5. Backend validates refresh token cookie
6. Backend returns new access token
7. Interceptor updates stored token
8. Interceptor retries original request with new token
9. Original request succeeds

**Important:** Prevent concurrent refresh requests if multiple APIs fail simultaneously.

**Code Example:**

```typescript
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject = new BehaviorSubject<string | null>(null);

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Add access token to request
    const token = this.authService.getAccessToken();
    if (token) {
      req = this.addToken(req, token);
    }

    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !req.url.includes('/auth/refresh')) {
          return this.handle401Error(req, next);
        }
        return throwError(() => error);
      })
    );
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      return this.authService.refreshToken().pipe(
        switchMap((response: RefreshTokenResponse) => {
          this.isRefreshing = false;
          this.refreshTokenSubject.next(response.accessToken);
          return next.handle(this.addToken(req, response.accessToken));
        }),
        catchError((error) => {
          this.isRefreshing = false;
          this.authService.logout();
          this.router.navigate(['/auth/login']);
          return throwError(() => error);
        })
      );
    } else {
      // Queue concurrent requests
      return this.refreshTokenSubject.pipe(
        filter(token => token !== null),
        take(1),
        switchMap(token => next.handle(this.addToken(req, token!)))
      );
    }
  }

  private addToken(req: HttpRequest<any>, token: string): HttpRequest<any> {
    return req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }
}
```

---

### Flow 4: Logout

**User Journey:**

1. User clicks "Cerrar Sesión"
2. Frontend calls `POST /api/auth/logout`
3. Backend invalidates refresh token
4. Backend clears refresh token cookie
5. Frontend clears access token from memory
6. Frontend clears user data from state
7. Frontend redirects to `/auth/login`

**Implementation:**

```typescript
async logout(): Promise<void> {
  try {
    // Call backend to invalidate session
    await this.http.post('/api/auth/logout', {}).toPromise();
  } catch (error) {
    // Continue logout even if API call fails
    console.error('Error during logout:', error);
  } finally {
    // Clear local state regardless of API result
    this.accessToken = null;
    this.currentUser = null;
    this.isAuthenticated = false;

    // Redirect to login
    this.router.navigate(['/auth/login']);
  }
}
```

---

### Flow 5: Forgot Password

**User Journey:**

1. User clicks "¿Olvidaste tu contraseña?" on login page
2. Navigates to `/auth/forgot-password`
3. Enters email address
4. Clicks "Enviar"
5. Frontend calls `POST /api/auth/password/forgot`
6. Backend sends email with reset link (if email exists)
7. Frontend shows success message (always, for security)
8. User checks email
9. Clicks link: `https://localhost:4200/auth/reset-password?token=abc123`
10. Frontend shows reset password form

**Email Link Format:**

```
https://localhost:4200/auth/reset-password?token={resetToken}
```

**Success Page:**

Show message: "Si tu correo está registrado, recibirás instrucciones para restablecer tu contraseña. Revisa tu bandeja de entrada y spam."

**Important:** Always show success message, even if email doesn't exist (prevents email enumeration attacks).

---

### Flow 6: Reset Password

**User Journey:**

1. User clicks link from email
2. Lands on `/auth/reset-password?token=abc123`
3. Frontend extracts token from query params
4. Shows form: New Password, Confirm Password
5. User fills and submits
6. Frontend validates:
   - Passwords match
   - Password meets requirements
7. Frontend calls `POST /api/auth/password/reset`
8. Backend validates token
9. Backend updates password
10. Frontend shows success message
11. Frontend redirects to `/auth/login` after 3 seconds

**Password Requirements Display:**

```
Tu contraseña debe tener:
✓ Al menos 8 caracteres
✓ Una letra mayúscula
✓ Una letra minúscula
✓ Un número
```

**Error Handling:**

| Error | Message | Action |
|-------|---------|--------|
| `INVALID_TOKEN` | "El enlace ha expirado o es inválido" | Show link to request new reset |
| `PASSWORD_MISMATCH` | "Las contraseñas no coinciden" | Highlight confirm field |
| `WEAK_PASSWORD` | "La contraseña no cumple los requisitos" | Show requirements again |

---

### Flow 7: Change Password (Authenticated User)

**User Journey:**

1. Authenticated user navigates to `/profile/change-password` or settings
2. Shows form: Current Password, New Password, Confirm New Password
3. User fills and submits
4. Frontend validates locally
5. Frontend calls `PUT /api/auth/password/change`
6. Backend validates current password
7. Backend updates password
8. Backend invalidates all sessions except current
9. Backend returns new access token
10. Frontend updates token
11. Frontend shows success message
12. Frontend redirects to profile

**Important:** After password change, user stays logged in with new token, but all other devices/sessions are logged out.

---

## State Management

### Recommended Approach: Angular Service + RxJS

Create an `AuthService` that manages authentication state and exposes observables.

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  // Private state
  private accessTokenSubject = new BehaviorSubject<string | null>(null);
  private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
  private isLoadingSubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<ApiError | null>(null);

  // Public observables
  public readonly accessToken$ = this.accessTokenSubject.asObservable();
  public readonly currentUser$ = this.currentUserSubject.asObservable();
  public readonly isLoading$ = this.isLoadingSubject.asObservable();
  public readonly error$ = this.errorSubject.asObservable();

  // Computed observables
  public readonly isAuthenticated$ = this.currentUser$.pipe(
    map(user => user !== null)
  );

  public readonly userRole$ = this.currentUser$.pipe(
    map(user => user?.role ?? null)
  );

  // Getters for synchronous access
  get currentUser(): UserDto | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.currentUser !== null;
  }

  // Methods
  async login(email: string, password: string): Promise<UserDto> { }
  async loginWithGoogle(): Promise<void> { }
  async logout(): Promise<void> { }
  async refreshToken(): Promise<RefreshTokenResponse> { }
  async getCurrentUser(): Promise<UserDto> { }
  async changePassword(request: ChangePasswordRequest): Promise<void> { }

  // Token management
  getAccessToken(): string | null {
    return this.accessTokenSubject.value;
  }

  setAccessToken(token: string): void {
    this.accessTokenSubject.next(token);
  }

  clearAuth(): void {
    this.accessTokenSubject.next(null);
    this.currentUserSubject.next(null);
  }
}
```

### Alternative: NgRx (if using Redux pattern)

If the project uses NgRx, create:

- `auth.actions.ts`: Login, Logout, RefreshToken, etc.
- `auth.reducer.ts`: Manage AuthState
- `auth.effects.ts`: Handle side effects (API calls)
- `auth.selectors.ts`: Select slices of state

**Note:** For auth module, a simple service is often sufficient. Use NgRx only if already used in the project.

---

## Route Structure

### Auth Routes (`/auth/*`)

```typescript
const routes: Routes = [
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        component: LoginComponent,
        canActivate: [GuestGuard] // Redirect if already logged in
      },
      {
        path: 'callback',
        component: OAuthCallbackComponent // Google OAuth callback handler
      },
      {
        path: 'forgot-password',
        component: ForgotPasswordComponent,
        canActivate: [GuestGuard]
      },
      {
        path: 'reset-password',
        component: ResetPasswordComponent,
        canActivate: [GuestGuard]
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  }
];
```

### Protected Routes

```typescript
{
  path: 'dashboard',
  component: DashboardComponent,
  canActivate: [AuthGuard]
}

{
  path: 'admin',
  loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
  canActivate: [AuthGuard, RoleGuard],
  data: { roles: [UserRole.ADMIN] }
}

{
  path: 'supervisor',
  loadChildren: () => import('./supervisor/supervisor.module').then(m => m.SupervisorModule),
  canActivate: [AuthGuard, RoleGuard],
  data: { roles: [UserRole.SUPERVISOR, UserRole.ADMIN] }
}

{
  path: 'beca',
  loadChildren: () => import('./beca/beca.module').then(m => m.BecaModule),
  canActivate: [AuthGuard, RoleGuard],
  data: { roles: [UserRole.BECA] }
}
```

### Root Redirect Logic

```typescript
{
  path: '',
  canActivate: [RootRedirectGuard],
  component: EmptyComponent
}
```

**RootRedirectGuard logic:**

```typescript
@Injectable({ providedIn: 'root' })
export class RootRedirectGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): boolean {
    const user = this.authService.currentUser;

    if (!user) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    switch (user.role) {
      case UserRole.ADMIN:
        this.router.navigate(['/admin/dashboard']);
        break;
      case UserRole.SUPERVISOR:
        this.router.navigate(['/supervisor/dashboard']);
        break;
      case UserRole.BECA:
        this.router.navigate(['/beca/dashboard']);
        break;
      default:
        this.router.navigate(['/auth/login']);
    }

    return false;
  }
}
```

---

## Guards and Route Protection

### 1. AuthGuard

Ensures user is authenticated before accessing route.

```typescript
@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean {
    if (this.authService.isAuthenticated) {
      return true;
    }

    // Store intended URL for redirect after login
    this.router.navigate(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }
}
```

### 2. RoleGuard

Ensures user has required role for the route.

```typescript
@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const user = this.authService.currentUser;

    if (!user) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    const requiredRoles = route.data['roles'] as UserRole[];

    if (!requiredRoles || requiredRoles.length === 0) {
      return true; // No role requirement
    }

    if (requiredRoles.includes(user.role)) {
      return true;
    }

    // User doesn't have required role
    this.router.navigate(['/forbidden']);
    return false;
  }
}
```

### 3. GuestGuard

Redirects to dashboard if user is already logged in (for login page).

```typescript
@Injectable({ providedIn: 'root' })
export class GuestGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(): boolean {
    if (this.authService.isAuthenticated) {
      const user = this.authService.currentUser!;

      switch (user.role) {
        case UserRole.ADMIN:
          this.router.navigate(['/admin/dashboard']);
          break;
        case UserRole.SUPERVISOR:
          this.router.navigate(['/supervisor/dashboard']);
          break;
        case UserRole.BECA:
          this.router.navigate(['/beca/dashboard']);
          break;
      }

      return false;
    }

    return true;
  }
}
```

---

## Client-Side Validation

### Login Form

```typescript
loginForm = this.fb.group({
  email: ['', [
    Validators.required,
    Validators.email,
    Validators.pattern(/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/)
  ]],
  password: ['', [
    Validators.required,
    Validators.minLength(1) // Don't validate password strength on login
  ]]
});

// Error messages
getEmailError(): string {
  const control = this.loginForm.get('email');
  if (control?.hasError('required')) {
    return 'El email es requerido';
  }
  if (control?.hasError('email') || control?.hasError('pattern')) {
    return 'Ingresa un email válido';
  }
  return '';
}

getPasswordError(): string {
  const control = this.loginForm.get('password');
  if (control?.hasError('required')) {
    return 'La contraseña es requerida';
  }
  return '';
}
```

### Reset/Change Password Form

```typescript
resetPasswordForm = this.fb.group({
  newPassword: ['', [
    Validators.required,
    Validators.minLength(8),
    this.passwordStrengthValidator()
  ]],
  confirmPassword: ['', [Validators.required]]
}, {
  validators: this.passwordMatchValidator()
});

// Custom validators
passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumber = /\d/.test(value);
    const isLongEnough = value.length >= 8;

    const valid = hasUpperCase && hasLowerCase && hasNumber && isLongEnough;

    return valid ? null : {
      passwordStrength: {
        hasUpperCase,
        hasLowerCase,
        hasNumber,
        isLongEnough
      }
    };
  };
}

passwordMatchValidator(): ValidatorFn {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const password = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

// Password requirements component
@Component({
  selector: 'app-password-requirements',
  template: `
    <div class="password-requirements">
      <p>Tu contraseña debe tener:</p>
      <ul>
        <li [class.valid]="requirements.isLongEnough">
          <i [class]="requirements.isLongEnough ? 'icon-check' : 'icon-x'"></i>
          Al menos 8 caracteres
        </li>
        <li [class.valid]="requirements.hasUpperCase">
          <i [class]="requirements.hasUpperCase ? 'icon-check' : 'icon-x'"></i>
          Una letra mayúscula
        </li>
        <li [class.valid]="requirements.hasLowerCase">
          <i [class]="requirements.hasLowerCase ? 'icon-check' : 'icon-x'"></i>
          Una letra minúscula
        </li>
        <li [class.valid]="requirements.hasNumber">
          <i [class]="requirements.hasNumber ? 'icon-check' : 'icon-x'"></i>
          Un número
        </li>
      </ul>
    </div>
  `
})
export class PasswordRequirementsComponent {
  @Input() password: string = '';

  get requirements() {
    return {
      isLongEnough: this.password.length >= 8,
      hasUpperCase: /[A-Z]/.test(this.password),
      hasLowerCase: /[a-z]/.test(this.password),
      hasNumber: /\d/.test(this.password)
    };
  }
}
```

---

## Error Handling

### Centralized Error Handler Service

```typescript
@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  constructor(private snackBar: MatSnackBar) {} // Or your notification service

  handleAuthError(error: any): void {
    if (error.error?.error) {
      const apiError = error.error.error as ApiError;
      this.showError(apiError.message);
    } else if (error.status === 0) {
      this.showError('No se pudo conectar con el servidor. Verifica tu conexión a internet.');
    } else if (error.status >= 500) {
      this.showError('Error del servidor. Por favor intenta más tarde.');
    } else {
      this.showError('Ocurrió un error inesperado. Por favor intenta nuevamente.');
    }
  }

  private showError(message: string): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
}
```

### HTTP Error Mapping

| Status Code | User Message | Technical Action |
|-------------|--------------|------------------|
| 0 | "No se pudo conectar con el servidor" | Check internet connection |
| 400 | Show API error message | Display validation errors |
| 401 | "Sesión expirada. Por favor inicia sesión" | Trigger refresh or redirect to login |
| 403 | "No tienes permiso para acceder a este recurso" | Show forbidden page |
| 404 | "Recurso no encontrado" | Log error, show generic message |
| 429 | "Demasiadas solicitudes. Intenta en {X} minutos" | Show rate limit message |
| 500 | "Error del servidor. Intenta más tarde" | Log error for debugging |

### Specific Auth Error Handling

```typescript
handleLoginError(error: HttpErrorResponse): void {
  const errorCode = error.error?.error?.code;

  switch (errorCode) {
    case AUTH_ERROR_CODES.INVALID_CREDENTIALS:
      this.showError('Email o contraseña incorrectos');
      break;

    case AUTH_ERROR_CODES.ACCOUNT_LOCKED:
      this.showError('Cuenta bloqueada por intentos fallidos. Intenta en 15 minutos');
      this.startLockoutCountdown(900); // 15 minutes
      break;

    case AUTH_ERROR_CODES.GOOGLE_ACCOUNT:
      this.showError('Esta cuenta usa Google. Usa "Iniciar sesión con Google"');
      this.highlightGoogleButton();
      break;

    default:
      this.showError('Error al iniciar sesión. Intenta nuevamente');
  }
}
```

---

## Security Considerations

### 1. Token Storage

**DO:**
- ✅ Store access token in memory (service variable)
- ✅ Let browser handle refresh token cookie (httpOnly, secure)
- ✅ Clear tokens on logout

**DON'T:**
- ❌ Store access token in localStorage (vulnerable to XSS)
- ❌ Store access token in sessionStorage (still vulnerable to XSS)
- ❌ Access refresh token from JavaScript (backend sets httpOnly cookie)

### 2. XSS Protection

- Sanitize all user inputs using Angular's built-in sanitization
- Use Angular's template syntax (avoids direct DOM manipulation)
- Set Content Security Policy headers (handled by backend)

### 3. CSRF Protection

- Refresh token cookie has `SameSite=Strict` attribute
- Backend validates origin for sensitive operations
- Angular's HttpClient automatically handles CSRF tokens if configured

### 4. Password Security

- Never log passwords
- Clear password fields after login attempt
- Use type="password" for password inputs
- Don't show password strength until user starts typing

### 5. Session Management

- Implement idle timeout (e.g., 30 minutes of inactivity)
- Warn user before auto-logout (e.g., "You'll be logged out in 2 minutes")
- Provide "Stay logged in" option to refresh session

```typescript
@Injectable({ providedIn: 'root' })
export class IdleService {
  private idleTimer: any;
  private readonly IDLE_TIMEOUT = 30 * 60 * 1000; // 30 minutes
  private readonly WARNING_TIME = 2 * 60 * 1000; // 2 minutes before logout

  constructor(
    private authService: AuthService,
    private dialog: MatDialog
  ) {
    this.setupIdleTimer();
  }

  private setupIdleTimer(): void {
    // Reset timer on user activity
    fromEvent(document, 'mousemove')
      .pipe(throttleTime(1000))
      .subscribe(() => this.resetTimer());

    fromEvent(document, 'keypress')
      .pipe(throttleTime(1000))
      .subscribe(() => this.resetTimer());
  }

  private resetTimer(): void {
    clearTimeout(this.idleTimer);
    this.idleTimer = setTimeout(() => this.showWarning(), this.IDLE_TIMEOUT - this.WARNING_TIME);
  }

  private showWarning(): void {
    const dialogRef = this.dialog.open(IdleWarningDialogComponent, {
      disableClose: true,
      data: { secondsRemaining: 120 }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === 'stay') {
        this.resetTimer();
      } else {
        this.authService.logout();
      }
    });
  }
}
```

### 6. Secure Redirect After Login

Always validate `returnUrl` parameter to prevent open redirect vulnerabilities:

```typescript
private getSafeReturnUrl(returnUrl: string | null): string {
  if (!returnUrl) {
    return this.getDefaultDashboard();
  }

  // Only allow internal URLs
  if (returnUrl.startsWith('http://') || returnUrl.startsWith('https://')) {
    return this.getDefaultDashboard();
  }

  // Only allow paths starting with /
  if (!returnUrl.startsWith('/')) {
    return this.getDefaultDashboard();
  }

  return returnUrl;
}
```

---

## Backend-Frontend Coordination

### What's Already Implemented in Backend

✅ **Implemented:**
- JWT authentication with access token + refresh token
- Login with email/password
- User roles (ADMIN, SUPERVISOR, BECA)
- Password hashing
- Token expiration (24h access, 7d refresh)
- Basic error responses

⚠️ **Partially Implemented:**
- Google OAuth (placeholder, needs OAuth client config)
- Password reset emails (needs email service integration)

❌ **Not Yet Implemented:**
- Rate limiting on login attempts
- Account lockout after failed attempts
- Detailed audit logging

### Testing Without Full Backend

**Mock Data for Development:**

```typescript
// mock-auth.service.ts
export class MockAuthService {
  async login(email: string, password: string): Promise<UserDto> {
    // Simulate API delay
    await this.delay(500);

    // Mock user based on email
    if (email === 'admin@universidad.edu' && password === 'admin123') {
      return {
        id: '1',
        email: 'admin@universidad.edu',
        firstName: 'Admin',
        lastName: 'Sistema',
        fullName: 'Admin Sistema',
        role: UserRole.ADMIN,
        photoUrl: null,
        isActive: true,
        lastLogin: new Date().toISOString(),
        authProvider: AuthProvider.Local
      };
    }

    if (email === 'supervisor@universidad.edu' && password === 'super123') {
      return {
        id: '2',
        email: 'supervisor@universidad.edu',
        firstName: 'Carlos',
        lastName: 'Supervisor',
        fullName: 'Carlos Supervisor',
        role: UserRole.SUPERVISOR,
        photoUrl: null,
        isActive: true,
        lastLogin: new Date().toISOString(),
        authProvider: AuthProvider.Local
      };
    }

    if (email === 'beca@universidad.edu' && password === 'beca123') {
      return {
        id: '3',
        email: 'beca@universidad.edu',
        firstName: 'Juan',
        lastName: 'Estudiante',
        fullName: 'Juan Estudiante',
        role: UserRole.BECA,
        photoUrl: null,
        isActive: true,
        lastLogin: new Date().toISOString(),
        authProvider: AuthProvider.Local
      };
    }

    throw new Error('Credenciales inválidas');
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}
```

Use Angular's dependency injection to swap implementations:

```typescript
// In development environment
providers: [
  {
    provide: AuthService,
    useClass: environment.useMockAuth ? MockAuthService : RealAuthService
  }
]
```

### API Error Response Format

All errors follow this structure:

```typescript
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message in Spanish",
    "details": [ // Optional, for validation errors
      {
        "field": "email",
        "message": "Email es requerido"
      }
    ]
  }
}
```

### CORS Configuration

Backend is configured to accept requests from:

- Development: `http://localhost:4200` (Angular)
- Development: `http://localhost:3000` (Next.js)
- Production: TBD

**Headers sent by backend:**

```
Access-Control-Allow-Origin: http://localhost:4200
Access-Control-Allow-Credentials: true
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
```

**Important:** `withCredentials: true` must be set in Angular's HttpClient for cookies to work:

```typescript
this.http.post('/api/auth/login', body, { withCredentials: true })
```

---

## Implementation Checklist

### Phase 1: Core Authentication

- [ ] Create AuthService with login/logout methods
- [ ] Create LoginComponent with form validation
- [ ] Implement HTTP interceptor for adding Authorization header
- [ ] Implement AuthGuard for route protection
- [ ] Store access token in memory (service variable)
- [ ] Test login flow end-to-end

### Phase 2: Token Refresh

- [ ] Implement refresh token logic in interceptor
- [ ] Handle 401 errors and automatic token refresh
- [ ] Prevent concurrent refresh requests
- [ ] Test token expiration scenario

### Phase 3: Role-Based Access

- [ ] Implement RoleGuard
- [ ] Create role-specific dashboard routes
- [ ] Test access control for each role
- [ ] Implement forbidden page (403)

### Phase 4: OAuth Integration

- [ ] Create Google login button on login page
- [ ] Implement popup/redirect to Google OAuth
- [ ] Create OAuth callback component
- [ ] Handle success/error from OAuth flow
- [ ] Test with Google account

### Phase 5: Password Management

- [ ] Create ForgotPasswordComponent
- [ ] Create ResetPasswordComponent with token handling
- [ ] Create ChangePasswordComponent (in user profile)
- [ ] Implement password strength validator
- [ ] Create PasswordRequirementsComponent
- [ ] Test all password flows

### Phase 6: UX Enhancements

- [ ] Implement idle timeout warning
- [ ] Add loading indicators
- [ ] Add form error messages
- [ ] Implement "Remember me" functionality (if needed)
- [ ] Add accessibility (ARIA labels, keyboard navigation)

### Phase 7: Testing

- [ ] Unit tests for AuthService
- [ ] Unit tests for guards
- [ ] Integration tests for login flow
- [ ] E2E tests for critical paths

---

## Conclusion

This guide provides the complete specification for implementing authentication in the Angular 19 frontend. All API endpoints, TypeScript interfaces, flows, and best practices are documented.

**Key Takeaways:**

1. Access token in memory, refresh token in httpOnly cookie
2. Automatic token refresh on 401 errors
3. Role-based routing with guards
4. Comprehensive error handling
5. Security-first approach

**Next Steps:**

1. Implement AuthService with core methods
2. Create login UI with validation
3. Set up HTTP interceptor
4. Test against backend API

For questions or updates, log changes in `SYNC_LOG.md`.

---

**Document maintained by:** dotnet-backend-engineer agent
**For Angular implementation:** angular-ux-engineer agent
**Sync log:** [SYNC_LOG.md](./SYNC_LOG.md)
