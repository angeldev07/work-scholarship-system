# Work Scholarship Web (Next.js)
## Frontend Completo en Next.js 15 con App Router

---

## 🎯 Descripción

Implementación completa del sistema usando **Next.js 15** con **App Router** y **Server Components**.

Incluye **todos los roles**:
- 👨‍💼 **Admin** - Gestión de ciclos, ubicaciones, selección
- 👔 **Supervisor** - Aprobación de jornadas, gestión de becas
- 🎓 **Beca** - Check-in/out, ausencias, consulta de horas

---

## 🚀 Quick Start

### Prerequisitos

- [Node.js 20+](https://nodejs.org/)

### 1. Instalar Dependencias

```bash
npm install
```

### 2. Generar Tipos desde API

```bash
npm run generate:types
```

Esto generará tipos TypeScript en `generated/` desde el OpenAPI del backend.

### 3. Configurar Environment Variables

Crear archivo `.env.local`:

```env
# API
NEXT_PUBLIC_API_URL=https://localhost:7001/api

# OAuth Google
NEXT_PUBLIC_GOOGLE_CLIENT_ID=your-client-id
```

### 4. Ejecutar en Desarrollo

```bash
npm run dev
```

App estará en: `http://localhost:3000`

---

## 📦 Estructura de Carpetas (App Router)

```
src/
├── app/                         # App Router (Next.js 15)
│   ├── (public)/                # Public routes (no auth)
│   │   ├── login/
│   │   └── register/
│   │
│   ├── (auth)/                  # Protected routes (authenticated)
│   │   ├── (admin)/             # Admin routes
│   │   │   ├── cycles/
│   │   │   ├── locations/
│   │   │   ├── selection/
│   │   │   ├── reports/
│   │   │   └── dashboard/
│   │   │
│   │   ├── (supervisor)/        # Supervisor routes
│   │   │   ├── approvals/
│   │   │   ├── scholars/
│   │   │   └── dashboard/
│   │   │
│   │   └── (scholar)/           # Scholar routes
│   │       ├── tracking/
│   │       ├── absences/
│   │       ├── hours/
│   │       └── dashboard/
│   │
│   ├── api/                     # API Routes (for OAuth, webhooks, etc.)
│   │   ├── auth/
│   │   └── webhooks/
│   │
│   ├── layout.tsx               # Root layout
│   ├── page.tsx                 # Home page (redirect)
│   └── not-found.tsx
│
├── components/                  # React Components
│   ├── ui/                      # UI primitives (shadcn/ui)
│   │   ├── button.tsx
│   │   ├── input.tsx
│   │   ├── dialog.tsx
│   │   └── ...
│   │
│   ├── layout/                  # Layout components
│   │   ├── sidebar.tsx
│   │   ├── navbar.tsx
│   │   └── footer.tsx
│   │
│   └── features/                # Feature-specific components
│       ├── tracking/
│       ├── selection/
│       └── ...
│
├── lib/                         # Utilities y configuración
│   ├── api/                     # API client
│   │   ├── client.ts            # Axios/Fetch wrapper
│   │   └── endpoints.ts
│   │
│   ├── auth/                    # Auth utilities
│   │   ├── session.ts
│   │   └── guards.ts
│   │
│   ├── hooks/                   # Custom React hooks
│   │   ├── useAuth.ts
│   │   ├── useApi.ts
│   │   └── ...
│   │
│   ├── utils/                   # Helper functions
│   │   ├── formatters.ts
│   │   └── validators.ts
│   │
│   └── constants.ts
│
├── types/                       # TypeScript types
│   └── index.ts
│
├── generated/                   # Tipos generados desde OpenAPI (gitignored)
│
├── public/                      # Static assets
│   ├── images/
│   └── icons/
│
└── styles/                      # Global styles
    └── globals.css
```

---

## 🎨 UI Library

**shadcn/ui** + **Tailwind CSS** - Componentes modernos y customizables

### Componentes Principales

- `Button` - Botones con variantes
- `Dialog` - Modales
- `Table` - Tablas con sort y filtros
- `Form` - Formularios con validación (React Hook Form + Zod)
- `Calendar` - Date picker
- `Toast` - Notificaciones
- `Avatar` - Avatares de usuario

---

## 🔧 Scripts

```bash
# Desarrollo
npm run dev              # Next.js dev server (port 3000)

# Build
npm run build            # Build para producción
npm run start            # Ejecutar build de producción

# Testing
npm run test             # Vitest
npm run test:ui          # Vitest UI
npm run test:coverage    # Coverage

# Linting
npm run lint             # ESLint
npm run lint:fix         # ESLint con auto-fix

# Type checking
npm run type-check       # TypeScript check

# Generación
npm run generate:types   # Generar tipos desde OpenAPI
```

---

## 🔑 Autenticación (Next.js App Router)

### Auth con Server Components

```typescript
// lib/auth/session.ts
import { cookies } from 'next/headers';

export async function getSession() {
  const token = cookies().get('auth-token')?.value;
  if (!token) return null;

  // Validate token and get user
  const user = await fetchUserFromToken(token);
  return user;
}
```

### Protected Routes

```typescript
// app/(auth)/admin/layout.tsx
import { redirect } from 'next/navigation';
import { getSession } from '@/lib/auth/session';

export default async function AdminLayout({ children }) {
  const session = await getSession();

  if (!session || session.role !== 'ADMIN') {
    redirect('/login');
  }

  return <>{children}</>;
}
```

### Route Groups

- `(public)/` - No requiere auth
- `(auth)/` - Requiere auth
- `(admin)/` - Requiere rol ADMIN
- `(supervisor)/` - Requiere rol SUPERVISOR
- `(scholar)/` - Requiere rol BECA

---

## 🌐 Comunicación con API

### API Client

```typescript
// lib/api/client.ts
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para agregar token
apiClient.interceptors.request.use((config) => {
  const token = getTokenFromCookie();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### Server Actions (Opcional)

```typescript
// app/(auth)/admin/actions.ts
'use server';

import { revalidatePath } from 'next/cache';

export async function createCycle(formData: FormData) {
  const data = {
    name: formData.get('name'),
    startDate: formData.get('startDate'),
    // ...
  };

  await apiClient.post('/cycles', data);
  revalidatePath('/admin/cycles');
}
```

---

## 📱 Responsive Design

- **Mobile first** con Tailwind breakpoints:
  - `sm`: 640px+
  - `md`: 768px+
  - `lg`: 1024px+
  - `xl`: 1280px+
  - `2xl`: 1536px+

- PWA-ready para módulo **Scholar** (check-in con cámara)

---

## 🧪 Testing

### Unit Tests (Vitest)

```bash
npm run test
```

### E2E Tests (Playwright)

```bash
npm run test:e2e
```

---

## 🚀 Deployment

### Vercel (Recomendado)

```bash
vercel --prod
```

### Docker

```bash
docker build -t scholarship-nextjs .
docker run -p 3000:3000 scholarship-nextjs
```

### Otras Plataformas

- **Netlify**
- **Railway**
- **Fly.io**

---

## ⚡ Performance

### Features de Next.js 15

- ✅ **Server Components** por defecto
- ✅ **Streaming SSR**
- ✅ **Parallel Routes**
- ✅ **Server Actions**
- ✅ **Image Optimization**
- ✅ **Font Optimization**

### Optimizaciones Aplicadas

- Lazy loading de componentes pesados
- Image optimization con `next/image`
- Caching estratégico con `revalidate`
- Code splitting automático por ruta

---

## 🔐 Seguridad

- CSRF protection en Server Actions
- Rate limiting en API routes
- Validación de inputs con Zod
- XSS protection (React por defecto)
- Secure headers (next.config.js)

---

## 📖 Recursos

- [Next.js Documentation](https://nextjs.org/docs)
- [App Router Guide](https://nextjs.org/docs/app)
- [shadcn/ui](https://ui.shadcn.com/)
- [Tailwind CSS](https://tailwindcss.com/docs)
- [React Hook Form](https://react-hook-form.com/)
- [Zod](https://zod.dev/)
