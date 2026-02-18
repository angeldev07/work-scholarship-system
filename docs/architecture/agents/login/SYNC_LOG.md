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

1. **Base API URL:** Use `https://localhost:7001` for development
2. **CORS:** Backend is configured to allow `http://localhost:4200`
3. **Cookies:** Set `withCredentials: true` in HttpClient for refresh token cookie
4. **Token Storage:** Store access token in service variable (memory), NOT localStorage
5. **Error Format:** All errors follow `ApiResponse<T>` format with `error.code` and `error.message`
6. **Testing:** Mock service provided in guide for development without full backend

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

---

**Last Updated:** 2026-02-18
**Next Review:** When PostgreSQL + migrations are ready for real integration testing
