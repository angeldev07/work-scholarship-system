# Work Scholarship API
## Backend .NET con Clean Architecture

---

## 🏗️ Arquitectura

Este proyecto implementa **Clean Architecture** (también conocida como Onion Architecture o Hexagonal Architecture):

```
src/
├── WorkScholarship.Domain/           # Núcleo - Entidades, Value Objects, Reglas de Negocio
├── WorkScholarship.Application/      # Casos de Uso, CQRS (MediatR), DTOs
├── WorkScholarship.Infrastructure/   # Implementaciones (EF Core, Files, Email, etc.)
└── WorkScholarship.WebAPI/           # Controllers, Middleware, Configuración
```

### Principios Aplicados

- ✅ **Dependency Inversion** - Dependencias apuntan hacia adentro
- ✅ **CQRS** con MediatR - Separación de Commands y Queries
- ✅ **Repository Pattern** - Abstracción de acceso a datos
- ✅ **Separation of Concerns** - Cada capa tiene responsabilidad única

---

## 🚀 Quick Start

### Prerequisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para Postgres y Redis)

### 1. Restaurar Dependencias

```bash
dotnet restore
```

### 2. Levantar Base de Datos

Desde el root del monorepo:

```bash
docker-compose up -d postgres redis
```

### 3. Aplicar Migraciones

```bash
dotnet ef database update --project src/WorkScholarship.Infrastructure --startup-project src/WorkScholarship.WebAPI
```

### 4. Ejecutar API

```bash
dotnet run --project src/WorkScholarship.WebAPI
```

API estará en: `https://localhost:7001`

---

## 📚 Documentación

- **Swagger UI:** `https://localhost:7001/swagger`
- **OpenAPI Spec:** `https://localhost:7001/swagger/v1/swagger.json`

---

## 🧪 Testing

```bash
# Todos los tests
dotnet test

# Solo tests unitarios
dotnet test tests/WorkScholarship.Domain.Tests
dotnet test tests/WorkScholarship.Application.Tests

# Tests de integración
dotnet test tests/WorkScholarship.Integration.Tests
```

---

## 📦 Estructura de Carpetas

```
src/
├── WorkScholarship.Domain/
│   ├── Entities/              # Entidades del dominio (User, BecaTrabajo, etc.)
│   ├── Enums/                 # Enumeraciones (UserRole, SelectionState, etc.)
│   ├── ValueObjects/          # Value Objects (Email, ScheduleSlot, etc.)
│   ├── Exceptions/            # Excepciones del dominio
│   └── Common/                # Interfaces base, BaseEntity
│
├── WorkScholarship.Application/
│   ├── Common/
│   │   ├── Interfaces/        # IApplicationDbContext, IFileStorageService, etc.
│   │   ├── Models/            # Result<T>, PaginatedList<T>
│   │   └── Behaviors/         # ValidationBehavior, LoggingBehavior (MediatR)
│   ├── Features/              # Feature folders (CQRS)
│   │   ├── Auth/
│   │   │   ├── Commands/      # RegisterCommand, LoginCommand
│   │   │   └── Queries/       # GetCurrentUserQuery
│   │   ├── Selections/
│   │   ├── Scholarships/
│   │   └── Locations/
│   └── DependencyInjection.cs
│
├── WorkScholarship.Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/    # EF Core Fluent API configurations
│   │   └── Migrations/
│   ├── Identity/              # CurrentUserService
│   ├── Services/              # Implementaciones de servicios
│   │   ├── FileStorageService.cs
│   │   ├── EmailService.cs
│   │   ├── ExcelParserService.cs
│   │   └── PdfParserService.cs
│   └── DependencyInjection.cs
│
└── WorkScholarship.WebAPI/
    ├── Controllers/           # API Controllers
    ├── Middleware/            # ExceptionHandlingMiddleware, etc.
    ├── Filters/               # ApiResponseFilter
    ├── appsettings.json
    └── Program.cs
```

---

## 🔧 Tecnologías

- **.NET 9** (LTS)
- **EF Core 9** con PostgreSQL
- **MediatR** (CQRS pattern)
- **FluentValidation** (validación de requests)
- **AutoMapper** (mappings)
- **Serilog** (logging estructurado)
- **JWT Authentication**
- **Google OAuth 2.0**
- **Hangfire** (background jobs)
- **EPPlus** (Excel processing)
- **QuestPDF** (PDF generation)

---

## 🌐 Endpoints Principales

| Módulo | Base Path | Descripción |
|--------|-----------|-------------|
| Auth | `/api/auth` | Login, register, OAuth |
| Selections | `/api/selections` | Gestión de procesos de selección |
| Scholarships | `/api/scholarships` | Gestión de becas trabajo |
| Locations | `/api/locations` | Gestión de ubicaciones |
| Tracking | `/api/tracking` | Check-in/out, jornadas |
| Reports | `/api/reports` | Reportes y métricas |

---

## 🔑 Variables de Entorno

Crear archivo `appsettings.Development.json` (no commitear):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=scholarship_db;Username=scholarship_user;Password=scholarship_dev_password"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-here-min-32-chars",
    "Issuer": "WorkScholarshipAPI",
    "Audience": "WorkScholarshipClient",
    "ExpirationMinutes": 1440
  },
  "GoogleOAuth": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "EmailSettings": {
    "Provider": "SendGrid",
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@scholarship.local",
    "FromName": "Work Scholarship System"
  },
  "StorageSettings": {
    "Provider": "Local",
    "LocalPath": "C:\\uploads"
  }
}
```

---

## 📝 Migraciones

### Crear nueva migración

```bash
dotnet ef migrations add MigrationName --project src/WorkScholarship.Infrastructure --startup-project src/WorkScholarship.WebAPI
```

### Aplicar migraciones

```bash
dotnet ef database update --project src/WorkScholarship.Infrastructure --startup-project src/WorkScholarship.WebAPI
```

### Revertir migración

```bash
dotnet ef database update PreviousMigrationName --project src/WorkScholarship.Infrastructure --startup-project src/WorkScholarship.WebAPI
```

---

## 🐛 Debugging

### VS Code

Ya hay configuración en `.vscode/launch.json`

### Visual Studio / Rider

Abrir `WorkScholarship.sln` y presionar F5

---

## 📖 Recursos

- [Clean Architecture by Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
