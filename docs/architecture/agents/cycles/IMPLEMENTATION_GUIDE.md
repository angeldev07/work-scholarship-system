# Cycle Management — Implementation Guide
## 26 Tasks in 6 Blocks

**Version:** 1.0
**Date:** 2026-02-26
**Reference:** `docs/architecture/cycles/CYCLE_ARCHITECTURE.md` v2.0

---

## Overview

Este documento define las 26 tareas incrementales para implementar el subsistema de Ciclos (RF-006 a RF-012). Cada tarea es autocontenida y construye sobre las anteriores. Los bloques estan ordenados por dependencia.

**Convenciones:**
- `[BE]` = Backend (.NET) task
- `[FE]` = Frontend (Angular) task
- `[BOTH]` = Requiere coordinacion entre ambos

---

## Bloque 1: Fundamentos (Domain + Location prerequisite)

> **Objetivo:** Establecer las entidades de dominio base y la migracion.
> **Prerequisito:** Ninguno — este es el punto de partida.

### Tarea 1 — [BE] CycleStatus enum + Cycle entity

**Que hacer:**
- Crear `Domain/Enums/CycleStatus.cs` con 5 valores (Configuration, ApplicationsOpen, ApplicationsClosed, Active, Closed)
- Crear `Domain/Entities/Cycle.cs` con:
  - Todas las propiedades de la seccion 2.1 del doc de arquitectura
  - Constructor privado
  - Factory method `Create()` con validaciones de dominio
  - State transition methods: `OpenApplications()`, `CloseApplications()`, `ReopenApplications()`, `Activate()`, `Close()`, `ExtendDates()`
  - Query methods: `IsModifiable`, `AcceptsApplications`, `IsOperational`
  - `RenewalProcessCompleted` y `ClonedFromCycleId` como properties

**Patron a seguir:** `User.cs` (private setters, static Create(), domain methods con Result<T>)

**Tests:**
- CycleTests.cs: Create() con datos validos, Create() con datos invalidos, cada transicion valida, cada transicion invalida, propiedades computed

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Domain/Enums/CycleStatus.cs
apps/api/src/WorkScholarship.Domain/Entities/Cycle.cs
apps/api/tests/WorkScholarship.Domain.Tests/Entities/CycleTests.cs
```

**Definition of Done:**
- [ ] CycleStatus enum con 5 valores y XML docs
- [ ] Cycle entity con todas las propiedades
- [ ] Create() factory con validaciones
- [ ] 6 state transition methods con guards
- [ ] Tests para cada transicion valida e invalida
- [ ] `dotnet build` sin errores

---

### Tarea 2 — [BE] Location entity (master catalog)

**Que hacer:**
- Crear `Domain/Entities/Location.cs` como entidad maestra (atemporal)
- Propiedades: Name, Description, Address, ImageUrl, Department, IsActive
- Factory method `Create()` con validaciones
- Method `Deactivate()` / `Activate()`

**Nota:** Location es parte del subsistema UBIC (RF-023), pero se necesita aqui como prerequisito para CycleLocation. Solo la entidad de dominio — el CRUD completo de ubicaciones se implementa despues.

**Tests:**
- LocationTests.cs: Create() valido/invalido, Activate/Deactivate

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Domain/Entities/Location.cs
apps/api/tests/WorkScholarship.Domain.Tests/Entities/LocationTests.cs
```

**Definition of Done:**
- [ ] Location entity con propiedades y factory method
- [ ] Tests de creacion y metodos de comportamiento
- [ ] `dotnet build` sin errores

---

### Tarea 3 — [BE] CycleLocation + SupervisorAssignment + ScheduleSlot entities

**Que hacer:**
- Crear `Domain/Entities/CycleLocation.cs` (junction table Cycle-Location)
  - Properties: CycleId, LocationId, ScholarshipsAvailable, ScholarshipsAssigned, IsActive
  - Navigation: Cycle, Location, ScheduleSlots, SupervisorAssignments
- Crear `Domain/Entities/SupervisorAssignment.cs`
  - Properties: CycleId, SupervisorId (FK -> User), CycleLocationId, AssignedAt
  - Navigation: Cycle, Supervisor (User), CycleLocation
- Crear `Domain/Entities/ScheduleSlot.cs`
  - Properties: CycleLocationId, DayOfWeek, StartTime, EndTime, RequiredScholars
  - Navigation: CycleLocation

**Tests:**
- CycleLocationTests.cs, SupervisorAssignmentTests.cs, ScheduleSlotTests.cs

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Domain/Entities/CycleLocation.cs
apps/api/src/WorkScholarship.Domain/Entities/SupervisorAssignment.cs
apps/api/src/WorkScholarship.Domain/Entities/ScheduleSlot.cs
apps/api/tests/WorkScholarship.Domain.Tests/Entities/CycleLocationTests.cs
apps/api/tests/WorkScholarship.Domain.Tests/Entities/SupervisorAssignmentTests.cs
apps/api/tests/WorkScholarship.Domain.Tests/Entities/ScheduleSlotTests.cs
```

**Definition of Done:**
- [ ] 3 entities con propiedades, factory methods, validaciones
- [ ] Navigation properties configuradas
- [ ] Tests por entity
- [ ] `dotnet build` sin errores

---

### Tarea 4 — [BE] Domain Events

**Que hacer:**
- Crear domain events para las transiciones del ciclo:
  - `CycleCreatedEvent(Guid CycleId)`
  - `ApplicationsOpenedEvent(Guid CycleId)`
  - `ApplicationsClosedEvent(Guid CycleId)`
  - `ApplicationsReopenedEvent(Guid CycleId)`
  - `CycleActivatedEvent(Guid CycleId)`
  - `CycleDatesExtendedEvent(Guid CycleId)`
  - `CycleClosedEvent(Guid CycleId)`

**Patron a seguir:** Si existen domain events en el codebase, seguir el mismo patron. Si no, crear `Domain/Events/` directory con records.

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Domain/Events/CycleCreatedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/ApplicationsOpenedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/ApplicationsClosedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/ApplicationsReopenedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/CycleActivatedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/CycleDatesExtendedEvent.cs
apps/api/src/WorkScholarship.Domain/Events/CycleClosedEvent.cs
```

**Definition of Done:**
- [ ] 7 domain event records
- [ ] Cycle entity AddDomainEvent() calls verified in each transition
- [ ] `dotnet build` sin errores

---

### Tarea 5 — [BE] EF Core configuration + migration

**Que hacer:**
- Crear `Infrastructure/Data/Configurations/CycleConfiguration.cs`
- Crear `Infrastructure/Data/Configurations/LocationConfiguration.cs`
- Crear `Infrastructure/Data/Configurations/CycleLocationConfiguration.cs`
- Crear `Infrastructure/Data/Configurations/SupervisorAssignmentConfiguration.cs`
- Crear `Infrastructure/Data/Configurations/ScheduleSlotConfiguration.cs`
- Add DbSets to `ApplicationDbContext`
- Create migration: `AddCyclesAndLocations`

**Indices sugeridos:**
```
IX_Cycles_Department_Status (para validar RN-001: 1 activo por dept)
IX_CycleLocations_CycleId
IX_SupervisorAssignments_CycleId
IX_SupervisorAssignments_SupervisorId
IX_ScheduleSlots_CycleLocationId
```

**Tests:**
- Integration tests para verificar que las configuraciones EF Core son correctas

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Infrastructure/Data/Configurations/CycleConfiguration.cs
apps/api/src/WorkScholarship.Infrastructure/Data/Configurations/LocationConfiguration.cs
apps/api/src/WorkScholarship.Infrastructure/Data/Configurations/CycleLocationConfiguration.cs
apps/api/src/WorkScholarship.Infrastructure/Data/Configurations/SupervisorAssignmentConfiguration.cs
apps/api/src/WorkScholarship.Infrastructure/Data/Configurations/ScheduleSlotConfiguration.cs
```

**Definition of Done:**
- [ ] 5 entity configurations
- [ ] DbSets in ApplicationDbContext
- [ ] Migration created and applied successfully
- [ ] Indices created
- [ ] `dotnet ef database update` works
- [ ] All existing tests still pass

---

## Bloque 2: Configuracion del Ciclo (CQRS + Controller)

> **Objetivo:** CRUD basico del ciclo + configuracion.
> **Prerequisito:** Bloque 1 completo.

### Tarea 6 — [BE] CreateCycleCommand + Handler + Validator

**Que hacer:**
- `Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommand.cs`
- `Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommandHandler.cs`
- `Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommandValidator.cs`
- Handler logic:
  1. Validate no active cycle for department (RN-001)
  2. Create Cycle via factory
  3. If `cloneFromCycleId` provided: load previous cycle's CycleLocations + SupervisorAssignments + ScheduleSlots and clone them
  4. If first cycle for department: auto-set `RenewalProcessCompleted = true`
  5. Save and return CycleDto

**Request/Response contract:**

```
POST /api/cycles
Request:
{
  "name": "2024-2",
  "department": "Biblioteca",
  "startDate": "2024-08-01",
  "endDate": "2024-12-15",
  "applicationDeadline": "2024-08-15",
  "interviewDate": "2024-08-22",
  "selectionDate": "2024-08-29",
  "totalScholarshipsAvailable": 25,
  "cloneFromCycleId": "guid-or-null"
}

Response (201):
{
  "success": true,
  "data": {
    "id": "guid",
    "name": "2024-2",
    "department": "Biblioteca",
    "status": "Configuration",
    "startDate": "2024-08-01",
    "endDate": "2024-12-15",
    "applicationDeadline": "2024-08-15",
    "interviewDate": "2024-08-22",
    "selectionDate": "2024-08-29",
    "totalScholarshipsAvailable": 25,
    "totalScholarshipsAssigned": 0,
    "renewalProcessCompleted": false,
    "clonedFromCycleId": "guid-or-null",
    "locationsCount": 5,
    "supervisorsCount": 3,
    "createdAt": "2024-07-15T..."
  }
}
```

**Validation rules (FluentValidation):**
```
Name: NotEmpty, MaxLength(100)
Department: NotEmpty, MaxLength(100)
StartDate: GreaterThan(DateTime.UtcNow)
EndDate: GreaterThan(StartDate)
ApplicationDeadline: GreaterThan(DateTime.UtcNow), LessThan(InterviewDate)
InterviewDate: LessThan(SelectionDate)
SelectionDate: LessThan(EndDate)
TotalScholarshipsAvailable: GreaterThan(0)
CloneFromCycleId: Must exist if provided, must be Closed status
```

**Tests:**
- Handler: create sin clone, create con clone, create duplicado (RN-001), create primer ciclo (auto-renewal)
- Validator: cada regla

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommand.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommandHandler.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/CreateCycle/CreateCycleCommandValidator.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/DTOs/CycleDto.cs
apps/api/tests/WorkScholarship.Application.Tests/Features/Cycles/Commands/CreateCycleCommandHandlerTests.cs
apps/api/tests/WorkScholarship.Application.Tests/Features/Cycles/Commands/CreateCycleCommandValidatorTests.cs
```

---

### Tarea 7 — [BE] ConfigureCycleCommand (add/remove locations, supervisors, schedules)

**Que hacer:**
- Command that accepts arrays of locations, supervisors, and schedules to configure
- Handler creates/updates/removes CycleLocations, SupervisorAssignments, ScheduleSlots
- Only works in Configuration status

**Request contract:**

```
PUT /api/cycles/{id}/configure
Request:
{
  "locations": [
    {
      "locationId": "guid",
      "scholarshipsAvailable": 3,
      "isActive": true,
      "scheduleSlots": [
        { "dayOfWeek": 1, "startTime": "08:00", "endTime": "10:00", "requiredScholars": 2 }
      ]
    }
  ],
  "supervisorAssignments": [
    {
      "supervisorId": "guid",
      "cycleLocationId": "guid"
    }
  ]
}

Response (200):
{
  "success": true,
  "data": { CycleDto with updated counts }
}
```

**Tests:**
- Add locations, remove location, update scholarships, assign supervisor, replace supervisor, invalid state (not Configuration)

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/ConfigureCycle/ConfigureCycleCommand.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/ConfigureCycle/ConfigureCycleCommandHandler.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/ConfigureCycle/ConfigureCycleCommandValidator.cs
apps/api/tests/WorkScholarship.Application.Tests/Features/Cycles/Commands/ConfigureCycleCommandHandlerTests.cs
```

---

### Tarea 8 — [BE] GetCycleByIdQuery + ListCyclesQuery + GetActiveCycleQuery

**Que hacer:**
- `GetCycleByIdQuery(Guid Id)` → returns CycleDetailDto (cycle + location count + supervisor count + scholar count)
- `ListCyclesQuery(string? Department, int? Year, CycleStatus? Status, int Page, int PageSize)` → paginated list
- `GetActiveCycleQuery(string Department)` → returns CycleDto or null

**Response contract (CycleDetailDto):**
```json
{
  "id": "guid",
  "name": "2024-2",
  "department": "Biblioteca",
  "status": "Configuration",
  "startDate": "...",
  "endDate": "...",
  "applicationDeadline": "...",
  "interviewDate": "...",
  "selectionDate": "...",
  "totalScholarshipsAvailable": 25,
  "totalScholarshipsAssigned": 0,
  "renewalProcessCompleted": false,
  "clonedFromCycleId": null,
  "closedAt": null,
  "closedBy": null,
  "createdAt": "...",
  "updatedAt": "...",
  "locationsCount": 5,
  "supervisorsCount": 3,
  "scholarsCount": 0
}
```

**Tests:**
- Each query with valid and empty data

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/GetCycleById/GetCycleByIdQuery.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/GetCycleById/GetCycleByIdQueryHandler.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/ListCycles/ListCyclesQuery.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/ListCycles/ListCyclesQueryHandler.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/GetActiveCycle/GetActiveCycleQuery.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/Queries/GetActiveCycle/GetActiveCycleQueryHandler.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/DTOs/CycleDetailDto.cs
apps/api/src/WorkScholarship.Application/Features/Cycles/DTOs/CycleListItemDto.cs
apps/api/tests/WorkScholarship.Application.Tests/Features/Cycles/Queries/
```

---

### Tarea 9 — [BE] CyclesController (core endpoints)

**Que hacer:**
- `POST /api/cycles` → CreateCycleCommand
- `GET /api/cycles` → ListCyclesQuery
- `GET /api/cycles/active` → GetActiveCycleQuery
- `GET /api/cycles/{id}` → GetCycleByIdQuery
- `PUT /api/cycles/{id}/configure` → ConfigureCycleCommand
- All endpoints require `[Authorize(Roles = "Admin")]`
- Follow AuthController pattern for ApiResponse<T> wrapping

**Tests:**
- CyclesControllerTests: cada endpoint, autorizacion, respuestas

**Archivos a crear:**
```
apps/api/src/WorkScholarship.WebAPI/Controllers/CyclesController.cs
apps/api/tests/WorkScholarship.WebAPI.Tests/Controllers/CyclesControllerTests.cs
```

---

### Tarea 10 — [BE] GetAdminDashboardStateQuery

**Que hacer:**
- New endpoint: `GET /api/admin/dashboard-state?department={dept}`
- Returns the complete state needed by the frontend to decide what to show

**Response contract:**
```json
{
  "success": true,
  "data": {
    "hasLocations": true,
    "locationsCount": 5,
    "hasSupervisors": true,
    "supervisorsCount": 3,
    "activeCycle": { CycleDto or null },
    "lastClosedCycle": { CycleDto or null },
    "cycleInConfiguration": { CycleDto or null },
    "pendingActions": [
      "NO_ACTIVE_CYCLE",
      "CYCLE_NEEDS_LOCATIONS",
      "CYCLE_NEEDS_SUPERVISORS",
      "RENEWALS_PENDING"
    ]
  }
}
```

**Logic:**
1. Count Locations WHERE Department = dept AND IsActive = true → hasLocations
2. Count Users WHERE Role = Supervisor AND IsActive = true → hasSupervisors
3. Get Cycle WHERE Department = dept AND Status != Closed ORDER BY CreatedAt DESC → activeCycle / cycleInConfiguration
4. Get Cycle WHERE Department = dept AND Status = Closed ORDER BY ClosedAt DESC LIMIT 1 → lastClosedCycle
5. Build pendingActions array based on state

**This query is the "health check" that replaces persisted SetupCompleted flags.**

**Tests:**
- Empty state, state with locations but no supervisors, state with cycle in config, state with active cycle, state with all complete

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Application/Features/Admin/Queries/GetDashboardState/GetAdminDashboardStateQuery.cs
apps/api/src/WorkScholarship.Application/Features/Admin/Queries/GetDashboardState/GetAdminDashboardStateQueryHandler.cs
apps/api/src/WorkScholarship.Application/Features/Admin/DTOs/AdminDashboardStateDto.cs
apps/api/tests/WorkScholarship.Application.Tests/Features/Admin/Queries/GetAdminDashboardStateQueryHandlerTests.cs
```

---

### Tarea 11 — [FE] CycleService + types

**Que hacer:**
- Create `core/services/cycle.service.ts` with methods for all cycle endpoints
- Create TypeScript interfaces matching the API contract DTOs
- Wire up HttpClient with proper error handling (follow AuthService pattern)

**Methods:**
```typescript
createCycle(data: CreateCycleRequest): Observable<ApiResponse<CycleDto>>
listCycles(params: ListCyclesParams): Observable<ApiResponse<PaginatedList<CycleListItemDto>>>
getActiveCycle(department: string): Observable<ApiResponse<CycleDto | null>>
getCycleById(id: string): Observable<ApiResponse<CycleDetailDto>>
configureCycle(id: string, data: ConfigureCycleRequest): Observable<ApiResponse<CycleDto>>
openApplications(id: string): Observable<ApiResponse<CycleDto>>
closeApplications(id: string): Observable<ApiResponse<CycleDto>>
reopenApplications(id: string): Observable<ApiResponse<CycleDto>>
extendDates(id: string, data: ExtendDatesRequest): Observable<ApiResponse<CycleDto>>
closeCycle(id: string): Observable<ApiResponse<CycleDto>>
getDashboardState(department: string): Observable<ApiResponse<AdminDashboardStateDto>>
```

**Tests:**
- CycleService unit tests (mock HttpClient, verify URLs, verify request/response mapping)

---

### Tarea 12 — [FE] Admin Dashboard (empty state + onboarding)

**Que hacer:**
- Replace admin dashboard placeholder with real component
- On init: call `getDashboardState()`
- Render different views based on state:
  - **Empty state** (no locations, no supervisors): Show welcome + setup wizard CTA
  - **Partial setup** (locations but no supervisors, or cycle in config): Show progress + resume wizard CTA
  - **No active cycle** (but has last closed cycle): Show "Start new cycle" CTA with last cycle info
  - **Active cycle**: Show dashboard metrics (placeholder for now)

**Wireframes:** See CYCLE_ARCHITECTURE.md sections 13.1, 13.2, 13.6

**Tests:**
- Component tests for each dashboard state variation

---

### Tarea 13 — [FE] Create Cycle form (smart defaults + clone option)

**Que hacer:**
- Create cycle creation page/dialog
- Smart defaults logic:
  - Name: month 1-6 → "{year}-1", month 7-12 → "{year}-2"
  - StartDate: first day of current month
  - EndDate: StartDate + 16 weeks
  - ApplicationDeadline: StartDate + 2 weeks
  - InterviewDate: ApplicationDeadline + 1 week
  - SelectionDate: InterviewDate + 1 week
- If `lastClosedCycle` exists: show "Clone setup from {name}?" toggle
  - If toggled: send `cloneFromCycleId` in request
  - Show summary: "5 locations, 3 supervisors will be imported"
- All fields editable (smart defaults are suggestions only)

**Tests:**
- Smart defaults calculation, form validation, clone toggle behavior

---

## Bloque 3: Wizard de Configuracion

> **Objetivo:** Wizard multi-step para configurar el ciclo.
> **Prerequisito:** Bloques 1-2 completos.

### Tarea 14 — [FE] Wizard container component (stepper)

**Que hacer:**
- Create wizard container with PrimeNG Stepper or custom stepper
- Steps: Ubicaciones → Supervisores → Horarios (3 steps for first cycle, 4 steps with Renewals for subsequent)
- Draft persistence in localStorage
- Resume: on init, check if cycle in Configuration exists, load its data, restore draft from localStorage
- Navigation: Back/Next/Save, validation before advancing

**Tests:**
- Stepper navigation, draft save/restore, step validation

---

### Tarea 15 — [FE] Wizard Step 1: Locations configuration

**Que hacer:**
- Show all locations for the department (from backend Location catalog)
- For each: toggle active/inactive, set scholarships available
- If cloned: show pre-filled from previous cycle
- Running total: "X/Y scholarships assigned to locations"
- Save step → calls ConfigureCycleCommand with locations array

**Tests:**
- Toggle location, update scholarships count, total calculation, save

---

### Tarea 16 — [FE] Wizard Step 2: Supervisor assignments

**Que hacer:**
- For each active CycleLocation: show dropdown to assign supervisor
- List available supervisors (users with role SUPERVISOR)
- Warning if location has no supervisor
- Save step → calls ConfigureCycleCommand with supervisorAssignments

**Tests:**
- Assign supervisor, change supervisor, unassigned warning

---

### Tarea 17 — [FE] Wizard Step 3: Schedule configuration

**Que hacer:**
- For each active CycleLocation: configure schedule slots
- Options: "Unified M-F" (same schedule every day) or "Custom" (per day)
- Each slot: startTime, endTime, requiredScholars
- If cloned: show pre-filled from previous cycle
- Save step → calls ConfigureCycleCommand with locations[].scheduleSlots

**Tests:**
- Add/remove slot, unified vs custom mode, validation (no overlapping times)

---

## Bloque 4: Transiciones de Estado

> **Objetivo:** Implementar las transiciones del ciclo.
> **Prerequisito:** Bloques 1-3 completos.

### Tarea 18 — [BE] OpenApplicationsCommand + CloseApplicationsCommand + ReopenApplicationsCommand

**Que hacer:**
- 3 commands, each calling the corresponding Cycle domain method
- OpenApplications validates:
  - At least 1 active CycleLocation (count from DB)
  - RenewalProcessCompleted (if not first cycle)
- CloseApplications: simple state check
- ReopenApplications: simple state check (escape valve)

**Request/Response:**
```
POST /api/cycles/{id}/open-applications    → 200 { success: true, data: CycleDto }
POST /api/cycles/{id}/close-applications   → 200 { success: true, data: CycleDto }
POST /api/cycles/{id}/reopen-applications  → 200 { success: true, data: CycleDto }
```

**Error codes:**
- INVALID_TRANSITION, NO_LOCATIONS, NO_SCHOLARSHIPS, SETUP_INCOMPLETE, RENEWALS_PENDING

**Tests:**
- Each transition: valid, invalid state, missing prerequisites

**Archivos a crear:**
```
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/OpenApplications/
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/CloseApplications/
apps/api/src/WorkScholarship.Application/Features/Cycles/Commands/ReopenApplications/
```

---

### Tarea 19 — [BE] ExtendCycleDatesCommand

**Que hacer:**
- Allows extending dates (not reducing) for non-closed cycles
- Validates date coherence

**Request:**
```
PUT /api/cycles/{id}/extend-dates
{
  "applicationDeadline": "2024-08-22",     // optional, only if extending
  "interviewDate": "2024-08-29",           // optional
  "selectionDate": "2024-09-05",           // optional
  "endDate": "2024-12-22"                  // optional
}
```

**Tests:**
- Extend each date, try to reduce (should fail), extend on closed cycle (should fail)

---

### Tarea 20 — [FE] Cycle detail page + state transition buttons

**Que hacer:**
- Cycle detail page showing all cycle data
- Status badge with color coding
- Action buttons based on current state:
  - Configuration → "Open Applications" button (disabled if prerequisites incomplete)
  - ApplicationsOpen → "Close Applications" button
  - ApplicationsClosed → "Reopen" + "Start Selection" buttons
  - Active → "Extend Dates" + "Close Cycle" buttons
- Confirmation dialogs for each transition
- Error handling for invalid transitions

**Tests:**
- Render correct buttons per state, confirmation flow, error display

---

### Tarea 21 — [FE] Extend dates dialog

**Que hacer:**
- Modal dialog to extend cycle dates
- Show current dates, allow modifying with calendar pickers
- Validation: new dates must be >= current dates
- Submit → ExtendCycleDatesCommand

**Tests:**
- Date validation, submit, error handling

---

## Bloque 5: Cierre e Historia

> **Objetivo:** Cerrar ciclos y consultar historicos.
> **Prerequisito:** Bloques 1-4 completos.

### Tarea 22 — [BE] CloseCycleCommand (pre-conditions + eligibility)

**Que hacer:**
- Validates pre-conditions before closing:
  - Status == Active
  - EndDate has passed (or admin override?)
  - No pending shifts (count from DB)
  - All logbooks generated (count from DB)
- On close:
  - Set Status = Closed, ClosedAt, ClosedBy
  - Calculate EligibleForRenewal for each ScholarAssignment (RN-002)
  - Emit CycleClosedEvent

**Note:** ScholarAssignment entity doesn't exist yet (SEL subsystem). For now, close without eligibility calculation — add it when SEL is implemented.

**Request:**
```
POST /api/cycles/{id}/close
Response: 200 { success: true, data: CycleDto }
```

**Error codes:**
- INVALID_TRANSITION, CYCLE_NOT_ENDED, PENDING_SHIFTS, MISSING_LOGBOOKS

**Tests:**
- Close with all preconditions met, each precondition failure

---

### Tarea 23 — [BE] Time Machine queries (historical data)

**Que hacer:**
- `GetCycleLocationsQuery(Guid CycleId)` → CycleLocations with Location details
- `GetCycleSupervisorsQuery(Guid CycleId)` → SupervisorAssignments with User details
- `GetCycleStatisticsQuery(Guid CycleId)` → Aggregated metrics

**Note:** GetCycleScholarsQuery and GetCycleDocumentsQuery will be added when those subsystems exist.

**Endpoints:**
```
GET /api/cycles/{id}/locations
GET /api/cycles/{id}/supervisors
GET /api/cycles/{id}/statistics
```

**Tests:**
- Each query with data, each query with empty cycle

---

### Tarea 24 — [FE] Cycle history list + historical view

**Que hacer:**
- Cycles list page (admin/cycles) with filters: department, year, status
- Pagination
- Click on cycle → detail page
- For closed cycles: "read-only" badge, no action buttons
- Time Machine data tabs: Locations, Supervisors, (Scholars placeholder), (Documents placeholder)

**Tests:**
- List rendering, filters, pagination, read-only mode for closed cycles

---

## Bloque 6: Renovaciones + Notificaciones (Post-MVP)

> **Objetivo:** Proceso de renovacion y notificaciones proactivas.
> **Prerequisito:** Bloques 1-5 completos + SEL subsystem partially implemented.

### Tarea 25 — [BOTH] Renewal process (candidates, invitations, submissions, confirmation)

**Que hacer (BE):**
- `GetRenewalCandidatesQuery` → ScholarAssignments from previous cycle with EligibleForRenewal = true
- `SendRenewalInvitationsCommand` → Send emails via IEmailService
- `SubmitRenewalCommand` → Scholar uploads new schedule
- `ConfirmRenewalsCommand` → Admin confirms, creates ScholarAssignments
- `SkipRenewalsCommand` → Admin skips renewals

**Que hacer (FE):**
- Wizard Step 4 (for subsequent cycles): Renewals management
- Show eligible candidates list
- "Send Invitations" button
- Track responses (pending, accepted, declined)
- "Confirm Renewals" / "Skip Renewals" buttons

**Endpoints:**
```
GET    /api/cycles/{id}/renewal-candidates
POST   /api/cycles/{id}/send-renewal-invitations
POST   /api/cycles/{id}/renewals
GET    /api/cycles/{id}/renewal-results
POST   /api/cycles/{id}/confirm-renewals
POST   /api/cycles/{id}/skip-renewals
```

**Note:** This task depends on ScholarAssignment entity from SEL subsystem.

---

### Tarea 26 — [BE] Proactive notifications (Hangfire jobs)

**Que hacer:**
- Configure Hangfire recurring jobs:
  - `CycleDeadlineCheckerJob` (daily 08:00 UTC)
  - `StaleConfigurationCheckerJob` (weekly)
  - `NoCycleReminderJob` (biweekly)
- `CycleNotificationLog` entity (anti-duplicate)
- `CheckCycleDeadlinesCommand` handler
- Email templates for each notification type

**See:** CYCLE_ARCHITECTURE.md section 14 for full specification.

**Note:** This is the lowest priority — implement after core cycle management is solid.

---

## Summary: Task Dependencies

```
Bloque 1: [T1] → [T2] → [T3] → [T4] → [T5]
                                          │
Bloque 2: [T6] → [T7] → [T8] → [T9] → [T10]
           │                              │
           ├── [T11] (FE, can start after T6 contract defined)
           ├── [T12] (FE, can start after T10 contract defined)
           └── [T13] (FE, can start after T6 contract defined)
                                          │
Bloque 3: [T14] → [T15] → [T16] → [T17]
                                    │
Bloque 4: [T18] → [T19] (BE)       │
           │                        │
           └── [T20] → [T21] (FE)  │
                                    │
Bloque 5: [T22] → [T23] (BE)       │
           │                        │
           └── [T24] (FE)          │
                                    │
Bloque 6: [T25] → [T26] (Post-MVP)
```

**Parallelism opportunities:**
- FE tasks T11, T12, T13 can start once BE defines the API contract (after T6)
- FE can use mocks while BE implements handlers
- T18/T19 (BE state transitions) and T14-T17 (FE wizard) can run in parallel
- T22/T23 (BE close/history) and T24 (FE history view) can overlap

---

## API Error Codes Reference

| Code | Message | When |
|------|---------|------|
| INVALID_TRANSITION | "Solo se puede X desde estado Y" | Wrong state for transition |
| NO_LOCATIONS | "Debe haber al menos una ubicacion activa" | OpenApplications without locations |
| NO_SCHOLARSHIPS | "El total de becas debe ser mayor a 0" | OpenApplications without scholarships |
| SETUP_INCOMPLETE | "Complete la configuracion" | OpenApplications without setup |
| RENEWALS_PENDING | "Procese las renovaciones primero" | OpenApplications without renewals (2nd+ cycle) |
| CYCLE_NOT_ENDED | "No se puede cerrar antes de la fecha fin" | Close before EndDate |
| PENDING_SHIFTS | "Hay X jornadas pendientes" | Close with unapproved shifts |
| MISSING_LOGBOOKS | "Faltan bitacoras para X becarios" | Close without logbooks |
| DUPLICATE_CYCLE | "Ya existe un ciclo activo para esta dependencia" | Create when active cycle exists |
| CYCLE_NOT_FOUND | "Ciclo no encontrado" | Invalid cycle ID |
| CYCLE_CLOSED | "No se pueden modificar datos de un ciclo cerrado" | Any write on closed cycle |

---

**Last Updated:** 2026-02-26
**Next Review:** After Block 1 completion
