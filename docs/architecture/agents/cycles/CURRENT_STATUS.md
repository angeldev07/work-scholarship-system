# Cycle Management Module — Current Status
## Last Updated: 2026-02-26 (Bloque 1 completado por backend)

---

## Quick Resume

Usa este archivo para retomar el trabajo. Copia el bloque del agente que necesites y pegalo como prompt.

---

## Estado General

| Componente | Estado | Notas |
|-----------|--------|-------|
| PostgreSQL (Docker) | Running | `tools/services-docker/docker-compose.yml` |
| Migracion EF Core | COMPLETO | `InitialCreate` + `AddCyclesAndLocations` aplicadas |
| Architecture Doc | COMPLETO | `docs/architecture/cycles/CYCLE_ARCHITECTURE.md` v2.0 |
| Domain Entities | **COMPLETO** | Cycle, CycleStatus, Location, CycleLocation, SupervisorAssignment, ScheduleSlot |
| Domain Events | **COMPLETO** | 7 eventos para transiciones del ciclo |
| EF Core Config | **COMPLETO** | 5 configuraciones + 11 índices |
| Application Layer | NO INICIADO | Commands, Queries, Validators (Bloque 2) |
| Infrastructure | **COMPLETO** (Bloque 1) | Configs + migration AddCyclesAndLocations |
| WebAPI Controller | NO INICIADO | CyclesController (Bloque 2) |
| Backend Tests | **COMPLETO** (Bloque 1) | 133 nuevos tests Domain (566 total pasando) |
| Frontend Angular | NO INICIADO | Dashboard, Wizard, Cycle management pages |
| Frontend Tests | NO INICIADO | Component + service tests |

---

## Requerimientos CICLO — Progreso

| RF | Nombre | Backend | Frontend | Estado |
|----|--------|---------|----------|--------|
| RF-006 | Crear Nuevo Ciclo | NOT STARTED | NOT STARTED | Pendiente |
| RF-007 | Configurar Ciclo | NOT STARTED | NOT STARTED | Pendiente |
| RF-008 | Abrir Postulaciones | NOT STARTED | NOT STARTED | Pendiente |
| RF-009 | Cerrar Postulaciones | NOT STARTED | NOT STARTED | Pendiente |
| RF-010 | Extender Fechas | NOT STARTED | NOT STARTED | Pendiente |
| RF-011 | Cerrar Ciclo | NOT STARTED | NOT STARTED | Pendiente |
| RF-012 | Ver Historial | NOT STARTED | NOT STARTED | Pendiente |

---

## Prerequisito: Location Entity (UBIC subsystem)

El ciclo depende de `Location` (catalogo maestro de ubicaciones). La entidad `Location` debe existir antes o crearse junto con `CycleLocation`. Ver `IMPLEMENTATION_GUIDE.md` Bloque 1, Tarea 2.

---

## Dependencias entre subsistemas

```
Cycle (CICLO) depende de:
  - Location (UBIC) — CycleLocation FK a Location
  - User (AUTH) — SupervisorAssignment FK a User (rol SUPERVISOR)

Cycle (CICLO) es dependencia de:
  - Selection (SEL) — Application.CycleId, ScholarAssignment.CycleId
  - Tracking (TRACK) — via ScholarAssignment
  - Absences (AUS) — via ScholarAssignment
  - Documents (DOC) — Document.CycleId
```

---

## Prompt para resumir agente Backend (.NET)

```
Lee tu memoria de contexto:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/.claude/agent-memory-local/dotnet-backend-engineer/CONTEXT.md

Lee el estado actual del modulo ciclos:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/CURRENT_STATUS.md

Lee la guia de implementacion con las 26 tareas:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/IMPLEMENTATION_GUIDE.md

Lee el documento de arquitectura completo:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/cycles/CYCLE_ARCHITECTURE.md

Lee el sync log para saber que hizo el frontend:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/SYNC_LOG.md

Estado actual:
- PostgreSQL corriendo en Docker (localhost:5432)
- Migracion InitialCreate aplicada (tablas Users, RefreshTokens)
- 433 tests backend pasando (98 Domain + 178 Application + 95 Infrastructure + 62 WebAPI)
- Auth module COMPLETO (9 endpoints)
- Backoffice Shell COMPLETO (routes + placeholders para todas las features)
- NO se ha iniciado el modulo de ciclos
- Documento de arquitectura CYCLE_ARCHITECTURE.md v2.0 listo como blueprint

Continua con: [DESCRIBE LA TAREA AQUI]
```

---

## Prompt para resumir agente Frontend (Angular)

```
Lee el estado actual del modulo ciclos:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/CURRENT_STATUS.md

Lee la guia de implementacion con las 26 tareas:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/IMPLEMENTATION_GUIDE.md

Lee el documento de arquitectura (especialmente secciones 10, 13, 15):
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/cycles/CYCLE_ARCHITECTURE.md

Lee el sync log para saber que hizo el backend:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/docs/architecture/agents/cycles/SYNC_LOG.md

Explora el codigo Angular existente:
C:/Users/angel/OneDrive/Escritorio/Development/proyectos portafolio/proyectos-biblioteca/work scholarship/work-scholarship-system/apps/web-angular/

Estado actual:
- Auth module COMPLETO (183 tests passing)
- Backoffice Shell COMPLETO (ShellComponent + NavigationService + role-based routes)
- Placeholders existentes para: admin/cycles/* (list, create, detail, configure, history)
- NO se ha iniciado el modulo de ciclos en frontend
- Backend tampoco ha iniciado — frontend puede empezar mocks primero
- Documento de arquitectura tiene wireframes ASCII del wizard (seccion 13.5)

Continua con: [DESCRIBE LA TAREA AQUI]
```

---

## Archivos clave

### Documentacion
- `docs/architecture/cycles/CYCLE_ARCHITECTURE.md` — Arquitectura completa (16 secciones)
- `docs/architecture/agents/cycles/IMPLEMENTATION_GUIDE.md` — 26 tareas en 6 bloques
- `docs/architecture/agents/cycles/SYNC_LOG.md` — Log de coordinacion
- `docs/architecture/agents/cycles/CURRENT_STATUS.md` — Este archivo
- `docs/requirements/es/REQUIREMENTS_COMPLETE.md` — RF-006 a RF-012

### Backend (existente a referenciar)
- `apps/api/src/WorkScholarship.Domain/Common/BaseEntity.cs` — Patron base entity
- `apps/api/src/WorkScholarship.Domain/Entities/User.cs` — Patron rich entity a seguir
- `apps/api/src/WorkScholarship.Domain/Enums/UserRole.cs` — Patron enum
- `apps/api/src/WorkScholarship.WebAPI/Controllers/AuthController.cs` — Patron controller
- `apps/api/src/WorkScholarship.Application/Features/Auth/` — Patron CQRS features

### Backend (a crear)
- `apps/api/src/WorkScholarship.Domain/Enums/CycleStatus.cs`
- `apps/api/src/WorkScholarship.Domain/Entities/Cycle.cs`
- `apps/api/src/WorkScholarship.Domain/Entities/Location.cs`
- `apps/api/src/WorkScholarship.Domain/Entities/CycleLocation.cs`
- `apps/api/src/WorkScholarship.Domain/Entities/SupervisorAssignment.cs`
- `apps/api/src/WorkScholarship.Application/Features/Cycles/`
- `apps/api/src/WorkScholarship.WebAPI/Controllers/CyclesController.cs`

### Frontend Angular (existente)
- `apps/web-angular/src/app/features/admin/cycles/` — Placeholder routes ya existen
- `apps/web-angular/src/app/core/services/` — Donde crear CycleService

### Memoria de agentes
- `.claude/agent-memory-local/dotnet-backend-engineer/CONTEXT.md`
- `.claude/agent-memory/angular-ux-engineer/MEMORY.md`
