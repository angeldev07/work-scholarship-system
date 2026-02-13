# Functional Requirements Document
## Work Scholarship Management and Tracking System

**Project:** Work Scholarship Management System
**Version:** 2.0 - Complete
**Date:** 2026-02-13

---

## 📋 Overview

This document is the **English version** of the functional requirements.

For the complete and detailed version in Spanish, please refer to:
**[REQUIREMENTS_COMPLETE.md](./REQUIREMENTS_COMPLETE.md)**

---

## Quick Summary

### System Purpose
Comprehensive system to manage the complete cycle of university work scholarships, from application and selection to daily hour tracking with photographic evidence.

### Scope
- **Semester cycle management**
- **Complete selection process** (application, interview, automatic matching)
- **Automatic renewal** for previous scholars with good performance
- **Real-time hour tracking** with mandatory photographic evidence
- **Absence management**
- **Official documentation generation** (logs, badges)

---

## System Roles

| Role | Description |
|------|-------------|
| **ADMIN** | Library/department administrator with full system control |
| **SUPERVISOR** | Zone manager responsible for approving work sessions |
| **BECA** | Scholarship student who performs work duties |

---

## Main Subsystems (10)

1. **AUTH** - Authentication & Authorization
2. **CICLO** - Cycle/Semester Management
3. **SEL** - Selection Process
4. **UBIC** - Location Management
5. **TRACK** - Hour Tracking System ⭐
6. **AUS** - Absence Management
7. **DOC** - Document Generation
8. **NOTIF** - Notification System
9. **REP** - Reports & Queries
10. **HIST** - History & Audit

---

## Total Requirements

- **54 Functional Requirements** (RF-001 to RF-054)
- **15 Business Rules**
- **8 Non-Functional Requirements**

### Priority Distribution

- 🔴 **High Priority:** 26 RFs (48%) - MVP and Core
- 🟡 **Medium Priority:** 22 RFs (41%) - Improvements
- 🟢 **Low Priority:** 6 RFs (11%) - Refinement

---

## Key Features

### ✨ Selection Process
- Excel upload with applicants
- Automatic user creation
- Schedule PDF validation
- Automatic location matching by schedule compatibility
- Interview management
- Final assignment

### ⭐ Renewal with Priority
Scholars from previous semesters with good performance:
- Have priority in next cycle
- Only need to upload updated schedule (no full application process)
- Automatic assignment if compatible (≥70%)

**Eligibility:** ≥90% attendance + ≥95% hours completed

### ⏱️ Hour Tracking
- **Check-in:** Scholar takes selfie when starting shift
- **Check-out:** Scholar takes photo when ending shift
- Photographic evidence is **mandatory** (prevents fraud)
- Supervisor reviews and approves sessions
- Only approved sessions count towards total hours

### 📊 Absence Management
- Report absences in advance or justify emergencies
- Medical absences require documentation
- Maximum 2 unjustified absences per month
- Automatic alerts if ≥3 unjustified absences

### 📄 Official Documents
- **Badges** (printable ID cards)
- **Official logs** (required by university to validate hours)

---

## Implementation Phases

### Phase 1 - MVP (Weeks 1-6)
- Authentication (JWT + OAuth)
- Cycle management
- Basic selection process
- Location management

### Phase 2 - Core (Weeks 7-12)
- **Hour tracking system** (check-in/out with photos)
- Absence management
- Document generation
- Email notifications

### Phase 3 - Improvements (Weeks 13-16)
- Renewal process
- Dashboards and reports
- In-app notifications
- History and audit

---

## Technology Stack

### Backend
- **.NET 9** with Clean Architecture
- **EF Core** + **PostgreSQL**
- **MediatR** (CQRS)
- **JWT** + **OAuth 2.0**

### Frontend
- **Angular 19** (first implementation)
- **Next.js 15** (second implementation)

---

## Complete Documentation

For detailed requirements including:
- All 54 functional requirements with acceptance criteria
- Business rules
- Data model
- Process flows
- Technical specifications

**Please refer to:** [REQUIREMENTS_COMPLETE.md](./REQUIREMENTS_COMPLETE.md)

---

**Note:** This is a working document that may be updated during development.
