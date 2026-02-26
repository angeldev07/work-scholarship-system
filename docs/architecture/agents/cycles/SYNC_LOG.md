# Cycle Management Module — Sync Log
## Communication Log Between Backend and Frontend Agents

**Purpose:** Track changes that affect the contract between backend (.NET) and frontend (Angular) for the Cycle Management module (RF-006 to RF-012).

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

### [2026-02-26] [coordinator] [INFO] Architecture document completed — ready for implementation

Comprehensive architecture document created at `docs/architecture/cycles/CYCLE_ARCHITECTURE.md` (v2.0, 16 sections).

**Key decisions documented:**
- 5-state machine: Configuration -> ApplicationsOpen -> ApplicationsClosed -> Active -> Closed
- CycleLocation junction table (not direct Location FK on Cycle)
- SupervisorAssignment with CycleId scope
- `SetupCompleted` and `IsFirstCycle` are NOT persisted — calculated dynamically via health check
- `RenewalProcessCompleted` IS persisted (one-time event flag)
- Clone from previous cycle supported via `cloneFromCycleId`
- Smart defaults calculated by frontend (name, dates)
- Wizard with hybrid persistence (backend for completed steps, localStorage for drafts)

**API contract defined (section 10):**

Core endpoints:
```
POST   /api/cycles                              → CreateCycleCommand (RF-006)
GET    /api/cycles                              → ListCyclesQuery (RF-012)
GET    /api/cycles/active?department={dept}      → GetActiveCycleQuery
GET    /api/cycles/{id}                         → GetCycleDetailQuery (RF-012)
PUT    /api/cycles/{id}/configure               → ConfigureCycleCommand (RF-007)
```

State transition endpoints:
```
POST   /api/cycles/{id}/open-applications       → OpenApplicationsCommand (RF-008)
POST   /api/cycles/{id}/close-applications      → CloseApplicationsCommand (RF-009)
POST   /api/cycles/{id}/reopen-applications     → ReopenApplicationsCommand (RF-009)
POST   /api/cycles/{id}/activate                → ActivateCycleCommand (RF-020)
PUT    /api/cycles/{id}/extend-dates            → ExtendCycleDatesCommand (RF-010)
POST   /api/cycles/{id}/close                   → CloseCycleCommand (RF-011)
```

Time Machine queries:
```
GET    /api/cycles/{id}/locations               → GetCycleLocationsQuery
GET    /api/cycles/{id}/scholars                → GetCycleScholarsQuery
GET    /api/cycles/{id}/supervisors             → GetCycleSupervisorsQuery
GET    /api/cycles/{id}/statistics              → GetCycleStatisticsQuery
GET    /api/cycles/{id}/documents               → GetCycleDocumentsQuery
```

Dashboard state:
```
GET    /api/admin/dashboard-state?department={dept} → GetAdminDashboardStateQuery
```

Renewal endpoints:
```
GET    /api/cycles/{id}/renewal-candidates
POST   /api/cycles/{id}/send-renewal-invitations
POST   /api/cycles/{id}/renewals
GET    /api/cycles/{id}/renewal-results
POST   /api/cycles/{id}/confirm-renewals
POST   /api/cycles/{id}/skip-renewals
```

**Implementation guide:** See `IMPLEMENTATION_GUIDE.md` for 26 tasks in 6 blocks.

**Action for both agents:** Read `CYCLE_ARCHITECTURE.md` fully before starting implementation. Start with Block 1 tasks from `IMPLEMENTATION_GUIDE.md`.

---

## Notes for Frontend Agent (angular-ux-engineer)

1. **Backoffice shell already has placeholders** for cycle routes (admin/cycles/*)
2. **Wizard wireframes** are in CYCLE_ARCHITECTURE.md section 13.5
3. **Smart defaults logic** (section 13.3) is frontend responsibility:
   - Name deduction from month (1-6 → "{year}-1", 7-12 → "{year}-2")
   - Date calculations (StartDate + offsets for each date)
4. **Dashboard state** drives the onboarding flow (section 13.1, 13.2)
5. **Draft persistence** uses localStorage for wizard step in-progress
6. **Cycle selector** in topbar for historical navigation (read-only mode)
7. All cycle endpoints require ADMIN role

## Notes for Backend Agent (dotnet-backend-engineer)

1. **Follow existing patterns**: BaseEntity, User entity (rich domain), AuthController
2. **Location entity** must be created first or in same migration as CycleLocation
3. **CycleStatus enum** follows UserRole enum pattern
4. **Health check approach**: `GetAdminDashboardStateQuery` counts CycleLocations/SupervisorAssignments to determine setup progress
5. **Clone logic** lives in `CreateCycleCommandHandler`, not in domain entity
6. **RenewalProcessCompleted** is auto-true for first cycle of a department
7. **Domain events**: CycleCreatedEvent, ApplicationsOpenedEvent, etc. (for future notification integration)
8. All endpoints ADMIN-only authorization

---

## Questions / Pending Clarifications

(Empty — add questions here as they arise during implementation)

---

## Change History

| Date | Agent | Change | Impact |
|------|-------|--------|--------|
| 2026-02-26 | coordinator | Architecture doc v2.0 + implementation guide created | Blueprint ready for implementation |

---

**Last Updated:** 2026-02-26 (Module setup)
**Next Review:** When first backend endpoint is implemented
