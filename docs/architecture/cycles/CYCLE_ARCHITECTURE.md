# Arquitectura del Subsistema de Ciclos (RF-006 a RF-012)

## Sistema de Gestion y Seguimiento de Becas Trabajo

**Version:** 2.0
**Fecha:** 2026-02-26
**Estado:** Documento de Arquitectura (pre-implementacion)
**Autor:** Coordinador de Arquitectura

---

## Indice

1. [Introduccion y Concepto Central](#1-introduccion-y-concepto-central)
2. [Entidad Cycle — Diseno de Dominio](#2-entidad-cycle--diseno-de-dominio)
3. [Maquina de Estados](#3-maquina-de-estados)
4. [Concepto "Maquina del Tiempo" — Arquitectura Temporal](#4-concepto-maquina-del-tiempo--arquitectura-temporal)
5. [Diagrama de Relaciones de Entidades](#5-diagrama-de-relaciones-de-entidades)
6. [Analisis de Impacto Cross-Subsistema](#6-analisis-de-impacto-cross-subsistema)
7. [Reglas de Inmutabilidad al Cerrar Ciclo](#7-reglas-de-inmutabilidad-al-cerrar-ciclo)
8. [Sketch de Entidad de Dominio (C#)](#8-sketch-de-entidad-de-dominio-c)
9. [Commands y Queries CQRS](#9-commands-y-queries-cqrs)
10. [Endpoints API REST](#10-endpoints-api-rest)
11. [Aprendizajes del Predecesor Django](#11-aprendizajes-del-predecesor-django)
12. [Reglas de Negocio Consolidadas](#12-reglas-de-negocio-consolidadas)
13. [Flujos UX del Admin — Edge Cases y Escenarios Reales](#13-flujos-ux-del-admin--edge-cases-y-escenarios-reales)
14. [Sistema de Notificaciones Proactivas del Ciclo](#14-sistema-de-notificaciones-proactivas-del-ciclo)
15. [Proceso de Renovacion dentro del Ciclo](#15-proceso-de-renovacion-dentro-del-ciclo)
16. [Consideraciones de Implementacion](#16-consideraciones-de-implementacion)

---

## 1. Introduccion y Concepto Central

### 1.1 Que es un Ciclo

Un **Ciclo** (o Semestre) es la unidad temporal fundamental del sistema. Representa un periodo academico (~16 semanas) durante el cual opera el programa de becas trabajo. Todo lo que sucede en el sistema — postulaciones, selecciones, jornadas laborales, ausencias, documentos — ocurre **dentro del contexto de un ciclo**.

### 1.2 El Ciclo como Frontera Temporal

El Ciclo actua como una **frontera temporal universal**. Esto significa que:

- Cada entidad operativa del sistema tiene una relacion directa o derivada con un Ciclo
- Los unicos datos que existen **fuera** del contexto de un ciclo son: `User` y `RefreshToken` (identidad pura)
- Cuando un ciclo se cierra, toda su informacion se congela como una **instantanea historica inmutable**

### 1.3 El Concepto "Maquina del Tiempo"

La idea central es que al seleccionar un ciclo cerrado, el sistema debe poder **reconstruir el estado completo** de ese periodo:

- Que ubicaciones estaban activas y con que horarios
- Que supervisores estaban asignados y a donde
- Que becarios participaron y donde fueron asignados
- Todas las jornadas trabajadas, aprobadas o rechazadas
- Todas las ausencias reportadas y su resolucion
- Todos los documentos generados (bitacoras, escarapelas)
- Las metricas finales del ciclo

Esto **no es una funcionalidad de auditoría** (para eso existe RF-052/RF-054). Es una **vista operativa historica**: el admin puede navegar a "2024-1" y ver todo como si estuviera en ese periodo, con la diferencia de que los datos son de solo lectura.

### 1.4 Mapeo de Requerimientos

| RF | Nombre | Prioridad | Rol |
|----|--------|-----------|-----|
| RF-006 | Crear Nuevo Ciclo Semestral | Alta | ADMIN |
| RF-007 | Configurar Ciclo | Alta | ADMIN |
| RF-008 | Abrir Periodo de Postulaciones | Alta | ADMIN |
| RF-009 | Cerrar Periodo de Postulaciones | Alta | ADMIN |
| RF-010 | Extender Fechas del Ciclo | Media | ADMIN |
| RF-011 | Cerrar Ciclo Semestral | Alta | ADMIN |
| RF-012 | Ver Historial de Ciclos | Media | ADMIN |

---

## 2. Entidad Cycle — Diseno de Dominio

### 2.1 Propiedades

```
Cycle : BaseEntity
├── Name                          string       "2024-1", "Enero-Mayo 2024"
├── Department                    string       "Biblioteca", "Centro de Computo"
├── Status                        CycleStatus  enum (5 estados)
│
├── StartDate                     DateTime     Fecha inicio del ciclo academico
├── EndDate                       DateTime     Fecha fin del ciclo academico
├── ApplicationDeadline           DateTime     Fecha limite para postulaciones
├── InterviewDate                 DateTime     Fecha programada para entrevistas
├── SelectionDate                 DateTime     Fecha de seleccion final
│
├── TotalScholarshipsAvailable    int          Plazas totales de becas
├── TotalScholarshipsAssigned     int          Plazas asignadas (calculado/mantenido)
│
├── ClosedAt                      DateTime?    Fecha real de cierre (null si no cerrado)
├── ClosedBy                      string?      Quien cerro el ciclo
│
├── RenewalProcessCompleted       bool         Renovaciones procesadas (auto-true si primer ciclo)
├── ClonedFromCycleId             Guid?        Ciclo del cual se clono el setup (null si manual)
│
│   NOTA: SetupCompleted e IsFirstCycle NO se persisten — se calculan
│   dinamicamente desde el estado actual de los datos (health check).
│   Ver seccion 13.3 para detalles.
│
├── [Navigation Properties]
├── CycleLocations                ICollection<CycleLocation>
├── Applications                  ICollection<Application>
├── ScholarAssignments            ICollection<ScholarAssignment>
├── SupervisorAssignments         ICollection<SupervisorAssignment>
└── Documents                     ICollection<Document>
```

### 2.2 Enum CycleStatus

```
CycleStatus
├── Configuration = 0       Estado inicial al crear. Permite setup de ubicaciones.
├── ApplicationsOpen = 1    Postulantes pueden registrarse.
├── ApplicationsClosed = 2  Fase de revision, entrevistas y seleccion.
├── Active = 3              Ciclo en operacion. Becas trabajan, se registran jornadas.
└── Closed = 4              Ciclo finalizado. Datos congelados, snapshot historico.
```

### 2.3 Entidades Auxiliares Introducidas por el Ciclo

#### CycleLocation (Junction Table — Ciclo + Ubicacion)

Las ubicaciones son entidades **maestras** que existen independientemente de los ciclos. Pero su participacion en un ciclo es temporal — una ubicacion puede estar activa en un ciclo y no en otro, o tener diferente cantidad de becas asignables por ciclo.

```
CycleLocation : BaseEntity
├── CycleId                       Guid (FK → Cycle)
├── LocationId                    Guid (FK → Location)
├── ScholarshipsAvailable         int          Becas asignables en esta ubicacion para este ciclo
├── ScholarshipsAssigned          int          Becas actualmente asignados
├── IsActive                      bool         Activa para este ciclo
│
├── [Navigation Properties]
├── Cycle                         Cycle
├── Location                      Location
├── ScheduleSlots                 ICollection<ScheduleSlot>     Horarios para este ciclo
└── SupervisorAssignments         ICollection<SupervisorAssignment>
```

**Razon de esta junction table:** En el predecesor Django, las ubicaciones NO tenian scope de ciclo. Esto impedia saber retrospectivamente que ubicaciones estaban activas en un ciclo pasado, ni cuantos becas tenian asignados. La junction table `CycleLocation` resuelve esto: cada ciclo tiene su propia "foto" de que ubicaciones participaron y con que configuracion.

#### SupervisorAssignment (Asignacion temporal de supervisor)

```
SupervisorAssignment : BaseEntity
├── CycleId                       Guid (FK → Cycle)
├── CycleLocationId               Guid (FK → CycleLocation)
├── SupervisorId                  Guid (FK → User)
├── AssignedAt                    DateTime
│
├── [Navigation Properties]
├── Cycle                         Cycle
├── CycleLocation                 CycleLocation
└── Supervisor                    User
```

#### ScholarAssignment (BecaTrabajo — Asignacion de becario a ciclo)

```
ScholarAssignment : BaseEntity
├── CycleId                       Guid (FK → Cycle)
├── UserId                        Guid (FK → User)
├── CycleLocationId               Guid (FK → CycleLocation)
├── ApplicationId                 Guid? (FK → Application, null si renovacion directa)
├── IsRenewal                     bool
├── Status                        ScholarStatus (Active, Suspended, Finished)
│
├── StartDate                     DateTime
├── EndDate                       DateTime?
├── TotalHoursWorked              decimal      Actualizado al aprobar jornadas
├── TotalAbsences                 int          Actualizado al procesar ausencias
├── EligibleForRenewal            bool?        Calculado al cerrar ciclo
│
├── [Navigation Properties]
├── Cycle                         Cycle
├── User                          User
├── CycleLocation                 CycleLocation
├── Application                   Application?
├── WorkShifts                    ICollection<WorkShift>      Jornadas trabajadas
├── Absences                      ICollection<Absence>        Ausencias reportadas
├── Schedule                      ICollection<ScholarSchedule> Horario asignado
└── Documents                     ICollection<Document>       Bitacoras, escarapelas
```

---

## 3. Maquina de Estados

### 3.1 Diagrama de Transiciones

```
                    ┌──────────────────┐
                    │  Configuration   │ ← Estado inicial (RF-006)
                    │                  │
                    │  Setup de        │
                    │  ubicaciones,    │
                    │  supervisores,   │
                    │  horarios        │
                    └────────┬─────────┘
                             │ OpenApplications() (RF-008)
                             │
                             │ Pre-condiciones:
                             │  - ≥1 CycleLocation activa
                             │  - Fechas definidas (Start < End)
                             │  - TotalScholarships > 0
                             │  - ApplicationDeadline > ahora
                             ▼
                    ┌──────────────────┐
                    │ ApplicationsOpen │
                    │                  │
             ┌──────│  Postulantes     │──────┐
             │      │  se registran    │      │
             │      └────────┬─────────┘      │
             │               │                │
             │               │ CloseApplications() (RF-009)
             │               ▼                │
             │      ┌──────────────────┐      │
             │      │ApplicationsClosed│      │
             │      │                  │      │
             │      │  Entrevistas,    │      │ ReopenApplications()
             │      │  evaluacion,     │      │ (RF-009: "se puede
             │      │  seleccion       │◄─────┘  reabrir manualmente")
             │      └────────┬─────────┘
             │               │ Activate() (RF-020 confirma seleccion)
             │               │
             │               │ Pre-condiciones:
             │               │  - Seleccion final confirmada
             │               │  - Becas asignados a ubicaciones
             │               │  - Rol cambiado a BECA
             │               ▼
             │      ┌──────────────────┐
             │      │     Active       │
             │      │                  │
             │      │  Operacion       │
             │      │  diaria:         │◄──── ExtendDates() (RF-010)
             │      │  check-in/out,   │      Puede extender fechas
             │      │  ausencias,      │      durante este estado
             │      │  aprobaciones    │
             │      └────────┬─────────┘
             │               │ Close() (RF-011)
             │               │
             │               │ Pre-condiciones:
             │               │  - Fecha actual ≥ EndDate
             │               │  - 0 jornadas pendientes de aprobar
             │               │  - Bitacoras generadas
             │               ▼
             │      ┌──────────────────┐
             │      │     Closed       │
             │      │                  │
             │      │  INMUTABLE       │
             │      │  Snapshot        │
             │      │  historico       │
             │      │                  │
             │      │  Elegibilidad    │
             │      │  de renovacion   │
             │      │  calculada       │
             │      └──────────────────┘
             │
             └──── ExtendDates() (RF-010)
                   Tambien aplicable en ApplicationsOpen
```

### 3.2 Tabla de Transiciones

| Estado Origen | Accion | Estado Destino | Pre-condiciones | Eventos Emitidos |
|--------------|--------|---------------|-----------------|-----------------|
| `Configuration` | `OpenApplications()` | `ApplicationsOpen` | ≥1 CycleLocation activa, fechas validas, TotalScholarships > 0 | `ApplicationsOpenedEvent` |
| `ApplicationsOpen` | `CloseApplications()` | `ApplicationsClosed` | Ninguna especial (admin decide) | `ApplicationsClosedEvent` |
| `ApplicationsClosed` | `ReopenApplications()` | `ApplicationsOpen` | Ninguna (escape valve) | `ApplicationsReopenedEvent` |
| `ApplicationsClosed` | `Activate()` | `Active` | Seleccion confirmada, becas asignados | `CycleActivatedEvent` |
| `Active` | `Close()` | `Closed` | EndDate alcanzada, 0 jornadas pendientes, bitacoras generadas | `CycleClosedEvent` |
| `Configuration` | `ExtendDates()` | `Configuration` | Nuevas fechas > fechas actuales | `CycleDatesExtendedEvent` |
| `ApplicationsOpen` | `ExtendDates()` | `ApplicationsOpen` | Nuevas fechas > fechas actuales, no reducir fechas pasadas | `CycleDatesExtendedEvent` |
| `Active` | `ExtendDates()` | `Active` | Solo puede extender EndDate | `CycleDatesExtendedEvent` |

### 3.3 Transiciones Invalidas (Guard Clauses)

Cualquier transicion no listada en la tabla anterior es **invalida** y debe retornar `Result.Error()`:

- `Configuration` → `Active` (no puede saltar estados)
- `ApplicationsOpen` → `Active` (debe cerrar postulaciones primero)
- `Active` → `ApplicationsOpen` (no se puede retroceder)
- `Closed` → cualquier estado (inmutable, sin retorno)
- Cualquier estado → `Configuration` (no se puede volver al inicio)

---

## 4. Concepto "Maquina del Tiempo" — Arquitectura Temporal

### 4.1 Principio Fundamental

> **CycleId es el parametro universal que define "cuando" sucedio algo.**

Consultar cualquier dato operativo requiere conocer el ciclo. El sistema siempre opera en el contexto de un ciclo — ya sea el activo (operacion diaria) o uno cerrado (consulta historica).

### 4.2 Capas de Datos Temporales

```
┌─────────────────────────────────────────────────────────────┐
│                    DATOS ATEMPORALES                         │
│                 (existen fuera de ciclos)                    │
│                                                             │
│  User            RefreshToken         Location (catalogo)   │
│  (identidad)     (sesion)             (datos maestros)      │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                  DATOS TEMPORALES — NIVEL 1                 │
│              (FK directa a Cycle)                           │
│                                                             │
│  CycleLocation          SupervisorAssignment                │
│  (ubicacion activa      (quien supervisa donde              │
│   en este ciclo)         en este ciclo)                     │
│                                                             │
│  Application            ScholarAssignment                   │
│  (postulacion a         (becario asignado a                 │
│   este ciclo)            este ciclo)                        │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                  DATOS TEMPORALES — NIVEL 2                 │
│              (FK derivada via ScholarAssignment)             │
│                                                             │
│  WorkShift              Absence                             │
│  (jornada laboral       (ausencia de un                     │
│   de un becario)         becario)                           │
│                                                             │
│  HourAdvanceRequest     HourCompensation                    │
│  (solicitud adelanto    (ajuste manual                      │
│   de horas)              de horas)                          │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                  DATOS TEMPORALES — NIVEL 3                 │
│              (generados a partir de niveles 1-2)            │
│                                                             │
│  Document (Bitacora, Escarapela)                            │
│  Notification (eventos del ciclo)                           │
│  CycleReport (metricas finales)                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 Como Funciona la Consulta Historica

Cuando el admin selecciona un ciclo cerrado (ej: "2024-1"), el sistema ejecuta queries filtrados por `CycleId`:

```
GET /api/cycles/{cycleId}                    → Datos del ciclo
GET /api/cycles/{cycleId}/locations          → CycleLocations (ubicaciones activas en ese ciclo)
GET /api/cycles/{cycleId}/supervisors        → SupervisorAssignments
GET /api/cycles/{cycleId}/scholars           → ScholarAssignments
GET /api/cycles/{cycleId}/scholars/{id}/shifts    → WorkShifts del becario en ese ciclo
GET /api/cycles/{cycleId}/scholars/{id}/absences  → Ausencias del becario en ese ciclo
GET /api/cycles/{cycleId}/statistics         → Metricas consolidadas
GET /api/cycles/{cycleId}/documents          → Bitacoras y escarapelas generadas
```

El frontend implementa un **selector de ciclo** en la UI. Cuando se selecciona un ciclo distinto al activo, toda la interfaz cambia a **modo lectura** y los datos se recargan con el nuevo `CycleId`.

### 4.4 Ciclo Activo vs Ciclo Historico

| Aspecto | Ciclo Activo | Ciclo Historico (Closed) |
|---------|-------------|------------------------|
| Edicion de datos | Permitida | **Bloqueada** |
| Check-in/out | Habilitado | No disponible |
| Aprobacion de jornadas | Habilitado | No disponible |
| Reportes de ausencia | Habilitado | No disponible |
| Generacion de documentos | Permitida | Solo consulta de existentes |
| Dashboards | Datos en tiempo real | Snapshot congelado |
| Selector UI | Resaltado como "actual" | Etiqueta "historico" + modo lectura |

---

## 5. Diagrama de Relaciones de Entidades

### 5.1 Diagrama General

```
                              ┌──────────────┐
                              │     User     │
                              │  (atemporal) │
                              └──────┬───────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
                    ▼                ▼                ▼
          ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
          │ Supervisor   │  │   Scholar    │  │ Application  │
          │ Assignment   │  │ Assignment   │  │ (Postulacion)│
          │              │  │ (BecaTrabajo)│  │              │
          │ CycleId (FK) │  │ CycleId (FK) │  │ CycleId (FK) │
          └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
                 │                 │                  │
                 │                 │                  │
                 ▼                 │                  │
          ┌──────────────┐        │                  │
          │    Cycle     │◄───────┴──────────────────┘
          │              │
          │  Name        │         ┌──────────────┐
          │  Department  │────────►│ CycleLocation│
          │  Status      │         │              │
          │  StartDate   │         │ CycleId (FK) │
          │  EndDate     │         │ LocationId   │
          │  ...         │         │ Scholarships │
          └──────────────┘         └──────┬───────┘
                                          │
                                          ▼
                                   ┌──────────────┐
                                   │   Location   │
                                   │  (atemporal) │
                                   │              │
                                   │  Name        │
                                   │  Description │
                                   │  Image       │
                                   └──────────────┘


          ScholarAssignment (Nivel 1)
                 │
                 ├──────────────────┐
                 │                  │
                 ▼                  ▼
          ┌──────────────┐  ┌──────────────┐
          │  WorkShift   │  │   Absence    │
          │  (Jornada)   │  │  (Ausencia)  │
          │              │  │              │
          │ ScholarAsg   │  │ ScholarAsg   │
          │   Id (FK)    │  │   Id (FK)    │
          │ CheckIn/Out  │  │ Date/Motivo  │
          │ Status       │  │ Status       │
          │ Photos       │  │ Document     │
          └──────────────┘  └──────────────┘
                                    │
          ┌──────────────┐          │
          │  Document    │◄─────────┘ (documentos de soporte)
          │ (Bitacora,   │
          │  Escarapela) │
          │              │
          │ CycleId (FK) │
          │ ScholarAsg   │
          │   Id (FK)?   │
          └──────────────┘
```

### 5.2 Cadena de Derivacion de CycleId

```
Cycle
 ├── CycleLocation ─────────────────── FK directa (CycleId)
 │    └── ScheduleSlot ──────────────── FK derivada (via CycleLocationId)
 │
 ├── SupervisorAssignment ──────────── FK directa (CycleId)
 │
 ├── Application ───────────────────── FK directa (CycleId)
 │    └── LocationCompatibility ────── FK derivada (via ApplicationId)
 │
 ├── ScholarAssignment ─────────────── FK directa (CycleId)
 │    ├── WorkShift ────────────────── FK derivada (via ScholarAssignmentId)
 │    ├── Absence ──────────────────── FK derivada (via ScholarAssignmentId)
 │    ├── HourAdvanceRequest ───────── FK derivada (via ScholarAssignmentId)
 │    ├── HourCompensation ─────────── FK derivada (via ScholarAssignmentId)
 │    └── ScholarSchedule ─────────── FK derivada (via ScholarAssignmentId)
 │
 ├── Document ──────────────────────── FK directa (CycleId)
 │
 └── Notification ──────────────────── Contextual (evento del ciclo)
```

---

## 6. Analisis de Impacto Cross-Subsistema

### 6.1 AUTH (RF-001 a RF-005) — Sin impacto directo

Los usuarios (`User`) son **atemporales**. No tienen FK a Cycle. Un usuario puede participar en multiples ciclos a lo largo del tiempo.

**Conexion indirecta:** Cuando se confirma la seleccion (RF-020), el rol del usuario cambia de POSTULANTE a BECA. Este cambio se registra en el audit log con contexto del ciclo, pero la entidad User no contiene CycleId.

### 6.2 CICLO (RF-006 a RF-012) — Core

Este es el subsistema que define la entidad Cycle y su maquina de estados. Todos los demas subsistemas dependen de el.

| RF | Impacto |
|----|---------|
| RF-006 | Crea la entidad Cycle, estado `Configuration` |
| RF-007 | Crea CycleLocations, SupervisorAssignments, ScheduleSlots |
| RF-008 | Transicion a `ApplicationsOpen`, habilita registro de postulantes |
| RF-009 | Transicion a `ApplicationsClosed`, bloquea nuevos registros |
| RF-010 | Modifica fechas sin cambiar estado |
| RF-011 | Transicion a `Closed`, congela datos, calcula elegibilidad |
| RF-012 | Query de ciclos con metricas, acceso a snapshots historicos |

### 6.3 SEL — Proceso de Seleccion (RF-013 a RF-022)

**Entidades afectadas:**
- `Application` — FK directa: `CycleId`. Cada postulacion pertenece a un ciclo.
- `ScholarAssignment` — FK directa: `CycleId`. La asignacion final de un becario es por ciclo.
- `LocationCompatibility` — FK derivada via `ApplicationId`.

**Flujo temporal:**
1. `Application` solo se crea si el ciclo esta en `ApplicationsOpen`
2. La seleccion final (RF-020) transiciona el ciclo a `Active`
3. Renovacion (RF-021): el sistema busca `ScholarAssignment` del ciclo anterior donde `EligibleForRenewal = true`

**Impacto de la maquina del tiempo:**
Al ver un ciclo historico, se puede reconstruir todo el proceso de seleccion: cuantos postularon, quienes fueron entrevistados, que compatibilidad tenian, quienes fueron seleccionados y a donde.

### 6.4 UBIC — Gestion de Ubicaciones (RF-023 a RF-028)

**Diseno dual:**
- `Location` — Entidad **maestra** (atemporal). Catalogo de ubicaciones fisicas.
- `CycleLocation` — Entidad **temporal**. Vincula una ubicacion a un ciclo con configuracion especifica.

**Por que este diseno?**

Una ubicacion como "Sala de Lectura" existe permanentemente. Pero en cada ciclo puede:
- Estar activa o inactiva
- Tener diferente cantidad de becas asignables
- Tener horarios diferentes
- Tener supervisores diferentes

La junction table `CycleLocation` captura esta variabilidad temporal sin duplicar los datos maestros de la ubicacion.

**Entidades afectadas:**
- `Location` — Sin CycleId (catalogo permanente)
- `CycleLocation` — FK directa: `CycleId`
- `ScheduleSlot` — FK: `CycleLocationId` (horarios por ciclo)
- `SupervisorAssignment` — FK directa: `CycleId` + FK: `CycleLocationId`

### 6.5 TRACK — Tracking de Horas (RF-029 a RF-034)

**Entidades afectadas:**
- `WorkShift` (Jornada) — FK: `ScholarAssignmentId` → CycleId derivado

**Dependencia del estado del ciclo:**
- Check-in/out solo funciona si el ciclo esta en `Active`
- Al cerrar el ciclo, no puede haber jornadas en estado `PendingApproval`
- Jornadas aprobadas alimentan `ScholarAssignment.TotalHoursWorked`

**Impacto de la maquina del tiempo:**
Se puede ver el historial completo de jornadas de cualquier becario en cualquier ciclo pasado, incluyendo fotos de evidencia, horas trabajadas y estado de aprobacion.

### 6.6 AUS — Gestion de Ausencias (RF-035 a RF-039)

**Entidades afectadas:**
- `Absence` — FK: `ScholarAssignmentId` → CycleId derivado
- `HourAdvanceRequest` — FK: `ScholarAssignmentId` → CycleId derivado
- `HourCompensation` — FK: `ScholarAssignmentId` → CycleId derivado

**Dependencia del estado del ciclo:**
- Ausencias solo se reportan con ciclo `Active`
- Ausencias alimentan `ScholarAssignment.TotalAbsences`
- El conteo de ausencias afecta la elegibilidad de renovacion (RN-007)

### 6.7 DOC — Generacion de Documentos (RF-040 a RF-042)

**Entidades afectadas:**
- `Document` — FK directa: `CycleId`, FK opcional: `ScholarAssignmentId`

**Tipos de documento:**
- Escarapela (carnet) — generada al confirmar seleccion, vinculada a ScholarAssignment
- Bitacora oficial — generada al cerrar ciclo, contiene todas las jornadas aprobadas
- Reportes exportados — CSV/Excel/PDF con datos del ciclo

**Dependencia del estado del ciclo:**
- Escarapelas: generables cuando ciclo esta `Active`
- Bitacoras finales: **requisito** para cerrar ciclo (RF-011)
- RF-012 permite descargar documentos de ciclos cerrados

### 6.8 NOTIF — Notificaciones (RF-043 a RF-045)

**No tiene FK directa a Cycle**, pero los eventos del ciclo disparan notificaciones:

| Transicion | Notificacion |
|-----------|-------------|
| `OpenApplications()` | Email/in-app a potenciales postulantes |
| `CloseApplications()` | Notificacion a postulantes incompletos |
| `Activate()` | Email de felicitacion a seleccionados, rechazo a no seleccionados |
| `ExtendDates()` | Notificacion a usuarios afectados |
| `Close()` → proximo | Recordatorio 1 semana antes de fin de ciclo |

**Implementacion:** Domain Events emitidos por la entidad Cycle en cada transicion. Un `INotificationHandler<T>` procesa el evento y genera las notificaciones correspondientes.

### 6.9 REP — Reportes y Consultas (RF-046 a RF-051)

**Todos los dashboards dependen del ciclo activo/seleccionado:**

- RF-046 (Dashboard Admin): Metricas del ciclo actual — becas activos, horas, ausencias
- RF-047 (Dashboard Supervisor): Jornadas pendientes de aprobar **del ciclo actual**
- RF-048 (Dashboard Beca): Horas acumuladas **en el ciclo actual**
- RF-049 (Consultar Postulantes): Filtra por CycleId
- RF-050 (Consultar Becas): Filtra por CycleId
- RF-051 (Historial de Ciclos): Es basicamente RF-012 con mas detalle

**Impacto de la maquina del tiempo:**
El selector de ciclo en la UI permite cambiar el contexto de todos los dashboards y reportes. Al seleccionar un ciclo cerrado, los dashboards muestran los datos congelados de ese periodo.

### 6.10 HIST — Historial y Auditoria (RF-052 a RF-054)

**AuditLog NO tiene FK directa a Cycle** — es una tabla independiente que registra acciones del sistema completo.

Sin embargo, el audit log puede incluir `CycleId` como parte del `EntityId` o `Metadata`:

```
AuditLog
├── Action: "CycleStatusChanged"
├── EntityType: "Cycle"
├── EntityId: "{cycleId}"
├── PreviousValue: "Configuration"
├── NewValue: "ApplicationsOpen"
```

El historial de un becario (RF-053) cruza multiples ciclos: `ScholarAssignment WHERE UserId = X`, cada uno con su CycleId.

---

## 7. Reglas de Inmutabilidad al Cerrar Ciclo

### 7.1 Que se Congela

Cuando un ciclo transiciona a `Closed`, las siguientes entidades quedan **inmutables**:

| Entidad | Accion Bloqueada | Excepcion |
|---------|-----------------|-----------|
| `Cycle` | Modificar cualquier campo excepto metadatos | Ningun campo operativo |
| `CycleLocation` | Crear, modificar, eliminar | Ninguna |
| `SupervisorAssignment` | Crear, modificar, eliminar | Ninguna |
| `Application` | Modificar estado | Ninguna |
| `ScholarAssignment` | Modificar campos operativos | `EligibleForRenewal` se calcula al cerrar |
| `WorkShift` | Crear, modificar, eliminar | Ninguna |
| `Absence` | Crear, modificar, eliminar | Ninguna |
| `Document` | Eliminar | Ninguna (los documentos ya generados persisten) |

### 7.2 Mecanismo de Proteccion

**Opcion recomendada: Guard de dominio (Domain-level)**

Cada entidad que depende de un ciclo incluye una validacion en sus metodos de modificacion:

```csharp
// En ScholarAssignment, por ejemplo:
public Result RegisterWorkShift(...)
{
    if (Cycle.Status == CycleStatus.Closed)
        return Result.Error("CYCLE_CLOSED", "No se pueden registrar jornadas en un ciclo cerrado.");
    // ...
}
```

**Opcion complementaria: EF Core SaveChanges interceptor**

Un interceptor que detecta cambios en entidades asociadas a ciclos cerrados y lanza una excepcion:

```csharp
// SaveChangesInterceptor que verifica:
// Si la entidad tiene relacion con un Cycle en estado Closed
// Y el ChangeTracker marca la entidad como Modified/Added
// → Throw InvalidOperationException
```

**Recomendacion:** Usar **ambos**. El guard de dominio como proteccion primaria (retorna Result.Error amigable), y el interceptor como red de seguridad (previene bugs que salten el domain layer).

### 7.3 Pre-condiciones para Cerrar (RF-011)

```
Close() requiere:
1. DateTime.UtcNow >= Cycle.EndDate
   → "No se puede cerrar antes de la fecha de fin"

2. WorkShifts.Count(s => s.Status == PendingApproval) == 0
   → "Hay {n} jornadas pendientes de aprobar"

3. Bitacoras generadas para todos los ScholarAssignments activos
   → "Faltan bitacoras para {n} becarios"

Si todas las condiciones pasan:
4. Cycle.Status = Closed
5. Cycle.ClosedAt = DateTime.UtcNow
6. Cycle.ClosedBy = currentUserId
7. Para cada ScholarAssignment:
   - Calcular EligibleForRenewal
   - Status = Finished
8. Emitir CycleClosedEvent
```

### 7.4 Calculo de Elegibilidad de Renovacion

Al cerrar el ciclo, por cada `ScholarAssignment` con `Status == Active`:

```
Datos necesarios:
- TotalHoursWorked (del ScholarAssignment)
- TotalExpectedHours (calculado desde ScholarSchedule * semanas del ciclo)
- TotalAbsences (del ScholarAssignment)
- TotalExpectedDays (dias habiles del ciclo segun horario)
- JustifiedAbsences (Absences donde Status == Approved)

Calculos:
- AttendanceRate = 1 - (TotalAbsences - JustifiedAbsences) / TotalExpectedDays
- HoursCompletionRate = TotalHoursWorked / TotalExpectedHours

Elegible si:
- AttendanceRate >= 0.90 (90%)
- HoursCompletionRate >= 0.95 (95%)
- Status != Suspended (no estuvo suspendido)
```

---

## 8. Sketch de Entidad de Dominio (C#)

### 8.1 CycleStatus Enum

```csharp
namespace WorkScholarship.Domain.Enums;

/// <summary>
/// Estados posibles de un ciclo semestral.
/// </summary>
public enum CycleStatus
{
    /// <summary>
    /// Estado inicial. Permite configurar ubicaciones, horarios y supervisores.
    /// </summary>
    Configuration = 0,

    /// <summary>
    /// Periodo de postulaciones abierto. Postulantes pueden registrarse.
    /// </summary>
    ApplicationsOpen = 1,

    /// <summary>
    /// Postulaciones cerradas. Fase de entrevistas, evaluacion y seleccion.
    /// </summary>
    ApplicationsClosed = 2,

    /// <summary>
    /// Ciclo en operacion activa. Becarios trabajan y registran jornadas.
    /// </summary>
    Active = 3,

    /// <summary>
    /// Ciclo finalizado. Datos congelados como snapshot historico inmutable.
    /// </summary>
    Closed = 4
}
```

### 8.2 Cycle Entity (Sketch)

```csharp
namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa un ciclo semestral del programa de becas trabajo.
/// Entidad rica con maquina de estados y reglas de negocio.
/// </summary>
public class Cycle : BaseEntity
{
    private Cycle() { }

    /// <summary>
    /// Crea un nuevo ciclo en estado Configuration.
    /// </summary>
    public static Cycle Create(
        string name,
        string department,
        DateTime startDate,
        DateTime endDate,
        DateTime applicationDeadline,
        DateTime interviewDate,
        DateTime selectionDate,
        int totalScholarshipsAvailable,
        string createdBy)
    {
        // Validaciones de dominio:
        // - name no vacio
        // - department no vacio
        // - startDate < endDate
        // - applicationDeadline < interviewDate < selectionDate
        // - applicationDeadline > DateTime.UtcNow
        // - totalScholarshipsAvailable > 0

        var cycle = new Cycle
        {
            Name = name,
            Department = department,
            Status = CycleStatus.Configuration,
            StartDate = startDate,
            EndDate = endDate,
            ApplicationDeadline = applicationDeadline,
            InterviewDate = interviewDate,
            SelectionDate = selectionDate,
            TotalScholarshipsAvailable = totalScholarshipsAvailable,
            TotalScholarshipsAssigned = 0,
            CreatedBy = createdBy
        };

        cycle.AddDomainEvent(new CycleCreatedEvent(cycle.Id));
        return cycle;
    }

    // --- Properties (private set) ---

    public string Name { get; private set; }
    public string Department { get; private set; }
    public CycleStatus Status { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public DateTime ApplicationDeadline { get; private set; }
    public DateTime InterviewDate { get; private set; }
    public DateTime SelectionDate { get; private set; }

    public int TotalScholarshipsAvailable { get; private set; }
    public int TotalScholarshipsAssigned { get; private set; }

    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }

    // --- Configuration Progress ---

    /// <summary>
    /// Indica si las renovaciones fueron procesadas (o salteadas) para este ciclo.
    /// Se marca true automaticamente si es el primer ciclo de la dependencia.
    /// Este es el UNICO flag persistido — el resto del progreso se calcula
    /// dinamicamente desde el estado actual de CycleLocations y SupervisorAssignments.
    /// </summary>
    public bool RenewalProcessCompleted { get; private set; }

    /// <summary>
    /// Ciclo del cual se clono la configuracion (ubicaciones, supervisores, horarios).
    /// Null si la configuracion fue manual.
    /// </summary>
    public Guid? ClonedFromCycleId { get; private set; }

    // --- State Transitions ---

    /// <summary>
    /// Abre el periodo de postulaciones.
    /// Pre: estado Configuration, al menos 1 CycleLocation activa.
    /// </summary>
    public Result OpenApplications(int activeCycleLocationsCount)
    {
        if (Status != CycleStatus.Configuration)
            return Result.Error("INVALID_TRANSITION",
                "Solo se puede abrir postulaciones desde estado Configuration.");

        if (activeCycleLocationsCount == 0)
            return Result.Error("NO_LOCATIONS",
                "Debe haber al menos una ubicacion activa.");

        if (TotalScholarshipsAvailable <= 0)
            return Result.Error("NO_SCHOLARSHIPS",
                "El total de becas disponibles debe ser mayor a 0.");

        Status = CycleStatus.ApplicationsOpen;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsOpenedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Cierra el periodo de postulaciones.
    /// Pre: estado ApplicationsOpen.
    /// </summary>
    public Result CloseApplications()
    {
        if (Status != CycleStatus.ApplicationsOpen)
            return Result.Error("INVALID_TRANSITION",
                "Solo se puede cerrar postulaciones desde estado ApplicationsOpen.");

        Status = CycleStatus.ApplicationsClosed;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsClosedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Reabre el periodo de postulaciones (escape valve).
    /// Pre: estado ApplicationsClosed.
    /// </summary>
    public Result ReopenApplications()
    {
        if (Status != CycleStatus.ApplicationsClosed)
            return Result.Error("INVALID_TRANSITION",
                "Solo se puede reabrir postulaciones desde estado ApplicationsClosed.");

        Status = CycleStatus.ApplicationsOpen;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ApplicationsReopenedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Activa el ciclo tras confirmar la seleccion final.
    /// Pre: estado ApplicationsClosed.
    /// </summary>
    public Result Activate()
    {
        if (Status != CycleStatus.ApplicationsClosed)
            return Result.Error("INVALID_TRANSITION",
                "Solo se puede activar desde estado ApplicationsClosed.");

        Status = CycleStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleActivatedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Cierra el ciclo oficialmente. Datos quedan inmutables.
    /// Pre: estado Active, sin jornadas pendientes, bitacoras generadas.
    /// </summary>
    public Result Close(
        int pendingShiftsCount,
        int missingLogbooksCount,
        string closedBy)
    {
        if (Status != CycleStatus.Active)
            return Result.Error("INVALID_TRANSITION",
                "Solo se puede cerrar desde estado Active.");

        if (DateTime.UtcNow < EndDate)
            return Result.Error("CYCLE_NOT_ENDED",
                "No se puede cerrar antes de la fecha de fin del ciclo.");

        if (pendingShiftsCount > 0)
            return Result.Error("PENDING_SHIFTS",
                $"Hay {pendingShiftsCount} jornadas pendientes de aprobar.");

        if (missingLogbooksCount > 0)
            return Result.Error("MISSING_LOGBOOKS",
                $"Faltan bitacoras para {missingLogbooksCount} becarios.");

        Status = CycleStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleClosedEvent(Id));
        return Result.Success();
    }

    /// <summary>
    /// Extiende fechas del ciclo.
    /// Pre: estado Configuration, ApplicationsOpen o Active.
    /// </summary>
    public Result ExtendDates(
        DateTime? newApplicationDeadline,
        DateTime? newInterviewDate,
        DateTime? newSelectionDate,
        DateTime? newEndDate)
    {
        if (Status == CycleStatus.Closed)
            return Result.Error("CYCLE_CLOSED",
                "No se pueden modificar fechas de un ciclo cerrado.");

        if (Status == CycleStatus.ApplicationsClosed)
            return Result.Error("INVALID_TRANSITION",
                "No se pueden extender fechas en estado ApplicationsClosed.");

        // Validar que las nuevas fechas no sean menores que las actuales
        // Validar coherencia temporal

        // Aplicar cambios...
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CycleDatesExtendedEvent(Id));
        return Result.Success();
    }

    // --- Query Methods ---

    /// <summary>
    /// Indica si el ciclo esta en un estado donde se permiten modificaciones.
    /// </summary>
    public bool IsModifiable => Status != CycleStatus.Closed;

    /// <summary>
    /// Indica si el ciclo acepta nuevas postulaciones.
    /// </summary>
    public bool AcceptsApplications => Status == CycleStatus.ApplicationsOpen;

    /// <summary>
    /// Indica si el ciclo esta en operacion (becarios trabajando).
    /// </summary>
    public bool IsOperational => Status == CycleStatus.Active;
}
```

---

## 9. Commands y Queries CQRS

### 9.1 Commands (modifican estado)

| Command | RF | Handler Logic |
|---------|-----|--------------|
| `CreateCycleCommand` | RF-006 | Valida unicidad por departamento + estado, crea Cycle en Configuration |
| `ConfigureCycleCommand` | RF-007 | Agrega/modifica CycleLocations, ScheduleSlots, SupervisorAssignments |
| `OpenApplicationsCommand` | RF-008 | Llama `Cycle.OpenApplications()`, valida pre-condiciones |
| `CloseApplicationsCommand` | RF-009 | Llama `Cycle.CloseApplications()` |
| `ReopenApplicationsCommand` | RF-009 | Llama `Cycle.ReopenApplications()` |
| `ActivateCycleCommand` | RF-020 | Llama `Cycle.Activate()` (disparado por seleccion final) |
| `ExtendCycleDatesCommand` | RF-010 | Llama `Cycle.ExtendDates()`, notifica afectados |
| `CloseCycleCommand` | RF-011 | Valida pre-condiciones, calcula elegibilidad, llama `Cycle.Close()` |

### 9.2 Queries (solo lectura)

| Query | RF | Retorna |
|-------|-----|---------|
| `GetCycleByIdQuery` | RF-012 | Cycle con datos basicos |
| `GetCycleDetailQuery` | RF-012 | Cycle + metricas + conteos |
| `ListCyclesQuery` | RF-012 | Lista paginada con filtros (departamento, anio, estado) |
| `GetCycleStatisticsQuery` | RF-012 | Metricas consolidadas del ciclo |
| `GetActiveCycleQuery` | Interno | Ciclo activo de un departamento (para operacion diaria) |
| `GetCycleLocationsQuery` | RF-012 | CycleLocations del ciclo (maquina del tiempo) |
| `GetCycleScholarsQuery` | RF-012 | ScholarAssignments del ciclo |
| `GetCycleSupervisorsQuery` | RF-012 | SupervisorAssignments del ciclo |

### 9.3 Validators (FluentValidation)

Cada Command tiene su Validator correspondiente:

```
CreateCycleCommandValidator:
  - Name: NotEmpty, MaxLength(100)
  - Department: NotEmpty, MaxLength(100)
  - StartDate: GreaterThan(DateTime.UtcNow)
  - EndDate: GreaterThan(StartDate)
  - ApplicationDeadline: GreaterThan(DateTime.UtcNow), LessThan(InterviewDate)
  - InterviewDate: LessThan(SelectionDate)
  - SelectionDate: LessThan(EndDate)
  - TotalScholarshipsAvailable: GreaterThan(0)
```

---

## 10. Endpoints API REST

### 10.1 Ciclo Core

```
POST   /api/cycles                              → CreateCycleCommand (RF-006)
GET    /api/cycles                              → ListCyclesQuery (RF-012)
GET    /api/cycles/active?department={dept}      → GetActiveCycleQuery
GET    /api/cycles/{id}                         → GetCycleDetailQuery (RF-012)
PUT    /api/cycles/{id}/configure               → ConfigureCycleCommand (RF-007)
```

### 10.2 Transiciones de Estado

```
POST   /api/cycles/{id}/open-applications       → OpenApplicationsCommand (RF-008)
POST   /api/cycles/{id}/close-applications      → CloseApplicationsCommand (RF-009)
POST   /api/cycles/{id}/reopen-applications     → ReopenApplicationsCommand (RF-009)
POST   /api/cycles/{id}/activate                → ActivateCycleCommand (RF-020)
PUT    /api/cycles/{id}/extend-dates            → ExtendCycleDatesCommand (RF-010)
POST   /api/cycles/{id}/close                   → CloseCycleCommand (RF-011)
```

### 10.3 Maquina del Tiempo (Queries historicos)

```
GET    /api/cycles/{id}/locations               → GetCycleLocationsQuery
GET    /api/cycles/{id}/scholars                → GetCycleScholarsQuery
GET    /api/cycles/{id}/supervisors             → GetCycleSupervisorsQuery
GET    /api/cycles/{id}/statistics              → GetCycleStatisticsQuery
GET    /api/cycles/{id}/documents               → GetCycleDocumentsQuery
```

### 10.4 Autorizacion

Todos los endpoints de ciclo requieren rol **ADMIN**. Los queries historicos eventualmente podrian abrirse a SUPERVISOR (para ver sus ciclos anteriores), pero inicialmente solo ADMIN.

---

## 11. Aprendizajes del Predecesor Django

### 11.1 Modelo Selection (equivalente a Cycle)

El sistema Django anterior tenia un modelo `Selection` con:
- 5 estados (similares a los propuestos)
- Campos: `name`, `semester`, `year`, `status`, fechas
- FK a `BecaTrabajo` (equivalente a ScholarAssignment)

### 11.2 Limitaciones que Mejoramos

| Aspecto | Django (anterior) | .NET (nuevo) |
|---------|------------------|-------------|
| Ubicaciones por ciclo | Sin scope temporal — ubicaciones eran globales | `CycleLocation` junction table — cada ciclo tiene su config |
| Supervisores por ciclo | Asignacion permanente | `SupervisorAssignment` con CycleId |
| Horarios por ciclo | Fijos en la ubicacion | `ScheduleSlot` por CycleLocation — pueden cambiar por ciclo |
| Tracking diario | No implementado | Subsistema completo (RF-029 a RF-034) |
| Vista historica | No existia | "Maquina del tiempo" con queries por CycleId |
| Inmutabilidad | Sin proteccion | Guards de dominio + interceptor EF Core |
| Renovacion | Manual | Automatizada con calculo de elegibilidad (RN-002) |

### 11.3 Que Conservamos

- La estructura basica de 5 estados funciona bien
- La relacion User → ScholarAssignment (BecaTrabajo) es correcta
- El concepto de "dependencia" (department) como agrupador

---

## 12. Reglas de Negocio Consolidadas

### Del documento de requerimientos (REQUIREMENTS_COMPLETE.md)

| Codigo | Regla | Impacto en Ciclos |
|--------|-------|------------------|
| RN-001 | Solo 1 ciclo activo por dependencia | `CreateCycleCommand` valida contra ciclos existentes con Status != Closed |
| RN-002 | Renovacion: ≥90% asistencia + ≥95% horas | `CloseCycleCommand` calcula `EligibleForRenewal` por ScholarAssignment |
| RN-003 | Renovacion simplificada (solo horario + confirmacion) | RF-021 depende de datos de elegibilidad del ciclo anterior |
| RN-004 | Orden de asignacion: renovaciones primero | `Activate()` requiere que renovaciones se procesen antes |
| RN-005 | Suma becas por ubicacion ≤ total ciclo | `ConfigureCycleCommand` valida sum(CycleLocation.ScholarshipsAvailable) |
| RN-010 | Bitacora oficial requerida para cierre | `Close()` pre-condicion: bitacoras generadas |
| RN-015 | Al cerrar: datos congelados + elegibilidad calculada | Inmutabilidad enforced por domain guards |

### Reglas adicionales descubiertas en el analisis

| Codigo | Regla | Detalle |
|--------|-------|---------|
| RN-016 | Ciclo en Configuration permite cambios ilimitados | No hay restricciones de modificacion en este estado |
| RN-017 | ApplicationsClosed permite reabrir | Escape valve para errores administrativos |
| RN-018 | ExtendDates no puede reducir fechas pasadas | Si ApplicationDeadline ya paso, no se puede mover hacia atras |
| RN-019 | Solo Active permite extender EndDate | En otros estados se pueden extender otras fechas |
| RN-020 | CycleLocation.ScholarshipsAssigned es consistente | Se actualiza automaticamente al asignar/desasignar becarios |
| RN-021 | Primer ciclo no tiene renovaciones | `IsFirstCycle` calculado, `RenewalProcessCompleted` auto-true |
| RN-022 | Renovaciones deben procesarse antes de abrir postulaciones | Guard en `OpenApplications()`: `!IsFirstCycle && !RenewalProcessCompleted` |
| RN-023 | Clone de setup es opcional pero recomendado | `cloneFromCycleId` en CreateCycleCommand, copia CycleLocations + Supervisors + Schedules |
| RN-024 | Smart defaults no son obligatorios | Frontend sugiere nombre/fechas, admin puede modificar todo |
| RN-025 | Notificaciones de deadline no se duplican | `CycleNotificationLog` previene re-envio del mismo tipo |
| RN-026 | Admin debe ser notificado por email si no entra al sistema | Hangfire jobs envian emails en fechas criticas del ciclo |

---

## 13. Flujos UX del Admin — Edge Cases y Escenarios Reales

Esta seccion aborda los escenarios reales que el admin enfrenta al interactuar con el sistema, y como el sistema debe responder de manera inteligente en cada caso.

### 13.1 Escenario: Primer Login del Admin (Sistema Vacio)

Cuando un admin inicia sesion por primera vez y no existe ningun ciclo:

```
Admin hace login
    │
    ▼
GET /api/cycles/active?department=Biblioteca
    │
    ▼ (retorna null — no hay ciclo)
    │
    ▼
Dashboard muestra: ESTADO VACIO (Empty State)
┌──────────────────────────────────────────────────────┐
│                                                      │
│   Bienvenido al Sistema de Becas Trabajo             │
│                                                      │
│   Para comenzar, necesitas:                          │
│                                                      │
│   1. ☐ Configurar ubicaciones de tu dependencia      │
│      (Donde trabajaran los becarios)                 │
│                                                      │
│   2. ☐ Registrar supervisores                        │
│      (Quienes aprobaran jornadas)                    │
│                                                      │
│   3. ☐ Crear tu primer ciclo semestral               │
│      (Iniciar el proceso de seleccion)               │
│                                                      │
│   [ Comenzar Setup → ]                               │
│                                                      │
└──────────────────────────────────────────────────────┘
```

**Query necesario:** `GetAdminDashboardStateQuery`

```
Retorna:
- hasLocations: bool          → ¿Existen ubicaciones en la dependencia?
- hasSupervisors: bool        → ¿Existen usuarios con rol SUPERVISOR?
- activeCycle: CycleDto?      → Ciclo activo (null si no hay)
- lastClosedCycle: CycleDto?  → Ultimo ciclo cerrado (para clonar setup)
- pendingActions: string[]    → Lista de acciones pendientes
```

**Flujo del onboarding:**

```
Paso 1: Crear ubicaciones (UBIC — requiere subsistema de ubicaciones)
  → Admin crea "Sala de Lectura", "Area de Computo", etc.
  → Cada una con descripcion, imagen, tipo de horario

Paso 2: Registrar supervisores (AUTH — crear usuarios con rol SUPERVISOR)
  → Admin crea usuarios supervisor o cambia rol de existentes
  → Cada supervisor se asociara a ubicaciones en el paso del ciclo

Paso 3: Crear primer ciclo
  → Con ubicaciones y supervisores ya existentes, puede configurar el ciclo
```

### 13.2 Escenario: Admin con Ciclo Anterior Cerrado (Nuevo Semestre)

Este es el caso mas comun a partir del segundo ciclo:

```
Admin hace login
    │
    ▼
GET /api/cycles/active → null
GET /api/cycles?status=Closed&orderBy=EndDate&limit=1 → ultimo ciclo cerrado
    │
    ▼
Dashboard muestra:
┌──────────────────────────────────────────────────────┐
│                                                      │
│   No hay un ciclo activo                             │
│                                                      │
│   Ultimo ciclo: "2024-1" (Cerrado: 15/06/2024)      │
│   • 25 becarios participaron                         │
│   • 18 elegibles para renovacion                     │
│                                                      │
│   [ Iniciar Nuevo Ciclo → ]                          │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### 13.3 Creacion Inteligente de Ciclo (Smart Defaults + Clone)

Al crear un nuevo ciclo, el sistema ofrece **valores inteligentes por defecto** y la opcion de **clonar el setup anterior**:

```
POST /api/cycles (CreateCycleCommand)

Datos enviados por el frontend:
{
  "department": "Biblioteca",               // heredado del admin
  "cloneFromCycleId": "guid-del-anterior",  // NUEVO: clonar setup (opcional)

  // Smart defaults calculados por el frontend:
  "name": "2024-2",                         // deducido: año + semestre segun mes
  "startDate": "2024-08-01",               // inicio del semestre academico
  "endDate": "2024-12-15",                 // ~16 semanas despues
  "applicationDeadline": "2024-08-15",     // 2 semanas para postular
  "interviewDate": "2024-08-22",           // 1 semana despues del cierre
  "selectionDate": "2024-08-29",           // 1 semana despues de entrevistas
  "totalScholarshipsAvailable": 25         // heredado del ciclo anterior si existe
}
```

**Logica de Smart Defaults (frontend):**

```
Deduccion del nombre:
  - Mes 1-6  → "{año}-1" (primer semestre)
  - Mes 7-12 → "{año}-2" (segundo semestre)

Deduccion de fechas:
  - StartDate: primer dia del mes actual (o inicio del proximo semestre)
  - EndDate: StartDate + 16 semanas
  - ApplicationDeadline: StartDate + 2 semanas
  - InterviewDate: ApplicationDeadline + 1 semana
  - SelectionDate: InterviewDate + 1 semana

Todas son SUGERENCIAS que el admin puede modificar.
```

**Logica de Clone (backend — `CreateCycleCommandHandler`):**

```
Si cloneFromCycleId != null:
  1. Cargar ciclo anterior con CycleLocations + SupervisorAssignments + ScheduleSlots
  2. Crear nuevo ciclo
  3. Por cada CycleLocation del ciclo anterior:
     - Crear CycleLocation nuevo (misma Location, misma config)
     - Copiar ScheduleSlots
  4. Por cada SupervisorAssignment del ciclo anterior:
     - Crear SupervisorAssignment nuevo (mismo supervisor, misma ubicacion)
  5. Retornar ciclo con setup clonado

El admin ve inmediatamente:
"Se importo la configuracion de '2024-1':
 - 5 ubicaciones activas
 - 3 supervisores asignados
 ¿Desea mantener esta configuracion o modificarla?"
```

### 13.4 Persistencia del Progreso del Wizard

El wizard tiene **persistencia hibrida** para que el admin pueda dejarlo a medias y retomarlo:

**Backend (steps completados):** Cada step que se completa modifica datos reales en BD:
- Step 1 completado → Cycle existe en BD (status Configuration)
- Step 2 completado → CycleLocations creadas en BD
- Step 3 completado → SupervisorAssignments creados en BD
- Step 4 completado → RenewalProcessCompleted = true

**Frontend (step en progreso):** El formulario a medio llenar se guarda en `localStorage` como draft.

**Al volver:** Frontend consulta `GetDashboardStateQuery`, que retorna el progreso calculado. Si hay un draft en localStorage para el step actual, lo restaura.

**Cambio de dispositivo:** Pierde el draft del step actual, pero no los steps completados (están en BD).

### 13.5 Flujo Visual: Wizard de Configuracion de Ciclo

Despues de crear el ciclo (con o sin clone), el admin entra a un **wizard de configuracion**:

```
Paso 1/3: Ubicaciones
┌──────────────────────────────────────────────────────┐
│ Ubicaciones para ciclo "2024-2"                      │
│                                                      │
│ ✅ Sala de Lectura      [3 becas] [Editar] [Quitar]  │
│ ✅ Area de Computo      [2 becas] [Editar] [Quitar]  │
│ ✅ Hemeroteca           [2 becas] [Editar] [Quitar]  │
│ ☐  Archivo Central      [—]      [Agregar]          │
│ ☐  Sala Audiovisual     [—]      [Agregar]          │
│                                                      │
│ Total: 7/25 becas asignados a ubicaciones            │
│                                                      │
│                              [← Atras] [Siguiente →] │
└──────────────────────────────────────────────────────┘

Paso 2/3: Supervisores
┌──────────────────────────────────────────────────────┐
│ Supervisores para ciclo "2024-2"                     │
│                                                      │
│ Sala de Lectura:                                     │
│   → Juan Perez (supervisor@uni.edu)    [Cambiar]     │
│                                                      │
│ Area de Computo:                                     │
│   → Maria Garcia (mgarcia@uni.edu)     [Cambiar]     │
│                                                      │
│ Hemeroteca:                                          │
│   → ⚠ Sin supervisor asignado          [Asignar]     │
│                                                      │
│                              [← Atras] [Siguiente →] │
└──────────────────────────────────────────────────────┘

Paso 3/3: Horarios
┌──────────────────────────────────────────────────────┐
│ Horarios por ubicacion                               │
│                                                      │
│ ▼ Sala de Lectura (Unificado L-V)                    │
│   08:00-10:00  [2 becas requeridos]                  │
│   10:00-12:00  [1 beca requerido]                    │
│   14:00-16:00  [2 becas requeridos]                  │
│                                                      │
│ ▼ Area de Computo (Personalizado)                    │
│   L-M-X 09:00-11:00 [1 beca]                        │
│   J-V   14:00-16:00 [1 beca]                        │
│                                                      │
│              [← Atras] [Guardar y Listo ✓]           │
└──────────────────────────────────────────────────────┘
```

### 13.6 Escenario: Admin Inicia Sesion con Ciclo Activo (Operacion Normal)

```
Admin hace login
    │
    ▼
GET /api/cycles/active → { status: "Active", ... }
    │
    ▼
Dashboard normal con:
- Metricas del ciclo actual
- Alertas pendientes (jornadas sin aprobar, ausencias, etc.)
- Indicador de progreso del ciclo (semana 8/16)
- Barra de fecha fin con countdown si se acerca
```

### 13.7 Escenario: Admin No Inicia Sesion (Notificaciones Externas)

**Problema:** Si el admin no entra al sistema, no ve los avisos del dashboard. El sistema debe notificarlo proactivamente por email.

**Solucion:** Background jobs (Hangfire) que ejecutan checks periodicos:

```
CycleDeadlineCheckerJob (ejecuta diariamente a las 08:00):
    │
    ├── Buscar ciclos en estado Active
    │   ├── EndDate en 2 semanas → email "El ciclo se acerca a su fin"
    │   ├── EndDate en 1 semana → email "URGENTE: El ciclo cierra en 7 dias"
    │   ├── EndDate alcanzada → email "El ciclo debe cerrarse. Acciones pendientes: ..."
    │   └── EndDate + 1 semana → email "CRITICO: Ciclo vencido sin cerrar"
    │
    ├── Buscar ciclos en estado ApplicationsOpen
    │   └── ApplicationDeadline en 3 dias → email "Postulaciones cierran pronto"
    │
    ├── Buscar ciclos en estado Configuration (sin actividad > 7 dias)
    │   └── email "Tienes un ciclo en configuracion sin completar"
    │
    └── Buscar dependencias SIN ciclo activo (y ultimo cierre > 30 dias)
        └── email "No has iniciado un nuevo ciclo. ¿Deseas comenzar?"
```

**Tabla de notificaciones del ciclo:**

| Trigger | Anticipacion | Canal | Destinatario | Mensaje |
|---------|-------------|-------|-------------|---------|
| EndDate approaching | -14 dias | Email + In-App | ADMIN | "El ciclo {name} finaliza en 2 semanas" |
| EndDate approaching | -7 dias | Email + In-App | ADMIN | "URGENTE: {name} finaliza en 7 dias. {n} jornadas pendientes" |
| EndDate reached | 0 dias | Email + In-App | ADMIN | "El ciclo {name} puede cerrarse. Revisa las pre-condiciones" |
| EndDate passed | +7 dias | Email | ADMIN | "CRITICO: {name} lleva 7 dias vencido sin cerrar" |
| ApplicationDeadline | -3 dias | Email + In-App | ADMIN | "Postulaciones cierran en 3 dias" |
| ApplicationDeadline | -3 dias | Email | POSTULANTES incompletos | "Completa tu formulario antes del {fecha}" |
| Cycle stale (Config) | 7 dias sin cambios | Email | ADMIN | "Tienes un ciclo sin configurar" |
| No active cycle | 30 dias desde cierre | Email | ADMIN | "¿Listo para iniciar un nuevo ciclo?" |
| SelectionDate | -3 dias | Email + In-App | ADMIN | "Fecha de seleccion en 3 dias" |

### 13.8 Escenario: Admin Llega al Final del Ciclo

Cuando `EndDate` se acerca, el dashboard muestra un banner prominente:

```
┌──────────────────────────────────────────────────────┐
│ ⚠ El ciclo "2024-1" finaliza en 5 dias (15/06/2024) │
│                                                      │
│ Pre-condiciones para cerrar:                         │
│ ✅ Todas las bitacoras generadas (25/25)             │
│ ❌ 3 jornadas pendientes de aprobar                  │
│    → Juan Perez (2), Maria Lopez (1)                 │
│                                                      │
│ Opciones:                                            │
│ [ Extender Fecha → ]  [ Revisar Pendientes → ]      │
│                                                      │
└──────────────────────────────────────────────────────┘
```

Cuando TODAS las pre-condiciones se cumplen:

```
┌──────────────────────────────────────────────────────┐
│ ✅ El ciclo "2024-1" esta listo para cerrarse        │
│                                                      │
│ ✅ Todas las bitacoras generadas (25/25)             │
│ ✅ 0 jornadas pendientes de aprobar                  │
│ ✅ Fecha fin alcanzada                               │
│                                                      │
│ Al cerrar:                                           │
│ • Los datos quedan congelados (solo lectura)         │
│ • Se calcula elegibilidad de renovacion              │
│ • Se genera reporte final del ciclo                  │
│ • 18 becarios serian elegibles para renovacion       │
│                                                      │
│ [ Cerrar Ciclo Oficialmente → ]                      │
│                                                      │
└──────────────────────────────────────────────────────┘
```

---

## 14. Sistema de Notificaciones Proactivas del Ciclo

### 14.1 Arquitectura de Background Jobs

El sistema usa **Hangfire** para ejecutar jobs periodicos que monitorean el estado de los ciclos:

```
Jobs Recurrentes (RecurringJob):
├── CycleDeadlineCheckerJob        → Diario 08:00 UTC
│   Revisa fechas limite y envia alertas
│
├── StaleConfigurationCheckerJob   → Semanal (lunes 09:00)
│   Detecta ciclos en Configuration sin actividad
│
├── NoCycleReminderJob             → Quincenal
│   Detecta dependencias sin ciclo activo
│
└── PendingApprovalsReminderJob    → Diario 08:00 UTC
    Recuerda a supervisores sobre jornadas pendientes
```

### 14.2 Nuevo Command: Notificaciones del Ciclo

```
CheckCycleDeadlinesCommand (disparado por Hangfire):
  1. Obtener todos los ciclos no-cerrados
  2. Por cada ciclo, evaluar reglas de la tabla de notificaciones (seccion 13.6)
  3. Por cada regla que se cumple:
     - Verificar que la notificacion no se haya enviado ya (tabla NotificationLog)
     - Crear Notification in-app
     - Encolar email via IEmailService
  4. Registrar en NotificationLog para evitar duplicados
```

### 14.3 NotificationLog (Anti-duplicado)

```
CycleNotificationLog
├── Id (PK)
├── CycleId (FK)
├── NotificationType          string ("EndDate_14d", "EndDate_7d", "Stale", etc.)
├── SentAt                    DateTime
├── RecipientId               Guid (FK → User)
└── Channel                   string ("Email", "InApp")
```

Antes de enviar una notificacion, se verifica:
```sql
NOT EXISTS (
  SELECT 1 FROM CycleNotificationLog
  WHERE CycleId = @cycleId
    AND NotificationType = @type
    AND SentAt > @cutoffDate  -- evita re-enviar si ya se envio recientemente
)
```

---

## 15. Proceso de Renovacion dentro del Ciclo

### 15.1 El Problema

Si las ubicaciones y supervisores estan scoped al ciclo, cada nuevo ciclo requiere reconfiguracion. Esto es tedioso si la configuracion no cambia mucho entre ciclos. Ademas, los becarios elegibles del ciclo anterior deben tener prioridad.

### 15.2 Flujo Completo: Desde Cierre hasta Nuevo Ciclo

```
CICLO ANTERIOR ("2024-1")
         │
         ▼ Close() → calcula EligibleForRenewal por becario
         │
═════════════════════════════════════════════════════
         │
NUEVO CICLO ("2024-2")
         │
         ▼ Create(cloneFromCycleId: "2024-1")
         │   → Clona CycleLocations, Supervisors, Schedules
         │   → Status: Configuration
         │
         ▼ Admin revisa/modifica setup clonado (wizard)
         │
         ▼ Sistema identifica becarios elegibles del ciclo anterior
         │   → Query: ScholarAssignments WHERE CycleId = "2024-1"
         │     AND EligibleForRenewal = true AND User.IsActive = true
         │   → Resultado: 18 becarios elegibles
         │
         ▼ Admin envia invitaciones de renovacion
         │   → POST /api/cycles/{id}/send-renewal-invitations
         │   → Email a cada elegible: "Eres elegible para renovar. Sube tu horario"
         │
         ▼ Becarios responden (tienen un plazo, ej: 1 semana)
         │   → POST /api/cycles/{id}/renewals
         │   → Suben horario nuevo (PDF), confirman interes
         │
         ▼ Sistema procesa renovaciones automaticamente
         │   → Valida PDF, calcula compatibilidad con ubicacion anterior
         │   → Si compatibilidad >= 70% → Asignacion automatica (ScholarAssignment nuevo)
         │   → Si compatibilidad < 70% → Va al pool de postulantes normales
         │
         ▼ Admin confirma renovaciones
         │   → POST /api/cycles/{id}/confirm-renewals
         │   → TotalScholarshipsAvailable -= renewedCount
         │   → Plazas restantes disponibles para postulantes nuevos
         │
         ▼ OpenApplications() — solo para plazas restantes
         │   → Postulantes nuevos compiten por plazas no-renovadas
         │
         ▼ (flujo normal: postulaciones, entrevistas, seleccion...)
```

### 15.3 Sub-estados dentro de Configuration

En lugar de agregar mas estados a la maquina principal (que la complejizaria), el ciclo tiene **flags de progreso** dentro de Configuration:

```
Cycle (propiedades adicionales):
├── SetupCompleted               bool    Ubicaciones y supervisores configurados
├── RenewalProcessCompleted      bool    Renovaciones procesadas (o N/A si es primer ciclo)
├── IsFirstCycle                 bool    Calculado: no existe ciclo anterior en esta dependencia
```

**Validacion para OpenApplications():**

```csharp
public Result OpenApplications(int activeCycleLocationsCount)
{
    // ... validaciones existentes ...

    if (!SetupCompleted)
        return Result.Error("SETUP_INCOMPLETE",
            "Complete la configuracion de ubicaciones y supervisores.");

    if (!IsFirstCycle && !RenewalProcessCompleted)
        return Result.Error("RENEWALS_PENDING",
            "Procese las renovaciones antes de abrir postulaciones.");

    // ... transicion ...
}
```

### 15.4 El Primer Ciclo (Caso Especial)

El primer ciclo de una dependencia es el **origen** — no hay becarios anteriores, por lo que:

- `IsFirstCycle = true` (calculado: no existe ciclo anterior con Status == Closed)
- `RenewalProcessCompleted = true` (se marca automaticamente, no aplica)
- El wizard de creacion NO muestra el paso de renovaciones
- Todas las plazas van directamente a postulantes nuevos

### 15.5 Endpoints de Renovacion

```
GET    /api/cycles/{id}/renewal-candidates
  → Lista de becarios elegibles del ciclo anterior
  → Retorna: userId, name, previousLocation, eligibilityScore, status

POST   /api/cycles/{id}/send-renewal-invitations
  → Envia emails a todos los candidatos elegibles
  → Establece plazo de respuesta (configurable, default: 7 dias)

POST   /api/cycles/{id}/renewals
  → Becario sube horario y confirma interes
  → Body: { scheduleFile: PDF, confirmInterest: true }

GET    /api/cycles/{id}/renewal-results
  → Resultados del matching automatico
  → Retorna: renewedCount, incompatibleCount, pendingCount

POST   /api/cycles/{id}/confirm-renewals
  → Admin confirma las renovaciones procesadas
  → Crea ScholarAssignments para renovados
  → Actualiza plazas disponibles

POST   /api/cycles/{id}/skip-renewals
  → Admin decide saltar renovaciones (ej: cambio de politica)
  → Marca RenewalProcessCompleted = true sin procesar
```

### 15.6 Commands CQRS de Renovacion

| Command | Descripcion |
|---------|-------------|
| `GetRenewalCandidatesQuery` | Busca elegibles del ciclo anterior |
| `SendRenewalInvitationsCommand` | Envia emails, crea registros de invitacion |
| `SubmitRenewalCommand` | Becario sube horario y confirma |
| `ProcessRenewalsCommand` | Calcula compatibilidad, asigna automaticamente |
| `ConfirmRenewalsCommand` | Admin confirma, crea ScholarAssignments |
| `SkipRenewalsCommand` | Admin salta el proceso de renovacion |

### 15.7 Tabla Resumen: Que Pasa en Cada Ciclo

| Aspecto | Primer Ciclo | Segundo+ Ciclo |
|---------|-------------|---------------|
| Clone de setup | No disponible (no hay anterior) | Disponible (admin elige) |
| Smart defaults | Solo fechas deducidas | Fechas + totalScholarships del anterior |
| Renovaciones | N/A (IsFirstCycle=true) | Obligatorio antes de abrir postulaciones |
| Plazas para nuevos | 100% del total | Total - renovados |
| Wizard de config | 3 pasos (ubicaciones, supervisores, horarios) | 4 pasos (+renovaciones) |

---

## 16. Consideraciones de Implementacion

### 16.1 Orden de Implementacion Sugerido

```
Fase 1: Core del Ciclo (RF-006, RF-007)
├── Domain: CycleStatus enum, Cycle entity, CycleLocation, SupervisorAssignment
├── Application: CreateCycleCommand (con clone) + ConfigureCycleCommand
├── Application: GetAdminDashboardStateQuery (empty state / onboarding)
├── Infrastructure: EF Core configuration + migration
├── WebAPI: CyclesController (POST /cycles, GET /cycles, PUT /cycles/{id}/configure)
└── Tests: Domain + Application + WebAPI

Fase 2: Transiciones de Estado (RF-008, RF-009, RF-010)
├── Domain: State transition methods en Cycle (con flags SetupCompleted, RenewalProcessCompleted)
├── Application: Open/Close/Reopen/ExtendDates commands
├── Domain Events: CycleCreatedEvent, ApplicationsOpenedEvent, etc.
├── WebAPI: Endpoints de transicion
└── Tests: Todas las transiciones validas e invalidas

Fase 3: Renovacion (RF-021 parcial — solo la parte de ciclo)
├── Application: GetRenewalCandidatesQuery, SendRenewalInvitationsCommand
├── Application: SubmitRenewalCommand, ProcessRenewalsCommand, ConfirmRenewalsCommand
├── WebAPI: Endpoints de renovacion bajo /api/cycles/{id}/...
└── Tests: Flujo completo de renovacion

Fase 4: Cierre e Historia (RF-011, RF-012)
├── Domain: Close() con pre-condiciones, calculo de elegibilidad
├── Application: CloseCycleCommand + queries historicos (maquina del tiempo)
├── Infrastructure: Interceptor de inmutabilidad (complementario)
├── WebAPI: Close endpoint + queries historicos
└── Tests: Cierre con todas las combinaciones de pre-condiciones

Fase 5: Notificaciones Proactivas
├── Infrastructure: Hangfire jobs (CycleDeadlineCheckerJob, etc.)
├── Application: CheckCycleDeadlinesCommand
├── Domain: CycleNotificationLog entity
└── Tests: Cada regla de notificacion

Nota: Activate() (transicion a Active) se implementa junto con
el subsistema de Seleccion (RF-020), no aqui.
```

### 16.2 Entidades Pendientes de Definir

Las siguientes entidades se mencionan en este documento pero se implementaran con sus subsistemas respectivos:

- `Location` — Subsistema UBIC (RF-023)
- `ScheduleSlot` — Subsistema UBIC (RF-025)
- `Application` (Postulacion) — Subsistema SEL (RF-013)
- `ScholarAssignment` (BecaTrabajo) — Subsistema SEL (RF-020)
- `WorkShift` (Jornada) — Subsistema TRACK (RF-029)
- `Absence` (Ausencia) — Subsistema AUS (RF-035)
- `Document` — Subsistema DOC (RF-040)

Para el Cycle core (Fases 1-3), las entidades auxiliares necesarias son:
- `CycleLocation` — Se implementa junto con RF-007 (configurar ciclo)
- `SupervisorAssignment` — Se implementa junto con RF-007

### 16.3 Patrones de Backend a Seguir

Basado en el codebase existente (`User` entity, `AuthController`, etc.):

- **BaseEntity**: Heredar de `BaseEntity` (Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, DomainEvents)
- **Rich Entity**: Private setters, static `Create()`, metodos de comportamiento
- **Result<T>**: Para errores de negocio en transiciones de estado
- **CQRS**: Un Command/Query por operacion, cada uno con Handler + Validator
- **FluentValidation**: Reglas declarativas por Command
- **NSubstitute**: Mocking en tests
- **UserBuilder pattern**: Crear `CycleBuilder` para tests
- **InMemory EF Core**: Para tests de Application layer

### 16.4 Migracion de Base de Datos

Se necesitara una nueva migracion EF Core que cree:

```
Tablas nuevas:
- Cycles (entidad principal)
- CycleLocations (junction table)
- SupervisorAssignments (asignaciones temporales)

Indices sugeridos:
- IX_Cycles_Department_Status (para validar RN-001: 1 activo por dept)
- IX_CycleLocations_CycleId (para queries de maquina del tiempo)
- IX_SupervisorAssignments_CycleId
- IX_SupervisorAssignments_SupervisorId

Nota: La tabla Locations (catalogo maestro) se crea con el
subsistema UBIC, no aqui. CycleLocations depende de ella,
por lo que Location debe existir primero o crearse en la
misma migracion.
```

---

## Apendice A: Glosario de Entidades

| Entidad | Alias en Espanol | Subsistema | Temporal |
|---------|-----------------|-----------|----------|
| `Cycle` | Ciclo/Semestre | CICLO | Es la frontera temporal |
| `CycleLocation` | Ubicacion por Ciclo | CICLO + UBIC | Si (FK directa) |
| `Location` | Ubicacion | UBIC | No (catalogo maestro) |
| `SupervisorAssignment` | Asignacion Supervisor | CICLO + UBIC | Si (FK directa) |
| `Application` | Postulacion | SEL | Si (FK directa) |
| `ScholarAssignment` | BecaTrabajo | SEL | Si (FK directa) |
| `WorkShift` | Jornada | TRACK | Si (FK derivada) |
| `Absence` | Ausencia | AUS | Si (FK derivada) |
| `Document` | Documento | DOC | Si (FK directa) |
| `User` | Usuario | AUTH | No (atemporal) |

---

**Fin del documento de arquitectura.**

**Siguiente paso:** Implementar Fases 1-3 del Cycle core usando este blueprint como guia.
