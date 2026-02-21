# Auth Module - Current Status
## Last Updated: 2026-02-20

---

## Quick Resume

Usa este archivo para retomar el trabajo. Copia el bloque del agente que necesites y pegalo como prompt.

---

## Estado General

| Componente | Estado | Notas |
|-----------|--------|-------|
| PostgreSQL (Docker) | Running | `tools/services-docker/docker-compose.yml` |
| Migracion EF Core | Aplicada | `InitialCreate` - tablas Users + RefreshTokens |
| Backend Fase 1 | Completo | 4 endpoints auth core funcionando |
| Backend Google OAuth | Completo | 2 endpoints OAuth con Google.Apis.Auth, credenciales configuradas |
| Frontend Angular Auth | Completo | 88 tests passing |
| Angular Proxy | Configurado | `proxy.conf.json` → `https://localhost:7001` |
| Session Restoration | Configurado | APP_INITIALIZER + AuthService.initializeAuth() |
| Seed de datos | Pendiente | No hay usuario admin para probar |
| Tests totales backend | 340 passing | 98 Domain + 114 Application + 51 WebAPI + 77 Infrastructure |

---

## Requerimientos AUTH - Progreso

| RF | Nombre | Backend | Frontend | Blocker |
|----|--------|---------|----------|---------|
| RF-001 | Login email/password | 95% | 100% | Falta seed admin |
| RF-002 | Google OAuth | 90% | 100% (mock) | Falta credenciales Google Cloud Console |
| RF-003 | Roles y permisos | 40% | 100% (guards) | Falta endpoint cambio de rol |
| RF-004 | Recuperar contrasena | 20% | 100% (UI) | Falta endpoints BE + IEmailService |
| RF-005 | Cambiar contrasena | 15% | Parcial | Falta endpoint BE + UI en perfil |

---

## Proximos Pasos (en orden de prioridad)

### 1. Seed de usuario admin (BLOCKER)
Sin esto no se puede probar nada end-to-end.
- Crear un seeder que inserte un admin con password hasheado
- Puede ser un endpoint temporal o un migration seed

### 2. Endpoints faltantes de auth (RF-003, RF-004, RF-005)
- `PUT /api/auth/password/change` - Cambiar contrasena (autenticado)
- `POST /api/auth/password/forgot` - Solicitar reset
- `POST /api/auth/password/reset` - Resetear con token
- `PUT /api/users/{id}/role` - Cambiar rol (solo ADMIN)

### 3. Google OAuth credenciales - COMPLETADO
- Proyecto creado en Google Cloud Console
- ClientId y ClientSecret configurados en `appsettings.Development.json`
- Redirect URI configurada: `https://localhost:7001/api/auth/google/callback`
- `client_secret.json` en `.gitignore` como backup

### 4. IEmailService
- Necesario para RF-004 (reset password) y RF-005 (notificar cambio)
- Implementacion: puede ser SendGrid, SMTP, o console logger para dev

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
- 6 endpoints implementados: POST login, POST refresh, POST logout, GET me, GET google/login, GET google/callback
- 340 tests passing (Google OAuth refactor + AllowedDomains + IWebHostEnvironment)
- Google Cloud Console credentials configured in appsettings.Development.json
- AllowedDomains changed from string to List<string>
- AuthController injects IWebHostEnvironment for conditional Secure cookie flag
- Falta: seed admin, endpoints de password (forgot/reset/change), endpoint cambio de rol

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
- Guards, interceptors, AuthService con signals
- UI: login, forgot-password, reset-password, oauth-callback
- Proxy configurado: proxy.conf.json → https://localhost:7001 (same-origin cookies)
- environment.ts apiUrl = '' (empty, uses proxy)
- APP_INITIALIZER: AuthService.initializeAuth() restaura sesion desde cookie al recargar
- Pendiente: UI de cambio de contrasena (en area autenticada)
- Backend Google OAuth endpoints disponibles y configurados con credenciales reales

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
- `apps/api/src/WorkScholarship.WebAPI/Program.cs` - Punto de entrada
- `apps/api/src/WorkScholarship.WebAPI/Controllers/AuthController.cs` - Endpoints auth (6 endpoints)
- `apps/api/src/WorkScholarship.Application/Features/Auth/` - Commands y Queries
- `apps/api/src/WorkScholarship.Application/Features/Auth/Commands/LoginWithGoogle/` - Google OAuth command
- `apps/api/src/WorkScholarship.Application/Common/Interfaces/IGoogleAuthService.cs` - Interface OAuth
- `apps/api/src/WorkScholarship.Application/Common/Models/GoogleAuthSettings.cs` - Config OAuth
- `apps/api/src/WorkScholarship.Domain/Entities/User.cs` - Entidad User rica
- `apps/api/src/WorkScholarship.Infrastructure/Identity/GoogleAuthService.cs` - Impl OAuth
- `apps/api/src/WorkScholarship.Infrastructure/Identity/` - JWT, Password, CurrentUser, GoogleAuth

### Frontend Angular
- `apps/web-angular/` - Raiz del proyecto Angular

### Infraestructura
- `tools/services-docker/docker-compose.yml` - PostgreSQL

### Memoria de agentes
- `.claude/agent-memory-local/dotnet-backend-engineer/CONTEXT.md`
