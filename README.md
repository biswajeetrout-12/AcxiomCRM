# AcxiomCRM — Role-Based Customer Relationship Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-purple)](https://learn.microsoft.com/en-us/aspnet/core/mvc/)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-8.0-green)](https://docs.microsoft.com/en-us/ef/core/)

## Overview

**AcxiomCRM** is a complete, role-based Customer Relationship Management web application built on **ASP.NET Core 8 MVC** with **Entity Framework Core** and **SQL Server LocalDB**.

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Web Framework | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 |
| Database | SQL Server Express / LocalDB |
| Identity | ASP.NET Core Identity |
| Frontend | Bootstrap 5.3, Bootstrap Icons, Chart.js 4.4 |
| Testing | xUnit, Moq, EF Core In-Memory |

---

## Demo Credentials (Development Only)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@acxiomcrm.com | Admin@123 |
| Manager | manager@acxiomcrm.com | Manager@123 |
| Sales Executive | sales@acxiomcrm.com | Sales@123 |

> Security: Demo credentials only seeded in Development. Never plain-text in DB - uses Identity UserManager hashing.

---

## Implemented Modules (Section 17.2)

- [x] Authentication & Authorization (Login, Register, Logout, Role-based Access Control)
- [x] Dashboard (KPI cards, Pipeline chart, Lead Status chart, Monthly Revenue trend)
- [x] Customer Management (CRUD, Search, Pagination, Role-filtered)
- [x] Lead Management (CRUD, Assign, Convert-to-Customer)
- [x] Follow-Up Management (CRUD, Mark-Complete, Overdue tracking)
- [x] Activity Management (CRUD, Type/status filters)
- [x] Opportunity Management (CRUD, Pipeline stage, Probability)
- [x] User & Role Management (Create, assign roles, activate/deactivate - Admin only)
- [x] Audit Log (System-wide tracking with old/new values - Admin only)
- [x] REST APIs (Customers, Leads, Opportunities, Follow-Ups, Auth, Reports)
- [x] Reports & Analytics (Pipeline, Conversion, Activity, Customer Portfolio, Follow-Up + CSV Export)

---

## Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server Express or LocalDB

### Setup

1. Configure database in appsettings.json:
   Server=(localdb)\mssqllocaldb;Database=AcxiomCRM_Dev;Trusted_Connection=True

2. Run the application:
   dotnet run --project AcxiomCRM/AcxiomCRM.csproj --urls http://localhost:5050

3. Open http://localhost:5050 and log in with demo credentials above.

Migrations are applied automatically at startup. Database and demo data are seeded on first run.

---

## Running Tests

Stop the running application first, then:
   dotnet test AcxiomCRM.Tests/AcxiomCRM.Tests.csproj -v normal

Tests use EF Core In-Memory - no live database required.

---

## Security Features

- ASP.NET Core Identity password hashing (PBKDF2/SHA-256)
- Account Lockout: 5 failed attempts = 15-minute lockout
- Anti-Forgery tokens on all state-changing forms
- Role-based authorization on all controllers
- Audit logging: every CRUD operation captured with user, IP, timestamp, old/new values
- HttpOnly cookies with 8-hour sliding expiration
- API returns 401/403 (not redirect) for unauthenticated API calls

---
