# Contributing to Work Scholarship System

¡Gracias por tu interés en contribuir al proyecto! 🎉

---

## 📋 Tabla de Contenidos

1. [Código de Conducta](#código-de-conducta)
2. [¿Cómo Puedo Contribuir?](#cómo-puedo-contribuir)
3. [Guía de Desarrollo](#guía-de-desarrollo)
4. [Convenciones de Código](#convenciones-de-código)
5. [Proceso de Pull Request](#proceso-de-pull-request)
6. [Reporte de Bugs](#reporte-de-bugs)
7. [Solicitud de Features](#solicitud-de-features)

---

## Código de Conducta

Este proyecto adhiere a un código de conducta. Al participar, te comprometes a mantener un ambiente respetuoso y acogedor.

---

## ¿Cómo Puedo Contribuir?

### 🐛 Reportar Bugs

1. Verifica que el bug no haya sido reportado previamente en [Issues](../../issues)
2. Si es nuevo, crea un issue usando el template de bug report
3. Incluye:
   - Descripción clara del problema
   - Pasos para reproducir
   - Comportamiento esperado vs actual
   - Screenshots si aplica
   - Información del entorno (OS, versión de .NET/Node, etc.)

### ✨ Proponer Features

1. Revisa los [Requerimientos Funcionales](docs/requirements/functional-requirements-es.md)
2. Crea un issue usando el template de feature request
3. Describe claramente:
   - El problema que resuelve
   - La solución propuesta
   - Alternativas consideradas

### 💻 Contribuir Código

1. Fork el repositorio
2. Crea una branch desde `main`: `git checkout -b feature/my-feature`
3. Realiza tus cambios
4. Ejecuta tests y linters
5. Commit siguiendo [Conventional Commits](#conventional-commits)
6. Push a tu fork
7. Abre un Pull Request

---

## Guía de Desarrollo

### Setup Inicial

```bash
# Clonar el repo
git clone https://github.com/tu-usuario/work-scholarship-system.git
cd work-scholarship-system

# Levantar servicios
docker-compose up -d

# Backend
cd apps/api
dotnet restore
dotnet ef database update --project src/WorkScholarship.Infrastructure
dotnet run --project src/WorkScholarship.WebAPI

# Frontend Angular
cd apps/web-angular
npm install
npm run dev

# Frontend Next.js
cd apps/web-nextjs
npm install
npm run dev
```

### Estructura de Branches

- `main` - Rama principal (protegida)
- `develop` - Rama de desarrollo (opcional)
- `feature/nombre` - Nuevas features
- `fix/nombre` - Bug fixes
- `docs/nombre` - Solo documentación
- `refactor/nombre` - Refactorización
- `test/nombre` - Agregar tests

---

## Convenciones de Código

### Backend (.NET)

#### Naming Conventions

```csharp
// PascalCase para clases, métodos, propiedades
public class BecaTrabajo { }
public void ProcessApplication() { }
public string FirstName { get; set; }

// camelCase para variables locales y parámetros
var userId = 123;
public void GetUser(int userId) { }

// _camelCase para campos privados
private readonly ILogger _logger;

// UPPER_CASE para constantes
public const string DEFAULT_ROLE = "BECA";
```

#### Clean Architecture

- **Domain:** Solo entidades, value objects, enums - sin dependencias externas
- **Application:** Casos de uso con MediatR - depende solo de Domain
- **Infrastructure:** Implementaciones - depende de Domain y Application
- **WebAPI:** Controllers - depende de Application

#### CQRS Pattern

```csharp
// Commands (modifican estado)
public class CreateCycleCommand : IRequest<Result<Guid>>
{
    public string Name { get; set; }
}

// Queries (solo lectura)
public class GetCycleByIdQuery : IRequest<CycleDto>
{
    public Guid Id { get; set; }
}
```

### Frontend (Angular/Next.js)

#### TypeScript

```typescript
// PascalCase para interfaces, types, classes
interface User { }
type UserRole = 'ADMIN' | 'SUPERVISOR' | 'BECA';
class UserService { }

// camelCase para variables, funciones
const userId = '123';
function getUser() { }

// UPPER_SNAKE_CASE para constantes
const API_BASE_URL = 'https://api.example.com';
```

#### Component Structure (Angular)

```
feature-name/
├── components/
│   ├── feature-list/
│   │   ├── feature-list.component.ts
│   │   ├── feature-list.component.html
│   │   ├── feature-list.component.scss
│   │   └── feature-list.component.spec.ts
│   └── ...
├── services/
├── models/
└── feature.routes.ts
```

---

## Proceso de Pull Request

### Checklist

Antes de abrir un PR, asegúrate de:

- [ ] El código compila sin errores
- [ ] Todos los tests pasan
- [ ] Linters pasan sin errores
- [ ] Documentación actualizada (si aplica)
- [ ] Commits siguen Conventional Commits
- [ ] PR tiene descripción clara

### Template de PR

```markdown
## Descripción
[Descripción breve de los cambios]

## Tipo de Cambio
- [ ] Bug fix (cambio que soluciona un issue)
- [ ] Nueva feature (cambio que agrega funcionalidad)
- [ ] Breaking change (fix o feature que causa que funcionalidad existente no funcione como antes)
- [ ] Documentación

## ¿Cómo se ha probado?
[Describe las pruebas realizadas]

## Checklist
- [ ] Mi código sigue el style guide del proyecto
- [ ] He realizado self-review de mi código
- [ ] He comentado mi código donde es necesario
- [ ] He actualizado la documentación
- [ ] Mis cambios no generan nuevos warnings
- [ ] He agregado tests
- [ ] Tests nuevos y existentes pasan localmente
```

### Review Process

1. Al menos 1 reviewer debe aprobar
2. Todos los comentarios deben ser resueltos
3. CI/CD debe pasar (cuando esté configurado)
4. Squash merge a `main`

---

## Conventional Commits

### Formato

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: Nueva feature
- `fix`: Bug fix
- `docs`: Solo documentación
- `style`: Cambios de formato (no afectan código)
- `refactor`: Refactorización
- `test`: Agregar o modificar tests
- `chore`: Cambios en build, CI, dependencias

### Ejemplos

```bash
feat(auth): add Google OAuth login

Implemented Google OAuth 2.0 authentication flow.
Users can now login using their Google account.

Closes #123

---

fix(tracking): resolve check-out timestamp issue

Fixed bug where check-out timestamp was not being
saved correctly when user forgot to check-out.

Fixes #456

---

docs(api): update README with new endpoints

Added documentation for new tracking endpoints.

---

refactor(selection): extract matching algorithm to service

Moved schedule matching logic from controller to
dedicated service for better testability.
```

---

## Testing

### Backend Tests

```bash
# Todos los tests
dotnet test

# Con coverage
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

```bash
# Angular
npm run test
npm run test:coverage

# Next.js
npm run test
npm run test:coverage
```

### Coverage Mínimo

- **Backend:** 70%
- **Frontend:** 60%

---

## Documentación

### ¿Cuándo Documentar?

- Nuevas features
- Cambios en API
- Cambios arquitectónicos
- Decisiones técnicas importantes

### Dónde Documentar

- **README.md:** Info general y quick start
- **docs/guides/:** Guías de uso
- **docs/architecture/:** Decisiones arquitectónicas (ADRs)
- **docs/api/:** Documentación de API
- **Código:** Comentarios XML/JSDoc para métodos públicos

---

## Estándares de Calidad

### Code Review Checklist

- [ ] El código es legible y está bien estructurado
- [ ] Las variables y funciones tienen nombres descriptivos
- [ ] No hay código duplicado (DRY)
- [ ] Funciones tienen responsabilidad única (SRP)
- [ ] Manejo adecuado de errores
- [ ] No hay hardcoded values (usar configuración)
- [ ] Tests cubren casos edge
- [ ] Documentación clara

---

## Ayuda Adicional

¿Necesitas ayuda? Puedes:

- Revisar la [documentación completa](docs/)
- Abrir un [Discussion](../../discussions)
- Contactar a los maintainers

---

¡Gracias por contribuir! 🚀
