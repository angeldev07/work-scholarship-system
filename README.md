# Work Scholarship Management System
## Sistema de Gestión y Seguimiento de Becas Trabajo

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular)](https://angular.dev/)
[![Next.js](https://img.shields.io/badge/Next.js-15-000000?logo=next.js)](https://nextjs.org/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)

---

## 📋 Descripción

Sistema integral para gestionar el ciclo completo de becas trabajo universitarias en bibliotecas y otras dependencias, desde la postulación y selección hasta el seguimiento diario de horas trabajadas con evidencia fotográfica.

### Características Principales

- 🎓 **Gestión de Ciclos Semestrales** - Configuración y administración de procesos por semestre
- 📝 **Proceso de Selección Completo** - Postulación, entrevistas, matching automático por horarios
- 🔄 **Renovación con Prioridad** - Becas anteriores con buen desempeño renuevan automáticamente
- ⏱️ **Tracking de Horas con Evidencia** - Check-in/out con foto obligatoria, aprobación de supervisores
- 📊 **Dashboards por Rol** - Vistas específicas para Admin, Supervisor y Beca
- 📄 **Documentación Oficial** - Generación de bitácoras y escarapelas

---

## 🏗️ Arquitectura

### Monorepo Structure

```
work-scholarship-system/
├── apps/
│   ├── api/              # Backend .NET con Clean Architecture
│   ├── web-angular/      # Frontend completo en Angular 19
│   └── web-nextjs/       # Frontend completo en Next.js 15
├── docs/                 # Documentación del proyecto
├── tools/                # Scripts y utilidades
└── .claude/              # Contexto para IA (gitignored)
```

### Stack Tecnológico

#### Backend
- **.NET 9** (LTS)
- **Clean Architecture** (Domain, Application, Infrastructure, WebAPI)
- **EF Core** con **PostgreSQL**
- **MediatR** (CQRS pattern)
- **FluentValidation**
- **JWT + OAuth 2.0** (Google)
- **Serilog** (logging estructurado)
- **Hangfire** (background jobs)

#### Frontend
- **Angular 19** (primera implementación)
- **Next.js 15** con App Router (segunda implementación)
- Ambos implementan **todas las features** (Admin, Supervisor, Beca)

#### Infraestructura
- **PostgreSQL 16**
- **Redis** (caché)
- **Docker** & **Docker Compose**
- **AWS S3** / **Cloudflare R2** (file storage)

---

## 🚀 Quick Start

### Prerequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

### 1. Clonar Repositorio

```bash
git clone https://github.com/tu-usuario/work-scholarship-system.git
cd work-scholarship-system
```

### 2. Levantar Servicios (Postgres + Redis)

```bash
docker-compose up -d
```

### 3. Backend (.NET)

```bash
cd apps/api
dotnet restore
dotnet ef database update --project src/WorkScholarship.Infrastructure
dotnet run --project src/WorkScholarship.WebAPI
```

Backend estará en: `https://localhost:7001`

### 4. Frontend Angular

```bash
cd apps/web-angular
npm install
npm run dev
```

Frontend estará en: `http://localhost:4200`

### 5. Frontend Next.js

```bash
cd apps/web-nextjs
npm install
npm run dev
```

Frontend estará en: `http://localhost:3000`

---

## 📚 Documentación

- [**Requerimientos Funcionales (ES)**](docs/requirements/REQUIREMENTS_COMPLETE.md) - 54 RFs organizados en 10 subsistemas
- [**Requerimientos Funcionales (EN)**](docs/requirements/functional-requirements-en.md) - Functional requirements (English)
- [**Guía de Desarrollo**](docs/guides/development-workflow.md) - Workflow y convenciones
- [**Arquitectura Backend**](apps/api/docs/clean-architecture.md) - Clean Architecture en .NET
- [**API Docs**](docs/api/openapi.yaml) - OpenAPI/Swagger specification

---

## 👥 Roles del Sistema

| Rol | Descripción | Features Principales |
|-----|-------------|---------------------|
| **ADMIN** | Administrador de biblioteca | Gestión de ciclos, ubicaciones, selección, reportes |
| **SUPERVISOR** | Encargado de zona | Aprobar jornadas, gestionar ausencias, supervisar becas |
| **BECA** | Estudiante becado | Check-in/out, reportar ausencias, consultar horas |

---

## 🎯 Roadmap

### ✅ Fase 1 - MVP (Semanas 1-6)
- [x] Setup del proyecto y arquitectura
- [ ] Autenticación (JWT + OAuth)
- [ ] Gestión de ciclos
- [ ] Proceso de selección básico
- [ ] Gestión de ubicaciones

### 🚧 Fase 2 - Core (Semanas 7-12)
- [ ] Sistema de tracking de horas
- [ ] Gestión de ausencias
- [ ] Generación de documentos
- [ ] Notificaciones por email

### 📋 Fase 3 - Mejoras (Semanas 13-16)
- [ ] Proceso de renovación
- [ ] Dashboards y reportes
- [ ] Notificaciones in-app
- [ ] Historial y auditoría

---

## 🤝 Contribuir

Este proyecto sigue convenciones estrictas:

1. **Clean Architecture** en backend
2. **Feature-based structure** en frontends
3. **Conventional Commits** (feat, fix, docs, etc.)
4. **PR con review obligatorio**

Ver [CONTRIBUTING.md](CONTRIBUTING.md) para más detalles.

---

## 📄 Licencia

Este proyecto es de código abierto bajo licencia [MIT](LICENSE).

---

## 👤 Autor

**Angel** - [GitHub](https://github.com/tu-usuario)

---

## 🙏 Agradecimientos

- Biblioteca Universidad (caso de uso original)
- Comunidad de .NET y Angular/Next.js
- Claude Code por asistencia en desarrollo

---

**⭐ Si este proyecto te ayuda, considera darle una estrella en GitHub!**
