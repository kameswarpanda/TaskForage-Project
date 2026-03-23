# 🔨 TaskForge — Enterprise Task & Project Management API

A production-level **.NET 8 Web API** demonstrating real-world backend engineering concepts: Clean Architecture, JWT + Basic Authentication, SQL Server with advanced queries, EF Core + ADO.NET, multithreading, caching, and more.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      TaskForge.API                          │
│  Controllers │ Middleware │ Program.cs │ Swagger             │
├─────────────────────────────────────────────────────────────┤
│                   TaskForge.Application                     │
│  DTOs │ Service Interfaces │ Pagination │ Response Models    │
├─────────────────────────────────────────────────────────────┤
│                   TaskForge.Infrastructure                  │
│  EF Core │ Repositories │ Services │ Auth │ Background Jobs │
├─────────────────────────────────────────────────────────────┤
│                     TaskForge.Domain                        │
│  Entities │ Enums │ Interfaces (IRepository, IUnitOfWork)   │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Flow:** API → Application → Infrastructure → Domain

---

## ✅ Features Matrix

| Category | Details |
|---|---|
| **Architecture** | Clean Architecture (Controller → Service → Repository), DI, UnitOfWork |
| **Auth** | JWT (access + refresh tokens), Basic Auth, Role-based (Admin/User) |
| **Database** | SQL Server, EF Core Code-First, ADO.NET for Stored Procedures |
| **SQL** | Stored Procedures, JOINs, GROUP BY, CTEs, Window Functions, Temp Tables, Triggers, Indexes |
| **Caching** | In-Memory (`IMemoryCache`) with configurable TTL |
| **Logging** | Serilog (Console + Rolling File), Request/Response logging middleware |
| **Error Handling** | Global exception middleware → ProblemDetails responses |
| **API Versioning** | URL-segment based (`/api/v1/...`) via `Asp.Versioning` |
| **Swagger** | OpenAPI docs with JWT Bearer auth support |
| **Multithreading** | `async/await`, `Parallel.ForEachAsync`, `SemaphoreSlim`, `ConcurrentQueue` |
| **Background Tasks** | `BackgroundService` for audit log processing & task notifications |
| **Optimization** | Covering indexes, composite indexes, connection pooling, query caching |

---

## 🚀 Setup Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express / LocalDB)

### Step 1: Setup Database
Run the SQL scripts **in order** against your SQL Server:

```sql
-- Open SSMS or Azure Data Studio and execute:
1. sql/01_CreateDatabase.sql    -- Creates database and tables
2. sql/02_StoredProcedures.sql  -- Creates all stored procedures
3. sql/03_Triggers.sql          -- Creates audit logging triggers
4. sql/04_Indexes.sql           -- Creates optimized indexes
5. sql/05_SeedData.sql          -- Seeds roles, users, and sample data
```

### Step 2: Configure Connection String
Update `src/TaskForge.API/appsettings.json` with your SQL Server connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TaskForgeDB;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Step 3: Build & Run

```bash
cd TaskForge
dotnet restore
dotnet build
cd src/TaskForge.API
dotnet run
```

### Step 4: Access the API
- **Swagger UI:** https://localhost:7201
- **API Base:** https://localhost:7201/api/v1

---

## 🔐 Authentication Flow

### JWT Authentication
```
1. POST /api/v1/auth/register   → Register user, get tokens
2. POST /api/v1/auth/login      → Login, get access + refresh tokens
3. Use access token:            Authorization: Bearer <token>
4. POST /api/v1/auth/refresh    → Refresh expired access token
5. POST /api/v1/auth/revoke     → Revoke refresh token (logout)
```

### Basic Authentication
```
Authorization: Basic base64(username:password)
```

---

## 📋 API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | Public | Register new user |
| POST | `/auth/login` | Public | Login, get JWT tokens |
| POST | `/auth/refresh` | Public | Refresh token |
| POST | `/auth/revoke` | JWT | Revoke refresh token |
| GET | `/users` | JWT | Get all users (paginated) |
| GET | `/users/{id}` | JWT | Get user by ID |
| POST | `/users` | Admin | Create user |
| PUT | `/users/{id}` | JWT | Update user |
| DELETE | `/users/{id}` | Admin | Delete user |
| POST | `/users/{id}/roles/{roleId}` | Admin | Assign role |
| DELETE | `/users/{id}/roles/{roleId}` | Admin | Remove role |
| GET | `/roles` | JWT | Get all roles |
| POST | `/roles` | Admin | Create role |
| DELETE | `/roles/{id}` | Admin | Delete role |
| GET | `/projects` | JWT | Get all projects (paginated) |
| POST | `/projects` | JWT | Create project |
| PUT | `/projects/{id}` | JWT | Update project |
| DELETE | `/projects/{id}` | Admin | Delete project |
| GET | `/tasks` | JWT | Get all tasks (filter, paginate) |
| GET | `/tasks/{id}` | JWT | Get task by ID |
| GET | `/tasks/user/{userId}` | JWT | Get tasks by assignee |
| POST | `/tasks` | JWT | Create task |
| PUT | `/tasks/{id}` | JWT | Update task |
| DELETE | `/tasks/{id}` | Admin | Delete task |
| PATCH | `/tasks/bulk-update-status` | Admin | Bulk update statuses |
| GET | `/reports/task-summary` | JWT | Task summary stats |
| GET | `/reports/project-tasks` | JWT | Per-project reports |
| GET | `/reports/user-productivity` | Admin | User productivity rankings |

---

## 📁 Project Structure

```
TaskForge/
├── TaskForge.sln
├── README.md
├── src/
│   ├── TaskForge.Domain/
│   │   ├── Entities/          (User, Role, Project, TaskItem, AuditLog, RefreshToken)
│   │   ├── Enums/             (TaskItemStatus, TaskPriority, UserStatus)
│   │   └── Interfaces/        (IRepository<T>, IUnitOfWork, IStoredProcedureExecutor)
│   ├── TaskForge.Application/
│   │   ├── DTOs/              (All request/response DTOs, PagedResult, ApiResponse)
│   │   └── Interfaces/        (IAuthService, IUserService, ITaskService, etc.)
│   ├── TaskForge.Infrastructure/
│   │   ├── Auth/              (JwtTokenProvider, BasicAuthenticationHandler)
│   │   ├── BackgroundServices/(AuditLogProcessor, TaskNotificationService)
│   │   ├── Data/              (AppDbContext, StoredProcedureExecutor)
│   │   ├── Repositories/      (Repository<T>, UnitOfWork)
│   │   └── Services/          (AuthService, UserService, TaskService, etc.)
│   └── TaskForge.API/
│       ├── Controllers/V1/    (Auth, Users, Roles, Projects, Tasks, Reports)
│       ├── Middleware/         (RequestLogging, ExceptionHandling)
│       └── Program.cs         (Full DI, auth, Swagger, Serilog config)
├── sql/
│   ├── 01_CreateDatabase.sql
│   ├── 02_StoredProcedures.sql
│   ├── 03_Triggers.sql
│   ├── 04_Indexes.sql
│   └── 05_SeedData.sql
└── postman/
    └── TaskForge.postman_collection.json
```

---

## 🧪 Testing with Postman

1. Import `postman/TaskForge.postman_collection.json` into Postman
2. The collection auto-saves tokens from login/register responses
3. Call **Login** first → all subsequent requests use the saved JWT

---

## 💡 Key Concepts Demonstrated

### Clean Architecture
- **Domain**: Pure entities, no framework dependencies
- **Application**: DTOs and interfaces only, no implementation
- **Infrastructure**: All external concerns (DB, auth, caching)
- **API**: HTTP layer, middleware, DI composition root

### Multithreading & Performance
- `async/await` throughout (non-blocking I/O)
- `Parallel.ForEachAsync` for bulk task updates
- `SemaphoreSlim` for thread-safe bulk operations
- `ConcurrentQueue` in `AuditLogProcessor` background service
- `ConcurrentDictionary` in `UnitOfWork` for repository caching

### SQL Server Advanced Features
- **Stored Procedures**: 8 SPs covering CRUD and analytics
- **JOINs**: INNER JOIN, LEFT JOIN across Users/Projects/Tasks
- **GROUP BY**: Aggregation in project reports
- **CTE**: Recursive data preparation in productivity reports
- **Window Functions**: `ROW_NUMBER()`, `RANK()` for rankings
- **Temp Tables**: Intermediate result computation in overdue tasks SP
- **Triggers**: `AFTER INSERT/UPDATE/DELETE` on TaskItems → AuditLogs
- **Indexes**: Composite, covering, filtered indexes with `INCLUDE` columns
