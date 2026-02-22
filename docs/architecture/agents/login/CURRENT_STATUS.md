# Auth Module - Current Status
## Last Updated: 2026-02-21

---

## Quick Resume

Usa este archivo para retomar el trabajo. Copia el bloque del agente que necesites y pegalo como prompt.

---

## Estado General

| Componente | Estado | Notas |
|-----------|--------|-------|
| PostgreSQL (Docker) | Running | `tools/services-docker/docker-compose.yml` |
| Migracion EF Core | Aplicada | `InitialCreate` - tablas Users + RefreshTokens |
| Backend Auth Core | COMPLETO | 9 endpoints auth funcionando |
| Backend Google OAuth | COMPLETO | Google.Apis.Auth, credenciales configuradas |
| Backend Password Mgmt | COMPLETO | forgot/reset/change endpoints + IEmailService (SMTP/MailerSend) |
| Frontend Angular Auth | COMPLETO | 88 tests passing, UI login + OAuth + password recovery |
| Angular Proxy | Configurado | `proxy.conf.json` → `https://localhost:7001` |
| Session Restoration | Configurado | APP_INITIALIZER + AuthService.initializeAuth() |
| Seed de datos | COMPLETO | 3 usuarios (admin, supervisor, beca) auto-creados |
| Tests totales backend | 433 passing | 98 Domain + 178 Application + 62 WebAPI + 95 Infrastructure |
| **RF-001 Login** | **COMPLETO** | Backend + Frontend + Seed + Proxy + Session restore |
| **RF-002 Google OAuth** | **COMPLETO** | Google.Apis.Auth + credenciales + Angular popup flow |

---

## Requerimientos AUTH - Progreso

| RF | Nombre | Backend | Frontend | Estado |
|----|--------|---------|----------|--------|
| RF-001 | Login email/password | DONE | DONE | **COMPLETO** |
| RF-002 | Google OAuth | DONE | DONE | **COMPLETO** |
| RF-003 | Roles y permisos | 40% | 100% (guards) | Pendiente: endpoint cambio de rol |
| RF-004 | Recuperar contrasena | DONE | DONE (UI) | **Backend + Frontend listos** |
| RF-005 | Cambiar contrasena | DONE | Parcial (service wired) | Pendiente: UI cambio en perfil |

---

## Seed de Datos de Desarrollo

| Email | Password | Rol |
|-------|----------|-----|
| admin@test.com | Admin123! | Admin |
| supervisor@test.com | Super123! | Supervisor |
| beca@test.com | Beca123! | Beca |

- `DatabaseSeeder.SeedDevelopmentUsersAsync()` en `Infrastructure/Data/`
- Llamado desde `Program.cs` solo en Development
- Idempotente (verifica por email antes de crear)
- `MigrateAsync()` corre antes del seed

---

## Proximos Pasos (en orden de prioridad)

### 1. Endpoint faltante de auth (RF-003)
- `PUT /api/users/{id}/role` - Cambiar rol (solo ADMIN)

### 2. UI de cambio de contrasena (RF-005 frontend)
- Componente en area autenticada de Angular
- Usa `PUT /api/auth/password/change` (ya implementado en backend)

### 3. Fase 1 MVP - Siguiente modulo
- Gestion de ciclos (CICLO: RF-006 a RF-012)
- Gestion de ubicaciones (UBIC: RF-023 a RF-028)

---

## Prompt para resumir agente Backend (.NET)

```
Lee tu memoria de contexto:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/.claude/agent-memory-local/dotnet-backend-engineer/CONTEXT.md

Lee el estado actual del modulo auth:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/CURRENT_STATUS.md

Lee el sync log para saber que hizo el frontend:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/SYNC_LOG.md

Lee el contrato API que debes cumplir:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/FRONTEND_AUTH_GUIDE.md

Estado actual:
- PostgreSQL corriendo en Docker (localhost:5432)
- Migracion InitialCreate aplicada (tablas Users, RefreshTokens)
- 9 endpoints implementados: POST login, POST refresh, POST logout, GET me, GET google/login, GET google/callback, POST password/forgot, POST password/reset, PUT password/change
- 433 tests passing (98 Domain + 178 Application + 95 Infrastructure + 62 WebAPI)
- RF-001 (Login), RF-002 (Google OAuth), RF-004 (Password Recovery), RF-005 (Change Password) COMPLETADOS en backend
- IEmailService + SmtpEmailService (MailKit/MailerSend SMTP) activo; ResendEmailService mantenido como alternativa
- Seed: 3 usuarios de prueba (admin, supervisor, beca) auto-creados en Development
- Pendiente: endpoint cambio de rol PUT /api/users/{id}/role (RF-003)

Continua con: [DESCRIBE LA TAREA AQUI]
```

---

## Prompt para resumir agente Frontend (Angular)

```
Lee el estado actual del modulo auth:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/CURRENT_STATUS.md

Lee el sync log para saber que hizo el backend:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/SYNC_LOG.md

Lee el contrato API:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/login/FRONTEND_AUTH_GUIDE.md

Explora el codigo Angular existente:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/apps/web-angular/

Estado actual:
- Auth module implementado con 88 tests
- RF-001 (Login) y RF-002 (Google OAuth) COMPLETADOS end-to-end
- Guards, interceptors, AuthService con signals
- UI: login, forgot-password, reset-password, oauth-callback
- Proxy configurado: proxy.conf.json → https://localhost:7001 (same-origin cookies)
- environment.ts apiUrl = '' (empty, uses proxy)
- APP_INITIALIZER: AuthService.initializeAuth() restaura sesion desde cookie al recargar
- Seed: backend tiene 3 usuarios de prueba (admin@test.com/Admin123!, etc.)
- Backend: RF-004 (forgot/reset) y RF-005 (change password) endpoints COMPLETADOS
- Pendiente: UI de cambio de contrasena (en area autenticada, usa PUT /api/auth/password/change)

Continua con: [DESCRIBE LA TAREA AQUI]
```

---

## Archivos clave del proyecto

### Documentacion
- `docs/requirements/es/REQUIREMENTS_COMPLETE.md` - 54 RFs completos
- `docs/architecture/agents/login/FRONTEND_AUTH_GUIDE.md` - Contrato API
- `docs/architecture/agents/login/SYNC_LOG.md` - Log de coordinacion
- `docs/architecture/agents/login/CURRENT_STATUS.md` - Este archivo

### Backend
- `apps/api/WorkScholarship.sln` - Solucion .NET
- `apps/api/src/WorkScholarship.WebAPI/Program.cs` - Punto de entrada + seed
- `apps/api/src/WorkScholarship.WebAPI/Controllers/AuthController.cs` - 9 endpoints auth
- `apps/api/src/WorkScholarship.Infrastructure/Data/DatabaseSeeder.cs` - Seed de usuarios dev
- `apps/api/src/WorkScholarship.Application/Features/Auth/` - Commands y Queries
- `apps/api/src/WorkScholarship.Domain/Entities/User.cs` - Entidad User rica
- `apps/api/src/WorkScholarship.Infrastructure/Identity/` - JWT, Password, CurrentUser, GoogleAuth

### Frontend Angular
- `apps/web-angular/` - Raiz del proyecto Angular
- `apps/web-angular/proxy.conf.json` - Proxy a backend
- `apps/web-angular/src/app/app.config.ts` - APP_INITIALIZER para session restore

### Infraestructura
- `tools/services-docker/docker-compose.yml` - PostgreSQL

### Memoria de agentes
- `.claude/agent-memory-local/dotnet-backend-engineer/CONTEXT.md`
- `.claude/agent-memory/angular-ux-engineer/MEMORY.md`
