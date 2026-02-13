# Work Scholarship Web (Angular)
## Frontend Completo en Angular 19

---

## 🎯 Descripción

Implementación completa del sistema usando **Angular 19** con arquitectura feature-based.

Incluye **todos los roles**:
- 👨‍💼 **Admin** - Gestión de ciclos, ubicaciones, selección
- 👔 **Supervisor** - Aprobación de jornadas, gestión de becas
- 🎓 **Beca** - Check-in/out, ausencias, consulta de horas

---

## 🚀 Quick Start

### Prerequisitos

- [Node.js 20+](https://nodejs.org/)
- [Angular CLI 19](https://angular.dev/tools/cli)

### 1. Instalar Dependencias

```bash
npm install
```

### 2. Generar Tipos desde API

```bash
npm run generate:types
```

Esto generará tipos TypeScript en `generated/` desde el OpenAPI del backend.

### 3. Ejecutar en Desarrollo

```bash
npm run dev
# o
ng serve
```

App estará en: `http://localhost:4200`

---

## 📦 Estructura de Carpetas

```
src/
├── app/
│   ├── core/                    # Singleton services, guards, interceptors
│   │   ├── guards/              # AuthGuard, RoleGuard
│   │   ├── interceptors/        # JwtInterceptor, ErrorInterceptor
│   │   ├── services/            # AuthService, ApiService
│   │   └── models/              # Core interfaces
│   │
│   ├── shared/                  # Componentes, directives, pipes compartidos
│   │   ├── components/          # Botones, modales, tablas reutilizables
│   │   ├── directives/
│   │   ├── pipes/
│   │   └── utils/
│   │
│   ├── features/                # Feature modules
│   │   ├── auth/                # Login, register, OAuth
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   └── auth.routes.ts
│   │   │
│   │   ├── admin/               # Módulo Admin
│   │   │   ├── cycles/          # Gestión de ciclos
│   │   │   ├── locations/       # Gestión de ubicaciones
│   │   │   ├── selection/       # Proceso de selección
│   │   │   ├── reports/         # Reportes y dashboards
│   │   │   └── admin.routes.ts
│   │   │
│   │   ├── supervisor/          # Módulo Supervisor
│   │   │   ├── approvals/       # Aprobar jornadas
│   │   │   ├── scholars/        # Gestión de becas
│   │   │   ├── dashboard/
│   │   │   └── supervisor.routes.ts
│   │   │
│   │   └── scholar/             # Módulo Beca
│   │       ├── tracking/        # Check-in/out
│   │       ├── absences/        # Reportar ausencias
│   │       ├── hours/           # Consultar horas
│   │       ├── dashboard/
│   │       └── scholar.routes.ts
│   │
│   ├── layout/                  # Layouts de la app
│   │   ├── main-layout/         # Layout principal (sidebar, navbar)
│   │   ├── auth-layout/         # Layout para login/register
│   │   └── public-layout/
│   │
│   ├── app.config.ts            # App configuration
│   ├── app.routes.ts            # Root routing
│   └── app.component.ts
│
├── assets/                      # Imágenes, fonts, etc.
├── environments/                # Environment configs
├── styles/                      # Global styles
└── generated/                   # Tipos generados desde OpenAPI (gitignored)
```

---

## 🎨 UI Library

**PrimeNG 18** - Componentes UI enterprise-ready

### Componentes Principales Usados

- `p-table` - Tablas con paginación, filtros, sort
- `p-dialog` - Modales
- `p-calendar` - Date picker
- `p-fileUpload` - Upload de archivos
- `p-chart` - Gráficos (Dashboard)
- `p-toast` - Notificaciones
- `p-confirmDialog` - Confirmaciones

---

## 🔧 Scripts

```bash
# Desarrollo
npm run dev              # ng serve (port 4200)
npm start                # Alias de dev

# Build
npm run build            # Build para producción
npm run build:dev        # Build para desarrollo

# Testing
npm run test             # Ejecutar tests (Karma)
npm run test:coverage    # Tests con coverage
npm run e2e              # Tests end-to-end

# Linting
npm run lint             # ESLint
npm run lint:fix         # ESLint con auto-fix

# Generación
npm run generate:types   # Generar tipos desde OpenAPI
```

---

## 🔑 Autenticación

### Login Flow

1. Usuario ingresa email/password o usa OAuth (Google)
2. API retorna JWT token
3. Token se guarda en `localStorage` (o `sessionStorage`)
4. `JwtInterceptor` agrega token a todos los requests
5. `AuthGuard` protege rutas según rol

### Roles y Rutas

| Rol | Rutas Base | Guard |
|-----|-----------|-------|
| ADMIN | `/admin/*` | `RoleGuard(['ADMIN'])` |
| SUPERVISOR | `/supervisor/*` | `RoleGuard(['SUPERVISOR'])` |
| BECA | `/scholar/*` | `RoleGuard(['BECA'])` |

---

## 🌐 Comunicación con API

### Generated Types

Los tipos TypeScript se generan automáticamente desde el OpenAPI del backend:

```typescript
// generated/models/User.ts
export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
}

// generated/enums/UserRole.ts
export enum UserRole {
  ADMIN = 'ADMIN',
  SUPERVISOR = 'SUPERVISOR',
  BECA = 'BECA'
}
```

### API Service

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../../generated/models/User';

@Injectable({ providedIn: 'root' })
export class UserService {
  private apiUrl = 'https://localhost:7001/api';

  constructor(private http: HttpClient) {}

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/auth/me`);
  }
}
```

---

## 📱 Responsive Design

- **Desktop first** con breakpoints:
  - `xl`: 1200px+
  - `lg`: 992px - 1199px
  - `md`: 768px - 991px
  - `sm`: 576px - 767px
  - `xs`: <576px

- Mobile-friendly para módulo **Scholar** (check-in con cámara)

---

## 🧪 Testing

### Unit Tests (Karma + Jasmine)

```bash
npm run test
```

### E2E Tests (Playwright/Cypress)

```bash
npm run e2e
```

---

## 🚀 Deployment

### Build para Producción

```bash
npm run build
```

Output en: `dist/web-angular/`

### Deploy a Vercel/Netlify

```bash
# Vercel
vercel --prod

# Netlify
netlify deploy --prod --dir=dist/web-angular
```

---

## 📖 Recursos

- [Angular Documentation](https://angular.dev/)
- [PrimeNG Documentation](https://primeng.org/)
- [RxJS Documentation](https://rxjs.dev/)
- [Angular Style Guide](https://angular.dev/style-guide)
