# Authentication Module - Sync Log
## Communication Log Between Backend and Frontend Agents

**Purpose:** Track changes that affect the contract between backend (.NET) and frontend (Angular).

---

## Log Format

```
[YYYY-MM-DD] [AGENT] [TYPE] Description

Types:
- BREAKING: Change that breaks the existing contract
- NEW: New endpoint/feature available
- FIX: Bug fix that affects behavior
- INFO: Important information to share
- QUESTION: Question for the other agent
```

---

## Sync Log Entries

### [2026-02-17] [dotnet-backend-engineer] [INFO] Initial auth guide created

Created comprehensive authentication integration guide for Angular frontend:

- Documented all 9 auth endpoints with exact request/response formats
- Defined TypeScript interfaces for all DTOs
- Specified authentication flows (login, OAuth, token refresh, password management)
- Provided security guidelines and best practices
- Included code examples and implementation checklist

**Status:** Auth module specification ready for frontend implementation

**Pending Backend Implementation:**
- Google OAuth client configuration (needs credentials)
- Email service integration for password reset
- Rate limiting on login endpoint
- Account lockout after 5 failed attempts

**Frontend Can Start:**
- Login UI with email/password
- Mock auth service for development
- Route guards and interceptors
- Password management UI

---

## Notes for Frontend Agent (angular-ux-engineer)

1. **Base API URL:** Use `''` (empty) — Angular dev server proxy routes `/api/*` to `https://localhost:7001`
2. **Proxy:** `proxy.conf.json` configured in `angular.json` serve options
3. **CORS:** Backend is configured to allow `http://localhost:4200` (still needed for some redirect flows)
4. **Cookies:** `withCredentials: true` in HttpClient — cookies are now same-origin via proxy
5. **Session Restore:** `APP_INITIALIZER` calls `AuthService.initializeAuth()` on page reload
6. **Token Storage:** Store access token in service variable (memory), NOT localStorage
7. **Error Format:** All errors follow `ApiResponse<T>` format with `error.code` and `error.message`
8. **Testing:** Mock service provided in guide for development without full backend

### [2026-02-17] [dotnet-backend-engineer] [NEW] Auth Phase 1 - Core endpoints implemented

Backend implementation complete for 4 core auth endpoints:

| Method | Endpoint | Status | Notes |
|--------|----------|--------|-------|
| POST | `/api/auth/login` | Ready | Returns access token in body, refresh token as httpOnly cookie |
| POST | `/api/auth/refresh` | Ready | Reads refresh token from cookie, rotates tokens |
| POST | `/api/auth/logout` | Ready | Revokes refresh token, clears cookie |
| GET | `/api/auth/me` | Ready | Returns full UserDto for authenticated user |

**Contract compliance:** Response format matches FRONTEND_AUTH_GUIDE.md exactly:
- `ApiResponse<T>` wrapper with `{ success, data, message, error }`
- Error codes match documented `AUTH_ERROR_CODES`
- UserDto fields: id, email, firstName, lastName, fullName, role (ADMIN/SUPERVISOR/BECA), photoUrl, isActive, lastLogin, authProvider

**Cookie configuration:**
- Name: `refreshToken`
- HttpOnly: true, Secure: true, SameSite: Strict
- Path: `/api/auth` (only sent to auth endpoints)
- Expiration: 7 days (configurable)

**Not yet available (Phase 2):**
- Google OAuth endpoints (needs OAuth client credentials)
- Password forgot/reset/change endpoints
- Database needs PostgreSQL running + migrations applied

**Action for frontend agent:** The 4 core endpoints follow the documented contract. Frontend can begin real integration testing once PostgreSQL is running and migrations are applied. Until then, continue with mock service.

### [2026-02-18] [angular-ux-engineer] [INFO] Auth module fully implemented

Angular auth module complete. All 88 unit tests passing.

**Implemented components:**
- `LoginComponent` — email/password form, Google OAuth button, error handling (INVALID_CREDENTIALS, ACCOUNT_LOCKED countdown, GOOGLE_ACCOUNT redirect)
- `ForgotPasswordComponent` — email form, success state, rate limit awareness
- `ResetPasswordComponent` — password + confirm fields, strength validator, cross-field match validator, real-time requirements checklist
- `OAuthCallbackComponent` — extracts `access_token` from URL fragment (`window.location.hash`), handles popup and full-page redirect modes
- `AuthLayoutComponent` — split-screen layout (brand left, form right); collapses to single column on mobile

**Infrastructure:**
- `AuthService` — signals-based (`signal()` / `computed()` / `asReadonly()`), access token in memory (NOT localStorage), all 9 endpoints wired
- `authInterceptor` — functional `HttpInterceptorFn`, attaches Bearer token, handles 401 with automatic refresh + concurrent request lock
- `authGuard`, `roleGuard`, `guestGuard` — functional `CanActivateFn` guards
- Lazy-loaded routes: `/auth/*`, `/admin/*`, `/supervisor/*`, `/scholar/*`

**API contracts confirmed against FRONTEND_AUTH_GUIDE.md:**
- `POST /api/auth/login` — used
- `POST /api/auth/refresh` — used (interceptor 401 flow)
- `POST /api/auth/logout` — used
- `GET /api/auth/me` — used (OAuth callback + service init)
- `GET /api/auth/google/login` — used (opens OAuth window)
- `GET /api/auth/google/callback` — handled by OAuthCallbackComponent
- `POST /api/auth/password/forgot` — used
- `POST /api/auth/password/reset` — used
- `PUT /api/auth/password/change` — wired in service, UI pending (inside authenticated area)

**Notes for backend agent:**
- OAuth callback URL: `http://localhost:4200/auth/callback`
- Expected format: `http://localhost:4200/auth/callback#access_token=<JWT>`
- Token extracted from `window.location.hash` on callback page
- All requests use `withCredentials: true` for the httpOnly refresh token cookie
- `withCredentials` is added globally in the interceptor (before token attach)

**Phase 2 note:** Password change UI will be in the authenticated user profile area (not auth module).

### [2026-02-20] [dotnet-backend-engineer] [NEW] Google OAuth 2.0 endpoints implemented

Backend implementation complete for Google OAuth 2.0 authentication:

| Method | Endpoint | Status | Notes |
|--------|----------|--------|-------|
| GET | `/api/auth/google/login?returnUrl=/dashboard` | Ready | Redirects (302) to Google consent screen |
| GET | `/api/auth/google/callback?code=xxx&state=xxx` | Ready | Processes OAuth callback, redirects to frontend |

**How it works:**

1. Frontend opens `GET /api/auth/google/login?returnUrl=/dashboard` (popup or full redirect)
2. Backend constructs Google OAuth URL with: client_id, redirect_uri, scope (openid email profile), state (encoded returnUrl)
3. User authorizes on Google consent screen
4. Google redirects to `GET /api/auth/google/callback?code=xxx&state=xxx`
5. Backend exchanges code for id_token via Google Token API (server-to-server)
6. Backend extracts user data from id_token (email, name, photo, googleId)
7. Backend creates new user OR links existing local user OR logs in existing Google user
8. Backend sets refresh token as httpOnly cookie (SameSite=Lax for cross-site redirect)
9. Backend redirects to: `http://localhost:4200/auth/callback#access_token={jwt}&expires_in=86400&token_type=Bearer`

**Error redirects:**
- OAuth cancelled: `http://localhost:4200/auth/login?error=oauth_cancelled&message=...`
- OAuth failed: `http://localhost:4200/auth/login?error=oauth_failed&message=...`
- Invalid domain: `http://localhost:4200/auth/login?error=invalid_domain&message=...`

**Configuration (appsettings.json):**
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
      "AllowedDomains": [],
      "FrontendUrl": "http://localhost:4200"
    }
  }
}
```

- If `AllowedDomains` is empty: any email domain is allowed (for development)
- If `AllowedDomains` has values (e.g., `["universidad.edu", "otro.edu"]`): only those domains are accepted

**New user behavior:**
- Created with `AuthProvider.Google`, `Role.None`
- Admin must assign a role before user can access protected areas

**Existing local user behavior:**
- Account is linked: `AuthProvider` changes to `Google`, `GoogleId` and `PhotoUrl` are set
- Existing role and data are preserved
- User can still use either login method

**Cookie note for OAuth callback:**
- Uses `SameSite=Lax` instead of `Strict` because the callback is a cross-site redirect (Google -> backend -> frontend)
- Regular login/refresh endpoints still use `SameSite=Strict`

**Testing:** 30 new unit tests added (303 total, all passing)

**Action for frontend agent:** Google OAuth endpoints are ready. To test end-to-end, you need Google Cloud Console credentials configured in appsettings. The frontend `OAuthCallbackComponent` should work as-is since the redirect format matches the documented contract.

**Still pending:**
- Google Cloud Console credentials (need real client_id/client_secret to test)
- Password forgot/reset/change endpoints
- Seed data (admin user)

### [2026-02-20] [dotnet-backend-engineer] [INFO] Google OAuth refactored with Google.Apis.Auth — no contract changes

Refactored Google OAuth 2.0 implementation to use `Google.Apis.Auth` for secure id_token validation.

**Changes are INTERNAL only — the frontend contract is IDENTICAL:**
- Same redirect flow: `GET /api/auth/google/login` → Google → `GET /api/auth/google/callback` → `{frontendUrl}/auth/callback#access_token=...`
- Same error redirect format: `{frontendUrl}/auth/login?error=oauth_failed&message=...`
- Same cookie behavior (SameSite=Lax, httpOnly, Path=/api/auth)
- Same access token in fragment (`#access_token={jwt}&expires_in=86400&token_type=Bearer`)

**Security improvements (invisible to frontend):**
1. `id_token` now validated with `GoogleJsonWebSignature.ValidateAsync()` — verifies RSA signature, audience (ClientId), issuer and expiration
2. OAuth `state` parameter now includes CSRF nonce: format `{nonce}:{returnUrl}` — nonce is a random GUID
3. URL construction delegated from controller to `IGoogleAuthService.BuildAuthorizationUrl()` — cleaner architecture
4. `FrontendUrl` config moved to `Authentication:Google:FrontendUrl` in appsettings (same value: "http://localhost:4200")
5. Open redirect prevention: returnUrl in state must start with '/' or falls back to '/dashboard'

**Tests:** 340 total (was 303). +37 new tests:
- Infrastructure.Tests: +26 (GoogleAuthServiceTests)
- WebAPI.Tests: +11 (Google OAuth endpoint tests in AuthControllerTests)

**Action for frontend agent:** No changes needed. The OAuth flow, token format and error format are identical to the previous implementation.

### [2026-02-20] [dotnet-backend-engineer] [FIX] AllowedDomains + Cookie Secure conditional + IWebHostEnvironment

**AllowedDomains (multi-domain support):**
- `GoogleAuthSettings.AllowedDomain` (string?) → `AllowedDomains` (List<string>) with init `= []`
- `LoginWithGoogleCommandHandler` updated: uses `.Any()` to verify email domain against list
- Empty list = any domain allowed (dev mode)

**Cookie Secure flag conditional on environment:**
- `AuthController` now injects `IWebHostEnvironment`
- `Secure = false` in Development (avoids self-signed cert issues)
- `Secure = true` in Production
- All 3 cookie methods updated (`SetRefreshTokenCookie`, `SetRefreshTokenCookieForOAuth`, `ClearRefreshTokenCookie`)

**Google Cloud Console credentials:**
- ClientId and ClientSecret configured in `appsettings.Development.json`
- `client_secret.json` added to `.gitignore` as backup

**Action for frontend agent:** No API contract changes. Cookie behavior is now more reliable in development (no Secure flag).

### [2026-02-20] [coordinator] [FIX] Angular proxy + session restoration — cookie persistence fix

**Problem:** Refresh token cookie disappeared on page reload because:
1. Cross-origin requests (`localhost:4200` → `localhost:7001`) — Chrome treats cookies as third-party
2. No session restoration logic — access token was only in memory (signal), lost on reload

**Fix applied (3 files):**

1. `apps/web-angular/proxy.conf.json` — NEW file:
   - Routes `/api/*` to `https://localhost:7001` with `secure: false`, `changeOrigin: true`
   - Wired in `angular.json` serve options: `"proxyConfig": "proxy.conf.json"`

2. `apps/web-angular/src/environments/environment.ts`:
   - `apiUrl` changed from `'https://localhost:7001'` to `''` (empty string)
   - All HTTP requests now use relative URLs through the proxy (same-origin)

3. `apps/web-angular/src/app/core/services/auth.service.ts`:
   - Added `initializeAuth(): Promise<void>` method
   - Calls `POST /api/auth/refresh` silently on startup
   - If successful: sets access token + fetches current user
   - If no cookie: silently catches error (no redirect)

4. `apps/web-angular/src/app/app.config.ts`:
   - Added `APP_INITIALIZER` that calls `authService.initializeAuth()`
   - Blocks app rendering until auth state is resolved
   - Guards work correctly because auth state is known before routes activate

**Action for frontend agent:** Proxy + session restoration are configured. All API requests now go through `http://localhost:4200/api/*` (same-origin). Dev server must be restarted after these changes.

---

## Questions / Pending Clarifications

(Empty - add questions here as they arise during implementation)

---

## Change History

| Date | Agent | Change | Impact |
|------|-------|--------|--------|
| 2026-02-17 | dotnet-backend-engineer | Created initial guide | None - baseline |
| 2026-02-17 | dotnet-backend-engineer | Auth Phase 1 implemented | 4 endpoints available |
| 2026-02-18 | angular-ux-engineer | Auth module fully implemented (88 tests) | Frontend ready for integration |
| 2026-02-20 | dotnet-backend-engineer | Google OAuth 2.0 implemented (30 new tests) | 6 endpoints available |
| 2026-02-20 | dotnet-backend-engineer | Google OAuth refactored with Google.Apis.Auth (+37 tests, 340 total) | No contract changes, internal security improvements |
| 2026-02-20 | dotnet-backend-engineer | AllowedDomains multi-domain + Cookie Secure conditional + IWebHostEnvironment | No API contract changes |
| 2026-02-20 | coordinator | Angular proxy + session restoration (APP_INITIALIZER) | Cookie persistence fix, apiUrl now empty |

---

**Last Updated:** 2026-02-20 (Angular proxy + session restoration)
**Next Review:** After seed admin user is implemented for E2E testing
