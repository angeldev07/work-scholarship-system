# Plan: Cycle Management Deep Analysis Document (RF-006 to RF-012)

## Context

Se solicitó un análisis profundo del subsistema de **Gestión de Ciclos** (RF-006 a RF-012) antes de la implementación. La idea clave es el **concepto de "máquina del tiempo"**: al visualizar un ciclo pasado, todos los datos relacionados (becarios, jornadas, ausencias, ubicaciones, supervisores) deben poder cargarse como una instantánea histórica congelada. Este documento servirá como **blueprint arquitectónico** — estableciendo la base lógica, estructural y arquitectónica sin implementar código.

## Qué se Creará

Un documento comprensivo de arquitectura en:
- **`docs/architecture/cycles/CYCLE_ARCHITECTURE.md`**

---

## Estructura del Documento

### 1. Diseño de la Entidad Cycle
- Entidad Cycle completa con todas las propiedades mapeadas desde RF-006
- Máquina de 5 estados: `Configuration → ApplicationsOpen → ApplicationsClosed → Active → Closed`
- Reglas de transición de estado y validación por transición
- Regla de negocio: solo 1 ciclo activo por dependencia (RN-001)

### 2. Especificación de la Máquina de Estados
- Transiciones válidas con pre-condiciones
- Guard clauses por transición (ej: "debe tener >=1 ubicación activa para abrir postulaciones")
- Quién puede disparar cada transición (todas solo ADMIN)
- Eventos emitidos por transición (para notificaciones, auditoría)

### 3. Concepto "Máquina del Tiempo" — Arquitectura de Datos Temporal
- **Principio de diseño**: CycleId como frontera temporal universal
- Toda entidad (excepto User, RefreshToken) tiene scope a un ciclo vía FK directa o derivada
- Cuando un ciclo está `Closed`, todos sus datos se vuelven **inmutables** (snapshot congelado)
- Consultar por CycleId reconstruye el estado completo de ese período
- **Cadena de dependencias**: Cycle → (Locations-per-cycle, Supervisors-per-cycle, Scholars, Shifts, Absences, Documents)

### 4. Análisis de Impacto Cross-Subsistema
Mapa de cómo CycleId se propaga a través de los 10 subsistemas:

| Subsistema | RFs | Relación con Cycle | Tipo de FK |
|-----------|-----|-------------------|------------|
| **AUTH** (RF-001-005) | Sin CycleId directo — Users son independientes del ciclo | N/A |
| **CICLO** (RF-006-012) | Entidad core, dueña de la máquina de estados | Es el Cycle |
| **SEL** (RF-013-022) | Postulacion.CycleId, BecaTrabajo.CycleId — selección es por ciclo | FK Directa |
| **UBIC** (RF-023-028) | CycleLocation (junction table), SupervisorAssignment.CycleId | FK Directa |
| **TRACK** (RF-029-034) | Jornada → BecaTrabajo → Cycle | FK Derivada |
| **AUS** (RF-035-039) | Ausencia → BecaTrabajo → Cycle | FK Derivada |
| **DOC** (RF-040-042) | Bitacora/Escarapela con scope a ciclo | FK Directa |
| **NOTIF** (RF-043-045) | Eventos disparados por transiciones de ciclo | Evento |
| **REP** (RF-046-051) | Todos los dashboards filtrados por ciclo activo/seleccionado | Filtro Query |
| **HIST** (RF-052-054) | AuditLog es independiente del ciclo, pero consultable por contexto | Contextual |

### 5. Diagrama de Relaciones de Entidades (ASCII)
- Mostrar Cycle como entidad central
- Todas las relaciones de primer orden (FK directa)
- Todas las relaciones de segundo orden (derivadas via BecaTrabajo)

### 6. Reglas de Inmutabilidad al Cerrar Ciclo
- Qué se congela y cómo (EF Core interceptor o guard de dominio)
- Validación que debe pasar antes de cerrar (criterios de RF-011):
  - Fecha actual ≥ fecha fin del ciclo
  - Todas las bitácoras generadas
  - No hay jornadas pendientes de aprobar
- Cálculo de elegibilidad de renovación (RN-002: ≥90% asistencia, ≥95% horas)

### 7. Sketch de Entidad de Dominio (Cycle)
- Properties, factory methods, métodos de transición de estado
- Siguiendo patrones existentes de User entity (private setters, static Create, domain methods)
- Enums: CycleStatus

### 8. Patrones Clave de Backend a Seguir
Basado en análisis del codebase existente:
- `BaseEntity` base class (Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, DomainEvents)
- Rich domain entity con private setters y factory `Create()` method
- CQRS: un Command por transición de estado (CreateCycleCommand, OpenApplicationsCommand, etc.)
- FluentValidation por command
- Result<T> para errores de negocio
- ApiResponse<T> wrapper

### 9. Aprendizajes del Predecesor Django
- Django tenía modelo `Selection` (equivalente a Cycle) con 5 estados
- Las Ubicaciones NO tenían scope de ciclo en Django — **esto lo mejoramos**
- No se implementó tracking diario — pizarra en blanco para subsistema TRACK
- No existía mecanismo de vista histórica — la "máquina del tiempo" es completamente nueva

---

## Archivos Clave Referenciados
- `apps/api/src/WorkScholarship.Domain/Common/BaseEntity.cs` — patrón de entidad base
- `apps/api/src/WorkScholarship.Domain/Entities/User.cs` — patrón de entidad rica a seguir
- `apps/api/src/WorkScholarship.Domain/Enums/UserRole.cs` — patrón de enums
- `docs/requirements/es/REQUIREMENTS_COMPLETE.md` — los 54 RFs (RF-006 a RF-012 específicamente)
- `docs/architecture/backoffice/BACKOFFICE_DESIGN.md` — formato de doc de arquitectura existente

## Verificación
1. Revisar el documento MD generado para completitud contra los 7 RFs de ciclo
2. Verificar que cada dependencia CycleId de cada subsistema esté documentada
3. Confirmar que las transiciones de la máquina de estados coincidan con criterios de RF-008, RF-009, RF-011
4. Asegurar que las reglas de inmutabilidad se alineen con RN-015 (reglas de cierre de ciclo)
5. Validar que el diseño de entidades siga los patrones de BaseEntity + User
