# Project Structure
## Work Scholarship System - Complete Folder Tree

---

```
work-scholarship-system/
│
├── apps/                                      # Applications
│   ├── api/                                   # Backend .NET
│   │   ├── src/                               # Source code
│   │   ├── tests/                             # Tests
│   │   ├── docs/                              # API-specific docs
│   │   └── README.md                          # ✅ Created
│   │
│   ├── web-angular/                           # Frontend Angular 19
│   │   ├── src/                               # Source code
│   │   ├── generated/                         # Generated types from OpenAPI
│   │   ├── docs/                              # Angular-specific docs
│   │   └── README.md                          # ✅ Created
│   │
│   └── web-nextjs/                            # Frontend Next.js 15
│       ├── src/                               # Source code
│       ├── generated/                         # Generated types from OpenAPI
│       ├── docs/                              # Next.js-specific docs
│       └── README.md                          # ✅ Created
│
├── docs/                                      # Documentation
│   ├── requirements/                          # Requirements documents
│   │   ├── REQUIREMENTS_COMPLETE.md           # ✅ Moved (54 RFs - Spanish)
│   │   └── functional-requirements-en.md      # ✅ Created (English summary)
│   │
│   ├── architecture/                          # Architecture docs
│   │   ├── decisions/                         # ADRs (Architecture Decision Records)
│   │   └── diagrams/                          # Architecture diagrams
│   │
│   ├── api/                                   # API documentation
│   │
│   └── guides/                                # User guides
│       └── getting-started.md                 # ✅ Created
│
├── tools/                                     # Scripts and utilities
│   ├── scripts/                               # Shell scripts
│   └── db-seeds/                              # Database seed files
│
├── .claude/                                   # Claude Code context (gitignored)
│   └── CLAUDE.md                              # ✅ Created - Main context
│
├── .github/                                   # GitHub configuration
│   ├── workflows/                             # CI/CD workflows
│   └── ISSUE_TEMPLATE/                        # Issue templates
│
├── .vscode/                                   # VS Code configuration
│
├── docker-compose.yml                         # ✅ Created - Dev services (Postgres, Redis)
├── .gitignore                                 # ✅ Created - Complete gitignore
├── README.md                                  # ✅ Created - Main README
├── CONTRIBUTING.md                            # ✅ Created - Contribution guidelines
├── LICENSE                                    # ✅ Created - MIT License
└── PROJECT_STRUCTURE.md                       # This file
```

---

## ✅ Files Created

### Root Level
- [x] `README.md` - Main project overview and quick start
- [x] `.gitignore` - Complete ignore rules (.NET + Node + Docker + Claude)
- [x] `docker-compose.yml` - PostgreSQL + Redis + pgAdmin + Redis Commander
- [x] `CONTRIBUTING.md` - Contribution guidelines and conventions
- [x] `LICENSE` - MIT License
- [x] `PROJECT_STRUCTURE.md` - This file

### Documentation
- [x] `docs/requirements/REQUIREMENTS_COMPLETE.md` - 54 functional requirements (Spanish) - MOVED
- [x] `docs/requirements/functional-requirements-en.md` - English version summary
- [x] `docs/guides/getting-started.md` - Quick start guide

### Backend (API)
- [x] `apps/api/README.md` - Clean Architecture details, commands, conventions

### Frontend (Angular)
- [x] `apps/web-angular/README.md` - Angular setup, structure, PrimeNG

### Frontend (Next.js)
- [x] `apps/web-nextjs/README.md` - Next.js setup, App Router, shadcn/ui

### Claude Context
- [x] `.claude/CLAUDE.md` - Complete project context for AI assistance

---

## 📦 What's Next?

### Immediate Next Steps

1. **Backend Setup:**
   ```bash
   cd apps/api
   dotnet new sln -n WorkScholarship
   # Create projects for Domain, Application, Infrastructure, WebAPI
   ```

2. **Frontend Setup:**
   ```bash
   cd apps/web-angular
   ng new . --routing --style=scss
   npm install primeng primeicons primeflex

   cd apps/web-nextjs
   npx create-next-app@latest . --typescript --tailwind --app
   npx shadcn-ui@latest init
   ```

3. **Initialize Git:**
   ```bash
   git init
   git add .
   git commit -m "feat: initial project structure with Clean Architecture"
   ```

---

## 🏗️ Folder Purpose Guide

| Folder | Purpose | Tracked in Git |
|--------|---------|----------------|
| `apps/api/src/` | Backend source code (.NET Clean Architecture) | ✅ Yes |
| `apps/api/tests/` | Backend unit and integration tests | ✅ Yes |
| `apps/web-angular/src/` | Angular application code | ✅ Yes |
| `apps/web-angular/generated/` | Auto-generated types from OpenAPI | ❌ No (gitignored) |
| `apps/web-nextjs/src/` | Next.js application code | ✅ Yes |
| `apps/web-nextjs/generated/` | Auto-generated types from OpenAPI | ❌ No (gitignored) |
| `docs/` | All project documentation | ✅ Yes |
| `tools/scripts/` | Helper scripts (setup, deploy, etc.) | ✅ Yes |
| `tools/db-seeds/` | Database seed files (.sql) | ✅ Yes |
| `.claude/` | AI context files | ❌ No (gitignored) |
| `.github/workflows/` | CI/CD pipelines (GitHub Actions) | ✅ Yes |
| `.vscode/` | VS Code workspace settings | ✅ Yes (partial) |

---

## 🎯 Key Principles

1. **No Shared Code Between Apps**
   - Each app (API, Angular, Next.js) is completely independent
   - Frontends generate their own types from backend OpenAPI spec
   - No `shared/` or `packages/` folders

2. **Clean Architecture in Backend**
   - Domain → Application → Infrastructure → WebAPI
   - Dependencies point inward
   - Feature folders, not technical folders

3. **Complete Frontend Implementations**
   - Angular and Next.js both implement ALL features
   - Not split by user role (Admin/Supervisor/Scholar)
   - Two full implementations for learning purposes

4. **Documentation First**
   - Requirements documented before coding
   - ADRs (Architecture Decision Records) for important decisions
   - Each app has its own README

---

## 📝 Notes

- All folders exist but may be empty (placeholders for future content)
- `generated/` folders will be created when running type generation scripts
- `.claude/` is gitignored but locally important for AI-assisted development
- Docker Compose provides all infrastructure for local development

---

**Created:** 2026-02-13
**Status:** ✅ Complete - Ready for development
**Next Step:** Initialize .NET solution and projects
