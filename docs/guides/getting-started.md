# Getting Started
## Work Scholarship System - Quick Start Guide

---

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

### Required

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

### Recommended

- [Visual Studio Code](https://code.visualstudio.com/) with extensions:
  - C# Dev Kit
  - Angular Language Service (for Angular)
  - ES7+ React/Redux/React-Native snippets (for Next.js)
  - Tailwind CSS IntelliSense
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/) (for .NET development)
- [PostgreSQL Client](https://www.pgadmin.org/) or use pgAdmin from Docker Compose

---

## 🚀 Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/work-scholarship-system.git
cd work-scholarship-system
```

### 2. Start Infrastructure Services

This will start PostgreSQL and Redis using Docker:

```bash
docker-compose up -d
```

Verify services are running:

```bash
docker ps
```

You should see:
- `scholarship-postgres` (Port 5432)
- `scholarship-redis` (Port 6379)
- `scholarship-pgadmin` (Port 5050) - Optional UI
- `scholarship-redis-commander` (Port 8081) - Optional UI

### 3. Setup Backend API

```bash
cd apps/api

# Restore NuGet packages
dotnet restore

# Create appsettings.Development.json
cp appsettings.json appsettings.Development.json
# Edit appsettings.Development.json with your settings
```

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=scholarship_db;Username=scholarship_user;Password=scholarship_dev_password"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-at-least-32-characters-long-for-security",
    "Issuer": "WorkScholarshipAPI",
    "Audience": "WorkScholarshipClient",
    "ExpirationMinutes": 1440
  }
}
```

**Apply database migrations:**
```bash
dotnet ef database update --project src/WorkScholarship.Infrastructure --startup-project src/WorkScholarship.WebAPI
```

**Run the API:**
```bash
dotnet run --project src/WorkScholarship.WebAPI
```

API will be available at: `https://localhost:7001`
Swagger UI: `https://localhost:7001/swagger`

### 4. Setup Frontend (Angular)

```bash
cd apps/web-angular

# Install dependencies
npm install

# Generate TypeScript types from API
npm run generate:types

# Start development server
npm run dev
```

Angular app will be available at: `http://localhost:4200`

### 5. Setup Frontend (Next.js)

```bash
cd apps/web-nextjs

# Install dependencies
npm install

# Create .env.local
echo "NEXT_PUBLIC_API_URL=https://localhost:7001/api" > .env.local

# Generate TypeScript types from API
npm run generate:types

# Start development server
npm run dev
```

Next.js app will be available at: `http://localhost:3000`

---

## 🔑 First Time Setup

### Create Admin User

You can create an admin user through the API or using the database seed script:

**Option 1: Using API (Swagger)**

1. Go to `https://localhost:7001/swagger`
2. Use the `/api/auth/register` endpoint with:
```json
{
  "email": "admin@university.edu",
  "password": "Admin123!",
  "firstName": "Admin",
  "lastName": "User",
  "role": "ADMIN"
}
```

**Option 2: Using Database Script**

```bash
cd tools/db-seeds
psql -h localhost -U scholarship_user -d scholarship_db -f create-admin-user.sql
```

### Test Login

1. Open Angular app: `http://localhost:4200`
2. Click "Login"
3. Enter admin credentials
4. You should be redirected to the admin dashboard

---

## 📚 Next Steps

Now that you have everything running:

1. **Explore the Documentation**
   - [Functional Requirements](../requirements/functional-requirements-es.md)
   - [Backend Architecture](../../apps/api/README.md)
   - [Development Workflow](./development-workflow.md)

2. **Understand the Domain**
   - Read about the 3 roles: Admin, Supervisor, Beca
   - Review the main processes: Selection, Renewal, Tracking

3. **Start Developing**
   - Pick a feature from the roadmap
   - Create a feature branch: `git checkout -b feature/my-feature`
   - Follow [Contributing Guidelines](../../CONTRIBUTING.md)

---

## 🐛 Troubleshooting

### Database Connection Issues

```bash
# Check if Postgres is running
docker ps | grep postgres

# Check database exists
docker exec -it scholarship-postgres psql -U scholarship_user -d scholarship_db -c "SELECT 1"

# View Postgres logs
docker logs scholarship-postgres
```

### API Not Starting

```bash
# Check for port conflicts
netstat -an | findstr :7001  # Windows
lsof -i :7001                # Mac/Linux

# Clean and rebuild
cd apps/api
dotnet clean
dotnet build
```

### Frontend Not Connecting to API

1. Verify API is running: `https://localhost:7001/swagger`
2. Check CORS settings in backend
3. Verify environment variables (`.env.local` for Next.js)
4. Clear browser cache and try again

### Type Generation Failing

```bash
# Ensure API is running first
# Then regenerate types
npm run generate:types

# If still failing, check OpenAPI spec
curl https://localhost:7001/swagger/v1/swagger.json
```

---

## 🆘 Getting Help

- Check [Troubleshooting Guide](./troubleshooting.md)
- Review [FAQ](./faq.md)
- Open an issue on GitHub
- Check existing discussions

---

## 🎉 You're Ready!

Congratulations! You now have a fully functional local development environment.

Happy coding! 🚀
