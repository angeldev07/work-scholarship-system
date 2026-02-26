# Backoffice Design Document
## Work Scholarship System — Dashboard Shell

**Versión:** 1.0
**Fecha:** 2026-02-25
**Estado:** Pendiente de aprobación
**Autor:** Angular UX Engineer Agent

---

## Tabla de Contenidos

1. [Visión General](#1-visión-general)
2. [Layout y Diseño Visual](#2-layout-y-diseño-visual)
3. [Mapa de Navegación por Rol](#3-mapa-de-navegación-por-rol)
4. [Estructura de Componentes del Shell](#4-estructura-de-componentes-del-shell)
5. [Estructura de Rutas Angular](#5-estructura-de-rutas-angular)
6. [Estrategia de Control de Roles](#6-estrategia-de-control-de-roles)
7. [Convenciones de UI/UX y Design System](#7-convenciones-de-uiux-y-design-system)
8. [Fases de Implementación](#8-fases-de-implementación)

---

## 1. Visión General

### 1.1 Propósito

El **Backoffice Shell** es el layout principal del área autenticada del sistema. Es el contenedor que envuelve todas las vistas internas (dashboards, gestión de ciclos, tracking, etc.) y provee:

- Navegación lateral (sidebar) configurable por rol
- Barra superior con información del usuario activo
- Área de contenido donde se renderizan las vistas hijas
- Control de acceso visual (qué ve cada rol en el menú)

El shell **no implementa lógica de negocio**. Su responsabilidad es exclusivamente estructural y de navegación.

### 1.2 Alcance de este documento

Este documento define el **diseño e implementación del shell** (layout + navegación). **No incluye** las implementaciones de las vistas internas (dashboards con datos, formularios, tablas), que se documenta por separado en cada subsistema.

### 1.3 Principios de diseño aplicados

- **Role-driven**: La navegación se construye desde una configuración declarativa, no hardcoded en el template. Agregar una nueva sección solo requiere agregar una entrada al objeto de configuración del menú.
- **Extensible**: Nuevo feature = nueva entrada en el archivo de configuración de navegación + nuevas rutas lazy-loaded. El shell no cambia.
- **Modern UX**: Inspiración visual en Vercel Dashboard, Linear y Notion. Clean, minimal, denso en información pero sin ruido visual.
- **Mobile-first**: Sidebar colapsable. En mobile, el sidebar es un drawer que se abre sobre el contenido.
- **PrimeNG-first**: Usar componentes de PrimeNG v20 donde existan (Drawer, Avatar, Badge, Tooltip, Menu). Complementar con SCSS custom solo donde PrimeNG no cubra.

---

## 2. Layout y Diseño Visual

### 2.1 Estructura general del layout

El layout es un grid de dos columnas: sidebar fijo a la izquierda + área principal a la derecha. El área principal tiene una topbar fija en la parte superior y el contenido scrollable debajo.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        NAVEGADOR (100dvh)                           │
├──────────────┬──────────────────────────────────────────────────────┤
│              │  TOPBAR (fija, 64px)                                  │
│   SIDEBAR    ├──────────────────────────────────────────────────────┤
│   (fijo,     │                                                       │
│   256px      │  CONTENT AREA (scrollable)                           │
│   expanded   │                                                       │
│   64px       │  <router-outlet>                                     │
│   collapsed) │                                                       │
│              │                                                       │
│              │                                                       │
└──────────────┴──────────────────────────────────────────────────────┘
```

### 2.2 Estados del sidebar

```
EXPANDED (>768px, por defecto)         COLLAPSED (toggle manual)
┌─────────────────────┐                ┌──────┐
│ [Logo] WorkScholar  │                │ [W]  │
├─────────────────────┤                ├──────┤
│ ○ Dashboard         │                │  ○   │
│ ○ Ciclos            │                │  ○   │
│   ○ Activo          │                │  ○   │
│   ○ Historial       │                │  ○   │
│ ○ Selección         │                │      │
│   ○ Postulantes     │                │      │
│   ...               │                │      │
├─────────────────────┤                ├──────┤
│ [Avatar] Ana García │                │ [A]  │
│   ADMIN ▾           │                │      │
└─────────────────────┘                └──────┘

MOBILE DRAWER (< 768px)
┌──────────────────────────────────────┐
│ [overlay oscuro]                     │
│ ┌─────────────────┐                  │
│ │ [Logo] [X]      │                  │
│ │ ○ Dashboard     │                  │
│ │ ○ Ciclos        │                  │
│ │   ...           │                  │
│ │                 │                  │
│ │ [Avatar] Ana    │                  │
│ └─────────────────┘                  │
└──────────────────────────────────────┘
```

### 2.3 Topbar (Header)

```
┌──────────────────────────────────────────────────────────────────────┐
│ [≡ Toggle]  Gestión de Ciclos / Ciclo Activo          [🔔3] [Avatar] │
│             Breadcrumb contextual                                    │
└──────────────────────────────────────────────────────────────────────┘
```

Elementos de la topbar (de izquierda a derecha):
1. **Botón toggle del sidebar** — ícono hamburguesa, colapsa/expande el sidebar
2. **Breadcrumb** — navegación contextual (ej: "Gestión de Ciclos / Ciclo Activo")
3. **Indicador de notificaciones** — ícono campana con badge de conteo (RF-044)
4. **Avatar de usuario** — foto o iniciales + nombre + rol. Al hacer click, despliega un menú con: "Mi Perfil", "Cambiar Contraseña" (RF-005), separator, "Cerrar Sesión"

### 2.4 Anatomía del ítem de menú en el sidebar

```
ÍTEM NIVEL 1 (sección principal)
┌─────────────────────────────┐
│  [icon]  Ciclos          ▾  │   ← expandible si tiene hijos
└─────────────────────────────┘

ÍTEM NIVEL 1 (activo)
┌─────────────────────────────┐
│█ [icon]  Dashboard          │   ← borde izquierdo accent color
└─────────────────────────────┘

ÍTEM NIVEL 2 (sub-ítem)
┌─────────────────────────────┐
│       ·  Ciclo Activo       │   ← indentado, sin ícono
└─────────────────────────────┘

BADGE de conteo (acciones pendientes)
┌─────────────────────────────┐
│  [icon]  Jornadas       [5] │   ← badge numérico rojo
└─────────────────────────────┘
```

Los ítems con badge de conteo son especialmente importantes para supervisores (jornadas pendientes de aprobar) y admins (postulantes pendientes).

---

## 3. Mapa de Navegación por Rol

### Leyenda de iconos PrimeIcons

Todos los íconos se toman de PrimeIcons (incluido en el stack). La columna "PrimeIcon" usa la clase `pi pi-*`.

---

### 3.1 ADMIN — Navegación completa

| # | Sección (Nivel 1) | Sub-sección (Nivel 2) | PrimeIcon | Ruta Angular | RFs cubiertos |
|---|---|---|---|---|---|
| 1 | Dashboard | — | `chart-pie` | `/admin/dashboard` | RF-046 |
| 2 | Ciclos | Ciclo Activo | `calendar` | `/admin/cycles/active` | RF-006, RF-007, RF-008, RF-009, RF-010, RF-011 |
|   |        | Historial de Ciclos | `history` | `/admin/cycles/history` | RF-012, RF-051 |
| 3 | Selección | Postulantes | `users` | `/admin/selection/applicants` | RF-013, RF-014, RF-015, RF-017, RF-018, RF-019, RF-022, RF-049 |
|   |           | Asignación | `send` | `/admin/selection/assignment` | RF-019, RF-020 |
|   |           | Renovaciones | `sync` | `/admin/selection/renewals` | RF-021 |
| 4 | Ubicaciones | Gestionar | `map-marker` | `/admin/locations` | RF-023, RF-024, RF-025, RF-026, RF-027, RF-028 |
| 5 | Jornadas | Pendientes | `clock` | `/admin/shifts/pending` | RF-032, RF-033, RF-034 |
|   |          | Historial | `list` | `/admin/shifts/history` | RF-033 |
| 6 | Ausencias | Pendientes | `exclamation-circle` | `/admin/absences/pending` | RF-036, RF-037, RF-039 |
|   |           | Historial | `list` | `/admin/absences/history` | RF-037 |
| 7 | Documentos | Escarapelas | `id-card` | `/admin/documents/badges` | RF-040 |
|   |            | Bitácoras | `file-pdf` | `/admin/documents/logs` | RF-041 |
|   |            | Exportar | `download` | `/admin/documents/export` | RF-042 |
| 8 | Reportes | General | `chart-bar` | `/admin/reports` | RF-046, RF-049, RF-050, RF-051 |
| 9 | Notificaciones | Log de emails | `envelope` | `/admin/notifications` | RF-043, RF-044, RF-045 |
| 10 | Auditoría | Logs | `shield` | `/admin/audit` | RF-052, RF-053, RF-054 |
| 11 | Usuarios | Gestionar | `users` | `/admin/users` | RF-003 |

**Notas de diseño ADMIN:**
- Las secciones "Ciclos" y "Selección" tienen sub-menú expandible (accordion en sidebar).
- El ítem "Postulantes" en Selección muestra badge con el conteo de postulantes con formulario completo pendientes de revisión.
- "Jornadas > Pendientes" muestra badge con el total de jornadas pendientes de aprobación en todas las ubicaciones.
- La sección "Usuarios" aparece al fondo del sidebar separada por un divisor, dado que es configuración global.

---

### 3.2 SUPERVISOR — Navegación acotada

| # | Sección (Nivel 1) | Sub-sección (Nivel 2) | PrimeIcon | Ruta Angular | RFs cubiertos |
|---|---|---|---|---|---|
| 1 | Dashboard | — | `chart-pie` | `/supervisor/dashboard` | RF-047 |
| 2 | Mis Becas | Lista | `users` | `/supervisor/scholars` | RF-028, RF-050, RF-053 |
|   |           | Detalle (por ID) | — | `/supervisor/scholars/:id` | RF-050, RF-053 |
| 3 | Jornadas | Pendientes de Aprobar | `clock` | `/supervisor/shifts/pending` | RF-032 |
|   |          | Historial | `list` | `/supervisor/shifts/history` | RF-033 |
| 4 | Ausencias | Pendientes de Revisar | `exclamation-circle` | `/supervisor/absences/pending` | RF-036 |
|   |           | Historial | `list` | `/supervisor/absences/history` | RF-037 |
| 5 | Entrevistas | Programadas | `calendar` | `/supervisor/interviews` | RF-018 |
| 6 | Bitácora | Generar / Ver | `file-pdf` | `/supervisor/logbook` | RF-041 |

**Notas de diseño SUPERVISOR:**
- "Jornadas > Pendientes de Aprobar" es la vista central del supervisor. Aparece primero visualmente (después del Dashboard) y tiene un badge destacado con el número de jornadas en espera.
- "Ausencias > Pendientes de Revisar" también lleva badge.
- El sidebar del supervisor es más corto que el del admin — esto es intencional. Menos opciones = menos cognitive load.
- "Entrevistas" solo aparece cuando el ciclo está en fase de selección (RF-018). En ciclos activos/cerrados, puede ocultarse o aparecer en gris como "sin entrevistas programadas".

---

### 3.3 BECA (Scholar) — Navegación personal

| # | Sección (Nivel 1) | Sub-sección (Nivel 2) | PrimeIcon | Ruta Angular | RFs cubiertos |
|---|---|---|---|---|---|
| 1 | Mi Dashboard | — | `home` | `/scholar/dashboard` | RF-048 |
| 2 | Mi Jornada | Iniciar / Finalizar | `play-circle` | `/scholar/shift` | RF-029, RF-030, RF-031 |
| 3 | Mis Horas | Acumulado | `clock` | `/scholar/hours` | RF-033 |
| 4 | Ausencias | Reportar | `exclamation-circle` | `/scholar/absences/new` | RF-035 |
|   |           | Mis Solicitudes | `list` | `/scholar/absences` | RF-035 |
| 5 | Adelanto de Horas | Solicitar | `calendar-plus` | `/scholar/extra-hours/new` | RF-038 |
|   |                   | Mis Solicitudes | `list` | `/scholar/extra-hours` | RF-038 |
| 6 | Mi Perfil | Datos personales / Cambiar Contraseña | `user` | `/scholar/profile` | RF-005 |
| 7 | Postulación | Estado de mi postulación | `send` | `/scholar/application` | RF-015, RF-022 |

**Notas de diseño BECA:**
- La sección "Mi Jornada" es el corazón de la experiencia del beca. Su CTA principal es el botón grande "Iniciar Jornada" / "Finalizar Jornada" que depende del estado actual.
- "Mi Jornada" tiene un estado activo muy visible (color primario, badge "EN CURSO") cuando hay una jornada abierta.
- La sección "Postulación" solo aparece si el usuario fue creado durante un proceso de selección y su postulación sigue activa. Una vez seleccionado (rol BECA activo), esta sección desaparece.
- El sidebar del beca es el más corto de los tres. Pocas opciones, enfoque en la acción diaria.

---

### 3.4 Resumen de visibilidad por rol

| Sección | ADMIN | SUPERVISOR | BECA |
|---------|-------|------------|------|
| Dashboard | Si | Si | Si |
| Ciclos | Si | No | No |
| Selección / Postulantes | Si | Si (solo Entrevistas) | Si (solo su postulación) |
| Ubicaciones | Si | No (ve ubicación en su perfil) | No |
| Jornadas (supervisión) | Si | Si | No |
| Mi Jornada (check-in/out) | No | No | Si |
| Ausencias (aprobar) | Si | Si | No |
| Mis Ausencias (reportar) | No | No | Si |
| Mis Horas | No | No | Si |
| Adelanto de Horas | No | No | Si |
| Documentos | Si | Si (bitácoras) | No |
| Reportes | Si | No | No |
| Notificaciones | Si | No | No |
| Auditoría | Si | No | No |
| Usuarios | Si | No | No |
| Mi Perfil | Via topbar | Via topbar | Si (en sidebar) |

---

## 4. Estructura de Componentes del Shell

### 4.1 Árbol de componentes

```
src/app/
├── layout/
│   ├── auth-layout/                       ← EXISTENTE (solo envuelve auth)
│   │   ├── auth-layout.component.ts
│   │   ├── auth-layout.component.html
│   │   └── auth-layout.component.scss
│   │
│   └── shell/                             ← NUEVO (backoffice shell)
│       ├── shell.component.ts             ← Layout principal (grid sidebar + main)
│       ├── shell.component.html
│       ├── shell.component.scss
│       ├── shell.component.spec.ts
│       │
│       ├── components/
│       │   ├── sidebar/
│       │   │   ├── sidebar.component.ts   ← Sidebar con menú dinámico
│       │   │   ├── sidebar.component.html
│       │   │   ├── sidebar.component.scss
│       │   │   └── sidebar.component.spec.ts
│       │   │
│       │   ├── topbar/
│       │   │   ├── topbar.component.ts    ← Header con breadcrumb, notifs, user menu
│       │   │   ├── topbar.component.html
│       │   │   ├── topbar.component.scss
│       │   │   └── topbar.component.spec.ts
│       │   │
│       │   └── user-menu/
│       │       ├── user-menu.component.ts  ← Dropdown de usuario (perfil, logout)
│       │       ├── user-menu.component.html
│       │       ├── user-menu.component.scss
│       │       └── user-menu.component.spec.ts
│       │
│       ├── models/
│       │   └── navigation.models.ts       ← NavItem, NavGroup, NavConfig interfaces
│       │
│       └── services/
│           └── navigation.service.ts      ← Construye el menú según el rol del usuario
```

### 4.2 Responsabilidades de cada componente

#### ShellComponent
- Renderiza el layout de dos columnas (sidebar + main)
- Gestiona el estado de si el sidebar está colapsado o expandido
- En mobile, gestiona la apertura/cierre del drawer
- Contiene el `<router-outlet>` principal del área autenticada
- **No** conoce nada de lógica de negocio

#### SidebarComponent
- Recibe la configuración del menú como `input()` desde `NavigationService`
- Renderiza los ítems de menú (nivel 1 con posibles hijos nivel 2)
- Marca el ítem activo según la ruta actual (usando `RouterLinkActive`)
- Muestra badges de conteo cuando corresponde
- Emite evento `collapsed` hacia `ShellComponent`
- En modo colapsado, muestra solo íconos (con tooltip de PrimeNG al hacer hover)

#### TopbarComponent
- Recibe el estado de colapso del sidebar para ajustar el botón toggle
- Renderiza el breadcrumb dinámico (basado en la ruta activa y datos del menú)
- Muestra badge de notificaciones in-app (RF-044) — el badge se conectará al futuro `NotificationService`
- Contiene `UserMenuComponent`

#### UserMenuComponent
- Muestra avatar (foto del usuario o iniciales generadas) + nombre + rol
- Al hacer click, abre un `p-popover` (PrimeNG) con las opciones del usuario:
  - "Mi Perfil" → navega a la ruta de perfil correspondiente al rol
  - "Cambiar Contraseña" → navega a la vista de cambio de contraseña (RF-005)
  - Separator
  - "Cerrar Sesión" → llama a `AuthService.logout()`

#### NavigationService
- Servicio `providedIn: 'root'`
- Computed signal `navItems()` que devuelve el árbol de navegación filtrado por el rol del usuario actual
- La configuración base del menú es un array estático de `NavItem[]` con metadata de roles permitidos
- El servicio filtra ese array según `AuthService.currentUser()?.role`
- También expone un signal `pendingCounts()` para los badges (conectará con APIs futuras)

### 4.3 Modelos de navegación (navigation.models.ts)

```typescript
// Cada ítem del menú
export interface NavItem {
  id: string;                          // identificador único (para tracking y a11y)
  label: string;                       // texto visible
  icon: string;                        // clase de PrimeIcons (sin el 'pi pi-' prefix)
  route?: string;                      // ruta Angular (si es un enlace directo)
  children?: NavItem[];                // sub-ítems (nivel 2)
  roles: UserRole[];                   // roles que pueden ver este ítem
  badgeKey?: string;                   // key para lookup en pendingCounts signal
  isVisible?: boolean;                 // override programático (ej: postulación solo si activa)
}

// Grupo separador en el sidebar (ej: separar "Configuración" de items principales)
export interface NavGroup {
  label?: string;                      // label del grupo (opcional, puede ser solo un separator)
  items: NavItem[];
}

// Configuración completa de navegación
export type NavConfig = NavGroup[];
```

---

## 5. Estructura de Rutas Angular

### 5.1 Diseño de rutas completo

Las rutas del shell se organizan en tres feature areas lazy-loaded, cada una con su propio layout (el shell). El shell se introduce como un `parent route` con `loadComponent` que carga `ShellComponent`. Las rutas hijas se renderizan dentro del `<router-outlet>` del shell.

```typescript
// apps/web-angular/src/app/app.routes.ts — diseño objetivo

export const routes: Routes = [
  // Root redirect inteligente (según rol del usuario autenticado)
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },

  // Auth (público, guarded por guestGuard) — existente
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.authRoutes),
  },

  // ADMIN area — requiere authGuard + roleGuard([ADMIN])
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.ADMIN] },
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.adminRoutes),
  },

  // SUPERVISOR area — requiere authGuard + roleGuard([SUPERVISOR])
  {
    path: 'supervisor',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.SUPERVISOR] },
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    loadChildren: () => import('./features/supervisor/supervisor.routes').then(m => m.supervisorRoutes),
  },

  // SCHOLAR area — requiere authGuard + roleGuard([BECA])
  {
    path: 'scholar',
    canActivate: [authGuard, roleGuard],
    data: { roles: [UserRole.BECA] },
    loadComponent: () => import('./layout/shell/shell.component').then(m => m.ShellComponent),
    loadChildren: () => import('./features/scholar/scholar.routes').then(m => m.scholarRoutes),
  },

  // Páginas de error
  { path: 'forbidden', loadComponent: () => import('./shared/components/forbidden/forbidden.component').then(m => m.ForbiddenComponent) },
  { path: '**', loadComponent: () => import('./shared/components/not-found/not-found.component').then(m => m.NotFoundComponent) },
];
```

### 5.2 Rutas internas del ADMIN

```typescript
// apps/web-angular/src/app/features/admin/admin.routes.ts — objetivo

export const adminRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard
  { path: 'dashboard', loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },

  // Gestión de Ciclos (RF-006 a RF-012)
  {
    path: 'cycles',
    children: [
      { path: '', redirectTo: 'active', pathMatch: 'full' },
      { path: 'active', loadComponent: () => import('./cycles/active-cycle/active-cycle.component').then(m => m.ActiveCycleComponent) },
      { path: 'history', loadComponent: () => import('./cycles/cycle-history/cycle-history.component').then(m => m.CycleHistoryComponent) },
      { path: ':id', loadComponent: () => import('./cycles/cycle-detail/cycle-detail.component').then(m => m.CycleDetailComponent) },
    ],
  },

  // Proceso de Selección (RF-013 a RF-022)
  {
    path: 'selection',
    children: [
      { path: '', redirectTo: 'applicants', pathMatch: 'full' },
      { path: 'applicants', loadComponent: () => import('./selection/applicants/applicants.component').then(m => m.ApplicantsComponent) },
      { path: 'applicants/:id', loadComponent: () => import('./selection/applicant-detail/applicant-detail.component').then(m => m.ApplicantDetailComponent) },
      { path: 'assignment', loadComponent: () => import('./selection/assignment/assignment.component').then(m => m.AssignmentComponent) },
      { path: 'renewals', loadComponent: () => import('./selection/renewals/renewals.component').then(m => m.RenewalsComponent) },
    ],
  },

  // Gestión de Ubicaciones (RF-023 a RF-028)
  {
    path: 'locations',
    children: [
      { path: '', loadComponent: () => import('./locations/locations-list/locations-list.component').then(m => m.LocationsListComponent) },
      { path: 'new', loadComponent: () => import('./locations/location-form/location-form.component').then(m => m.LocationFormComponent) },
      { path: ':id', loadComponent: () => import('./locations/location-detail/location-detail.component').then(m => m.LocationDetailComponent) },
      { path: ':id/edit', loadComponent: () => import('./locations/location-form/location-form.component').then(m => m.LocationFormComponent) },
    ],
  },

  // Tracking / Jornadas (RF-032 a RF-034)
  {
    path: 'shifts',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      { path: 'pending', loadComponent: () => import('./shifts/shifts-pending/shifts-pending.component').then(m => m.ShiftsPendingComponent) },
      { path: 'history', loadComponent: () => import('./shifts/shifts-history/shifts-history.component').then(m => m.ShiftsHistoryComponent) },
      { path: ':id', loadComponent: () => import('./shifts/shift-detail/shift-detail.component').then(m => m.ShiftDetailComponent) },
    ],
  },

  // Ausencias (RF-036 a RF-039)
  {
    path: 'absences',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      { path: 'pending', loadComponent: () => import('./absences/absences-pending/absences-pending.component').then(m => m.AbsencesPendingComponent) },
      { path: 'history', loadComponent: () => import('./absences/absences-history/absences-history.component').then(m => m.AbsencesHistoryComponent) },
    ],
  },

  // Documentos (RF-040 a RF-042)
  {
    path: 'documents',
    children: [
      { path: '', redirectTo: 'badges', pathMatch: 'full' },
      { path: 'badges', loadComponent: () => import('./documents/badges/badges.component').then(m => m.BadgesComponent) },
      { path: 'logs', loadComponent: () => import('./documents/logbooks/logbooks.component').then(m => m.LogbooksComponent) },
      { path: 'export', loadComponent: () => import('./documents/export/export.component').then(m => m.ExportComponent) },
    ],
  },

  // Reportes (RF-046, RF-049 a RF-051)
  { path: 'reports', loadComponent: () => import('./reports/reports.component').then(m => m.ReportsComponent) },

  // Notificaciones (RF-043 a RF-045)
  { path: 'notifications', loadComponent: () => import('./notifications/notifications.component').then(m => m.NotificationsComponent) },

  // Auditoría (RF-052 a RF-054)
  { path: 'audit', loadComponent: () => import('./audit/audit.component').then(m => m.AuditComponent) },

  // Gestión de Usuarios (RF-003)
  {
    path: 'users',
    children: [
      { path: '', loadComponent: () => import('./users/users-list/users-list.component').then(m => m.UsersListComponent) },
      { path: ':id', loadComponent: () => import('./users/user-detail/user-detail.component').then(m => m.UserDetailComponent) },
    ],
  },
];
```

### 5.3 Rutas internas del SUPERVISOR

```typescript
// apps/web-angular/src/app/features/supervisor/supervisor.routes.ts — objetivo

export const supervisorRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard (RF-047)
  { path: 'dashboard', loadComponent: () => import('./dashboard/supervisor-dashboard.component').then(m => m.SupervisorDashboardComponent) },

  // Mis Becas (RF-050, RF-053)
  {
    path: 'scholars',
    children: [
      { path: '', loadComponent: () => import('./scholars/scholars-list/scholars-list.component').then(m => m.ScholarsListComponent) },
      { path: ':id', loadComponent: () => import('./scholars/scholar-detail/scholar-detail.component').then(m => m.ScholarDetailComponent) },
    ],
  },

  // Jornadas — Aprobar (RF-032, RF-033)
  {
    path: 'shifts',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      { path: 'pending', loadComponent: () => import('./shifts/shifts-pending/shifts-pending.component').then(m => m.ShiftsPendingComponent) },
      { path: 'history', loadComponent: () => import('./shifts/shifts-history/shifts-history.component').then(m => m.ShiftsHistoryComponent) },
      { path: ':id', loadComponent: () => import('./shifts/shift-review/shift-review.component').then(m => m.ShiftReviewComponent) },
    ],
  },

  // Ausencias — Revisar (RF-036, RF-037)
  {
    path: 'absences',
    children: [
      { path: '', redirectTo: 'pending', pathMatch: 'full' },
      { path: 'pending', loadComponent: () => import('./absences/absences-pending/absences-pending.component').then(m => m.AbsencesPendingComponent) },
      { path: 'history', loadComponent: () => import('./absences/absences-history/absences-history.component').then(m => m.AbsencesHistoryComponent) },
    ],
  },

  // Entrevistas (RF-018)
  { path: 'interviews', loadComponent: () => import('./interviews/interviews.component').then(m => m.InterviewsComponent) },

  // Bitácora (RF-041)
  { path: 'logbook', loadComponent: () => import('./logbook/logbook.component').then(m => m.LogbookComponent) },
];
```

### 5.4 Rutas internas del BECA (Scholar)

```typescript
// apps/web-angular/src/app/features/scholar/scholar.routes.ts — objetivo

export const scholarRoutes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Dashboard personal (RF-048)
  { path: 'dashboard', loadComponent: () => import('./dashboard/scholar-dashboard.component').then(m => m.ScholarDashboardComponent) },

  // Mi Jornada — Check-in / Check-out (RF-029, RF-030, RF-031)
  { path: 'shift', loadComponent: () => import('./shift/shift.component').then(m => m.ShiftComponent) },

  // Mis Horas acumuladas (RF-033)
  { path: 'hours', loadComponent: () => import('./hours/hours.component').then(m => m.HoursComponent) },

  // Ausencias (RF-035)
  {
    path: 'absences',
    children: [
      { path: '', loadComponent: () => import('./absences/absences-list/absences-list.component').then(m => m.AbsencesListComponent) },
      { path: 'new', loadComponent: () => import('./absences/absence-form/absence-form.component').then(m => m.AbsenceFormComponent) },
    ],
  },

  // Adelanto de Horas (RF-038)
  {
    path: 'extra-hours',
    children: [
      { path: '', loadComponent: () => import('./extra-hours/extra-hours-list/extra-hours-list.component').then(m => m.ExtraHoursListComponent) },
      { path: 'new', loadComponent: () => import('./extra-hours/extra-hours-form/extra-hours-form.component').then(m => m.ExtraHoursFormComponent) },
    ],
  },

  // Mi Perfil — incluye cambio de contraseña (RF-005)
  { path: 'profile', loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent) },

  // Estado de postulación (RF-015, RF-022) — solo visible si postulación activa
  { path: 'application', loadComponent: () => import('./application/application.component').then(m => m.ApplicationComponent) },
];
```

---

## 6. Estrategia de Control de Roles

### 6.1 Tres capas de protección

El control de roles opera en tres capas distintas que se complementan:

```
CAPA 1: Router Guards (rutas)
   authGuard + roleGuard([ADMIN]) en las rutas padre
   → Redirige a /auth/login si no autenticado
   → Redirige a /forbidden si rol incorrecto

CAPA 2: NavigationService (menú)
   Construye el árbol de menú filtrando por rol
   → Un SUPERVISOR nunca ve en el menú las secciones de ADMIN
   → La configuración del menú es la única fuente de verdad

CAPA 3: Directiva hasRole (elementos dentro de vistas)
   Para botones o secciones específicas dentro de una vista compartida
   → Ej: el botón "Aprobar en lote" solo visible para SUPERVISOR o ADMIN
   → Usar solo cuando la lógica de visibilidad no puede resolverse con rutas separadas
```

### 6.2 NavigationService — señales reactivas

```typescript
// Pseudo-código del NavigationService (señales)

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly authService = inject(AuthService);

  // Configuración estática del menú (fuente de verdad)
  private readonly NAV_CONFIG: NavConfig = [ /* ver sección 3 */ ];

  // Computed signal: menú filtrado por rol del usuario actual
  readonly navItems = computed(() => {
    const role = this.authService.currentUser()?.role ?? UserRole.NONE;
    return this.filterByRole(this.NAV_CONFIG, role);
  });

  // Computed signal: conteos de pendientes (se conectará a APIs futuras)
  // Se declara ahora como objeto vacío y se llenará cuando existan los servicios
  readonly pendingCounts = computed(() => ({
    shifts: 0,        // jornadas pendientes de aprobar
    absences: 0,      // ausencias pendientes de revisar
    applicants: 0,    // postulantes pendientes de revisar
  }));

  private filterByRole(config: NavConfig, role: UserRole): NavConfig { /* ... */ }
}
```

### 6.3 Directiva HasRole (para uso en templates)

Se implementa como una **directiva estructural** similar a `*ngIf` pero basada en roles:

```typescript
// Uso en template:
// <div *appHasRole="[UserRole.ADMIN, UserRole.SUPERVISOR]">
//   Contenido solo visible para admin o supervisor
// </div>

// La directiva inyecta AuthService y evalúa el rol usando signals
```

Esta directiva se usa **excepcionalmente**, solo cuando una vista es compartida entre roles y parte del contenido debe diferenciarse. Para el shell en sí, toda la separación se hace con rutas distintas por rol.

### 6.4 Redirección inteligente desde la raíz

Actualmente `''` redirige a `auth/login`. Una vez que el usuario esté autenticado y haga login, el `AuthService` redirige al área correcta según el rol. Este comportamiento ya está implementado en el flujo de login existente.

Para el shell, también se contempla: si un usuario autenticado llega a `/`, el redirect debería llevarlos directamente a su área. Esto puede implementarse en el guard raíz o con una ruta redirectora que consulte el rol del usuario.

---

## 7. Convenciones de UI/UX y Design System

### 7.1 Paleta de colores del shell

La paleta se define extendiendo los tokens existentes en `src/styles/tokens.scss`.

```scss
// Colores del sidebar y shell
$shell-sidebar-bg: #0f172a;          // Slate 900 — fondo del sidebar (dark, profesional)
$shell-sidebar-hover: #1e293b;       // Slate 800 — hover de ítem de menú
$shell-sidebar-active-bg: #1e3a5f;   // Azul oscuro — fondo de ítem activo
$shell-sidebar-active-accent: #3b82f6; // Blue 500 — borde izquierdo del ítem activo
$shell-sidebar-text: #94a3b8;        // Slate 400 — texto de ítem inactivo
$shell-sidebar-text-active: #f1f5f9; // Slate 100 — texto de ítem activo
$shell-sidebar-logo-text: #f8fafc;   // Slate 50 — texto del logo
$shell-sidebar-width-expanded: 256px;
$shell-sidebar-width-collapsed: 64px;

$shell-topbar-bg: #ffffff;           // Blanco — topbar con shadow sutil
$shell-topbar-height: 64px;
$shell-topbar-border: #e2e8f0;       // Slate 200 — línea inferior del topbar
$shell-topbar-shadow: 0 1px 3px rgba(0,0,0,0.06), 0 1px 2px rgba(0,0,0,0.04);

$shell-content-bg: #f8fafc;          // Slate 50 — fondo del área de contenido
```

**Justificación de la paleta:**
- Sidebar dark (Slate 900) crea una jerarquía visual clara entre navegación y contenido. Es el patrón usado por Linear, GitHub, Notion. Contrasta bien con el contenido claro.
- Topbar blanca con shadow sutil da sensación de elevación sobre el contenido.
- El fondo del contenido en Slate 50 (casi blanco) evita el blanco puro que genera fatiga visual en sesiones largas.

### 7.2 Tipografía del shell

```scss
// Sidebar
$shell-nav-font-size: 0.875rem;       // 14px — tamaño de texto de menú
$shell-nav-font-weight-normal: 400;
$shell-nav-font-weight-active: 500;   // Semi-bold en ítem activo
$shell-nav-group-label-size: 0.6875rem; // 11px — labels de grupos (UPPERCASE, letter-spacing)
$shell-nav-group-label-weight: 600;

// Topbar
$shell-topbar-breadcrumb-size: 0.875rem; // 14px
$shell-topbar-breadcrumb-weight: 500;
$shell-topbar-user-name-size: 0.875rem;
$shell-topbar-user-role-size: 0.6875rem;
```

### 7.3 Espaciado y dimensiones

```scss
// Sidebar
$shell-sidebar-padding-x: 16px;      // padding horizontal interno del sidebar
$shell-sidebar-item-padding: 10px 12px; // padding de cada ítem de menú
$shell-sidebar-item-gap: 2px;        // espacio entre ítems
$shell-sidebar-icon-size: 18px;
$shell-sidebar-logo-height: 56px;    // altura de la zona del logo

// Topbar
$shell-topbar-padding-x: 24px;

// Content
$shell-content-padding: 24px;        // padding interno del área de contenido (desktop)
$shell-content-padding-mobile: 16px;
```

### 7.4 Breakpoints y responsive

```scss
$breakpoint-mobile: 768px;   // < 768px = mobile (sidebar como drawer)
$breakpoint-tablet: 1024px;  // 768px - 1024px = tablet (sidebar colapsado por defecto)
$breakpoint-desktop: 1024px; // > 1024px = desktop (sidebar expandido por defecto)
```

- **Mobile (< 768px)**: Sidebar se convierte en drawer (PrimeNG Drawer). El botón de toggle en el topbar lo abre/cierra. El overlay oscuro cubre el contenido al abrirse.
- **Tablet (768px - 1024px)**: Sidebar visible pero colapsado por defecto (solo íconos, 64px). Toggle disponible para expandir.
- **Desktop (> 1024px)**: Sidebar expandido por defecto (256px). Toggle para colapsar si el usuario lo prefiere.

### 7.5 Componentes PrimeNG utilizados en el shell

| Componente | Uso |
|---|---|
| `p-drawer` | Sidebar en modo mobile |
| `p-avatar` | Avatar de usuario en topbar y user menu |
| `p-badge` | Badge de conteo en ítems del menú y campana |
| `p-tooltip` | Tooltip en ítems del sidebar colapsado |
| `p-popover` | Menú de usuario al hacer click en el avatar |
| `p-breadcrumb` | Breadcrumb en topbar |
| `p-divider` | Separadores en el sidebar (entre grupos de menú) |
| `p-button` | Botón de toggle del sidebar |
| `p-ripple` | Efecto ripple en ítems del menú (directiva) |

### 7.6 Animaciones del shell

```typescript
// Animaciones definidas en ShellComponent
animations: [
  // Sidebar expand/collapse (solo el contenido de texto)
  trigger('sidebarLabel', [
    transition(':enter', [
      style({ opacity: 0, width: 0 }),
      animate('200ms ease-out', style({ opacity: 1, width: '*' })),
    ]),
    transition(':leave', [
      animate('150ms ease-in', style({ opacity: 0, width: 0 })),
    ]),
  ]),

  // Sub-menú accordion
  trigger('submenuExpand', [
    transition(':enter', [
      style({ height: 0, opacity: 0 }),
      animate('200ms ease-out', style({ height: '*', opacity: 1 })),
    ]),
    transition(':leave', [
      animate('150ms ease-in', style({ height: 0, opacity: 0 })),
    ]),
  ]),
]
```

Criterio: **200ms ease-out** para entradas, **150ms ease-in** para salidas. Esto cumple con la guía de "motion con propósito" — las animaciones comunican el cambio de estado sin distraer.

---

## 8. Fases de Implementación

### Fase 1: Shell base funcional (lo que se implementa AHORA)

**Objetivo:** Tener el layout, navegación y guard de roles funcionando. Los dashboards placeholders existentes quedan dentro del shell.

**Entregables:**
1. `NavigationService` con la configuración completa del menú por rol (sin badges reales aún — todos en 0)
2. `ShellComponent` — layout grid sidebar + main, gestión de estado collapsed/expanded
3. `SidebarComponent` — menú dinámico desde NavigationService, active state, animación accordion
4. `TopbarComponent` — toggle, breadcrumb (simple al inicio), avatar de usuario
5. `UserMenuComponent` — popover con logout y navegación a perfil
6. `NavItem`, `NavGroup`, `NavConfig` interfaces en `navigation.models.ts`
7. Actualización de `admin.routes.ts`, `supervisor.routes.ts`, `scholar.routes.ts` con la estructura completa de rutas (la mayoría con componentes placeholder que se implementan en fases siguientes)
8. `HasRoleDirective` básica
9. Unit tests de todos los componentes del shell
10. Responsive: drawer en mobile, estados collapsed/expanded en tablet/desktop

**Lo que NO se implementa en esta fase:**
- Datos reales en los badges (se conectan cuando existan los servicios de cada subsistema)
- Las vistas internas (dashboards con datos, formularios, tablas) — solo placeholders
- Notificaciones in-app (RF-044) — la campana aparece en el topbar pero no conectada aún

### Fase 2: Módulo de Gestión de Ciclos (RF-006 a RF-012)

- Implementar las vistas bajo `/admin/cycles/*`
- Conectar el badge del sidebar con el estado del ciclo activo
- Formulario de creación de ciclo, configuración, timeline de estado

### Fase 3: Módulo de Ubicaciones (RF-023 a RF-028)

- Implementar las vistas bajo `/admin/locations/*`
- Formulario de ubicación con tipo de horario y slots
- Asignación de supervisores

### Fase 4: Módulo de Selección (RF-013 a RF-022)

- Upload de Excel con preview
- Vista de postulantes con filtros y compatibilidad
- Interfaz de asignación (drag-and-drop o tabla)
- Proceso de entrevistas
- Confirmación final

### Fase 5: Módulo de Tracking (RF-029 a RF-034)

- Vista de check-in/out del beca con captura de cámara
- Vista de supervisor para aprobar jornadas (con fotos)
- Badges reales conectados a la API
- Alertas de jornadas irregulares

### Fase 6: Módulo de Ausencias (RF-035 a RF-039)

- Formulario de reporte de ausencia del beca
- Vista de supervisor para revisar
- Contador de ausencias y alertas

### Fase 7: Documentos, Reportes y Notificaciones (RF-040 a RF-045)

- Generación de escarapelas y bitácoras
- Exportación de reportes
- Sistema de notificaciones in-app (conectar la campana del topbar)

### Fase 8: Historial y Auditoría (RF-052 a RF-054)

- Log de auditoría con filtros
- Historial del beca por ciclo

---

## Apéndice: Decisiones de Diseño y Alternativas Consideradas

### Decisión 1: Shell compartido vs Shell por rol

**Alternativa descartada:** Tener tres shells separados (`AdminShellComponent`, `SupervisorShellComponent`, `ScholarShellComponent`).

**Decisión elegida:** Un único `ShellComponent` con un `NavigationService` que construye el menú según el rol.

**Justificación:** Un solo shell es más mantenible. El layout es idéntico para los tres roles — solo difiere el contenido del menú. Duplicar el componente de layout por rol crearía tres puntos de mantenimiento para un cambio que debería ser uno solo (ej: cambiar el padding del sidebar).

### Decisión 2: Menú como configuración vs menú hardcoded en template

**Alternativa descartada:** Template con múltiples `@if (isAdmin())` / `@if (isSupervisor())` etc.

**Decisión elegida:** Configuración declarativa en `NavigationService` como array de `NavItem[]` con propiedad `roles: UserRole[]`.

**Justificación:** Agregar una nueva sección al backoffice en el futuro solo requiere agregar un objeto al array de configuración. El template del sidebar no cambia. Es el patrón usado por sistemas de administración empresariales (Ant Design Pro, AdminLTE).

### Decisión 3: PrimeNG Drawer vs implementación custom del sidebar

**Alternativa descartada:** Sidebar completamente custom con SCSS.

**Decisión elegida:** Custom SCSS para desktop (div con CSS transition en width), `p-drawer` de PrimeNG para mobile.

**Justificación:** En desktop, el sidebar colapsable necesita un control muy preciso del ancho con transición CSS — más fácil con CSS custom que adaptar el Drawer de PrimeNG. En mobile, el behavior de drawer (overlay, swipe, focus trap) es exactamente lo que ofrece `p-drawer`, por lo que se usa PrimeNG allí.

### Decisión 4: Breadcrumb dinámico vs título estático de página

**Alternativa descartada:** Cada componente de vista gestiona su propio título en el topbar.

**Decisión elegida:** El `TopbarComponent` calcula el breadcrumb automáticamente a partir de la ruta activa del router, usando el árbol de navegación del `NavigationService` como fuente de labels.

**Justificación:** Un breadcrumb centralizado garantiza consistencia. Si el label de una sección cambia, cambia en un solo lugar (la configuración del menú) y se refleja automáticamente en el breadcrumb.

---

*Documento generado por Angular UX Engineer Agent — Pendiente de revisión y aprobación antes de iniciar implementación.*
