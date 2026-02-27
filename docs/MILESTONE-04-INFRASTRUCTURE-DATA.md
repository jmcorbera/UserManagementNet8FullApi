# Milestone 04 — Infrastructure/Data (MySQL)

Documentation of what was implemented in the fourth milestone: persistence layer with EF Core and MySQL, concrete repositories, mappings for domain entities (including Value Objects), global soft delete filter, date/time provider, and integration tests with Testcontainers.

---

## 1. Milestone Objective

- Implement the **Infrastructure/Persistence** layer with EF Core and MySQL (Pomelo provider).
- Create `MySqlDbContext` with mappings (Fluent API) for `User` and `UserOtp`.
- Map **directly** to Domain entities using `ValueConverter` for `Email`.
- Implement **concrete repositories** (`UserRepository`, `UserOtpRepository`) that persist with `SaveChangesAsync`.
- Configure **global soft delete filter** for `User` (`IsDeleted`).
- Implement concrete **`DateTimeProvider`** for `IDateTimeProvider`.
- **Register** repositories and `IDateTimeProvider` in Infrastructure DI.
- **Keep fakes** in Application for external services (`IEmailSender`, `IOtpGenerator`, `ICognitoIdentityService`) that will be implemented in milestones 06-07.
- Add **integration tests with Testcontainers MySQL** to validate persistence and soft delete.

---

## 2. Structure (Infrastructure)

```
src/UserManagement.Infrastructure/
├── Persistence/
│   ├── MySqlDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   └── UserOtpConfiguration.cs
│   └── Repositories/
│       ├── UserRepository.cs
│       └── UserOtpRepository.cs
├── Services/
│   └── DateTimeProvider.cs
├── AssemblyReference.cs
├── DependencyInjection.cs
└── UserManagement.Infrastructure.csproj
```

---

## 3. Main Elements

### 3.1 Packages

Packages added in `UserManagement.Infrastructure.csproj`:

- `Microsoft.EntityFrameworkCore` (8.0.2)
- `Pomelo.EntityFrameworkCore.MySql` (8.0.2)
- `Microsoft.EntityFrameworkCore.Design` (8.0.2)

### 3.2 MySqlDbContext

`MySqlDbContext` inherits from `DbContext` and exposes:

- `DbSet<User> Users`
- `DbSet<UserOtp> UserOtps`

Applies configurations from the assembly using `ApplyConfigurationsFromAssembly`.

### 3.3 EF Core Configurations (Fluent API)

**UserConfiguration**

- Table: `Users`
- PK: `Id` (Guid)
- `Email`: Value Object converted to string (max 320 chars), unique index
- `Name`: string (max 200 chars)
- `Status`: enum `UserStatus` (int)
- `CognitoSub`: nullable string (max 100 chars), index
- `IsDeleted`: bool
- Audit properties: `Created`, `LastModified`
- **Global query filter**: `HasQueryFilter(u => !u.IsDeleted)` for soft delete

**UserOtpConfiguration**

- Table: `UserOtps`
- PK: `Id` (Guid)
- `Email`: Value Object converted to string (max 320 chars)
- `Code`: string (max 20 chars)
- `ExpiresAt`: DateTimeOffset
- `Used`: bool
- `CreatedAt`: DateTimeOffset
- Composite indexes: `(Email, Code)` and `(Email, CreatedAt)`

**ValueConverter for Email**

```csharp
var emailConverter = new ValueConverter<Email, string>(
    v => v.Value,
    v => Email.Create(v));
```

Allows EF Core to persist the `Email` Value Object as a string in the database.

---

### 3.4 Repositories

**UserRepository**

Implements `IUserRepository` with:

- `GetByIdAsync`
- `GetByEmailAsync`
- `GetByCognitoSubAsync`
- `GetAllAsync` (ordered by `Created` descending)
- `AddAsync` (with `SaveChangesAsync`)
- `UpdateAsync` (with `SaveChangesAsync`)
- `ExistsByEmailAsync`

All read methods respect the **global soft delete filter**.

**UserOtpRepository**

Implements `IUserOtpRepository` with:

- `GetByEmailAndCodeAsync`
- `GetLatestByEmailAsync` (ordered by `CreatedAt` descending)
- `AddAsync` (with `SaveChangesAsync`)
- `UpdateAsync` (with `SaveChangesAsync`)

---

### 3.5 DateTimeProvider

**Concrete Implementation**

`DateTimeProvider` implements `IDateTimeProvider` returning `DateTimeOffset.UtcNow`. It is registered as Singleton in Infrastructure.

```csharp
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

---

### 3.6 Dependency Injection

**Changes in `UserManagement.Application.DependencyInjection`**

- Registers MediatR, FluentValidation, pipeline behaviors and options.
- **Keeps fakes** for external services that will be implemented in later milestones:
  - `IEmailSender` → `EmailSender` (fake, milestone 06)
  - `IOtpGenerator` → `OtpGenerator` (fake, milestone 06)
  - `ICognitoIdentityService` → `CognitoIdentityService` (fake, milestone 07)

**Changes in `UserManagement.Infrastructure.DependencyInjection`**

- Registers `MySqlDbContext` with MySQL (Pomelo) using `ConnectionStrings:MySqlServerConnectionString`.
- Uses `ServerVersion.AutoDetect` to automatically detect MySQL version.
- Registers concrete repositories:
  - `IUserRepository` → `UserRepository` (Scoped)
  - `IUserOtpRepository` → `UserOtpRepository` (Scoped)
- Registers date/time provider:
  - `IDateTimeProvider` → `DateTimeProvider` (Singleton)
- **Conditional**: only registers DbContext if `ConnectionStrings:MySqlServerConnectionString` exists (to avoid breaking tests that don't use DB).

---

## 4. Integration Tests

### 4.1 Test Packages

Added in `UserManagement.IntegrationTests.csproj`:

- `Testcontainers.MySql` (3.10.0)
- Reference to `UserManagement.Infrastructure` project

### 4.2 PersistenceTests

Test class that validates:

1. **Basic persistence**: `AddAsync` + `GetByEmailAsync` works correctly.
2. **Soft delete**: users marked as `IsDeleted` are not returned in `GetByEmailAsync`, `GetAllAsync` or `ExistsByEmailAsync`.
3. **UserOtpRepository**: `AddAsync` + `GetByEmailAndCodeAsync` works.

**Test Infrastructure**

- Uses `Testcontainers.MySql` to spin up an ephemeral MySQL container.
- Implements `IAsyncLifetime` (xUnit) to initialize/destroy the container per test class.
- Calls `EnsureCreatedAsync()` to create the schema before running tests.

---

## 5. Milestone Verification

### 5.1 Build

```bash
dotnet build
```

### 5.2 Tests

```bash
dotnet test
```

**Tests that should pass**:

- `HealthCheckTests` (milestone 01)
- `ArquitectureTests` (milestone 01-03)
- `PersistenceTests` (milestone 04):
  - `UserRepository_AddAndGetByEmail_ShouldPersistAndRetrieve`
  - `UserRepository_SoftDelete_ShouldFilterDeletedUsers`
  - `UserRepository_ExistsByEmail_ShouldRespectSoftDelete`
  - `UserOtpRepository_AddAndGetByEmailAndCode_ShouldWork`

### 5.3 Manual Verifications

- ✅ Infrastructure compiles with EF Core + Pomelo MySQL.
- ✅ `AppDbContext` has Fluent API configurations for `User` and `UserOtp`.
- ✅ `Email` (Value Object) is persisted as string using `ValueConverter`.
- ✅ Global soft delete filter works (users with `IsDeleted = true` are not returned).
- ✅ Concrete repositories work with `SaveChangesAsync`.
- ✅ Application does **not** depend on Infrastructure (architecture rule is maintained).
- ✅ Persistence tests pass with Testcontainers MySQL.

---

## 6. Next Steps (Milestone 05)

In milestone 05 (Outbox + Idempotency) the following will be implemented:

- `OutboxMessages` table for domain events.
- Event persistence in the same transaction (Unit of Work).
- Background processor to publish events.
- `IdempotencyKeys` table for idempotent commands.
- Idempotency decorator for `RegisterUser` and `VerifyOtp`.

---

## 7. Key Files Created/Modified

| File | Purpose |
|---------|-----------|
| `Persistence/MySqlDbContext.cs` | DbContext with User and UserOtp DbSets |
| `Persistence/Configurations/UserConfiguration.cs` | Fluent API for User (Email converter, soft delete filter, indexes) |
| `Persistence/Configurations/UserOtpConfiguration.cs` | Fluent API for UserOtp (Email converter, composite indexes) |
| `Persistence/Repositories/UserRepository.cs` | Concrete implementation of IUserRepository with EF Core |
| `Persistence/Repositories/UserOtpRepository.cs` | Concrete implementation of IUserOtpRepository with EF Core |
| `Services/DateTimeProvider.cs` | Concrete implementation of IDateTimeProvider |
| `DependencyInjection.cs` (Infrastructure) | Registration of DbContext + repositories + DateTimeProvider |
| `DependencyInjection.cs` (Application) | Registration of fakes for external services (EmailSender, OtpGenerator, CognitoIdentityService) |
| `tests/UserManagement.IntegrationTests/PersistenceTests.cs` | Persistence tests with Testcontainers MySQL |

---

## 8. Verification Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run only persistence tests
dotnet test --filter FullyQualifiedName~PersistenceTests
```

---

## 9. Technical Notes

### Soft Delete in MySQL

The global filter `HasQueryFilter(u => !u.IsDeleted)` is automatically applied to all EF Core queries on `User`. To include deleted users (e.g., for auditing), you can use `IgnoreQueryFilters()`.

### ValueConverter for Email

The `ValueConverter` allows EF Core to:

- **Persist**: converts `Email` → `string` (using `email.Value`).
- **Hydrate**: converts `string` → `Email` (using `Email.Create(value)`).

This keeps the Value Object in the domain without creating separate persistence entities.

### Unique Index on Email

MySQL respects collation (case-sensitivity) in unique indexes. The current configuration uses MySQL's default collation (typically `utf8mb4_0900_ai_ci` in MySQL 8.0, which is case-insensitive). The `Email` Value Object normalizes to lowercase before persisting, avoiding duplicates due to case differences.

### Testcontainers

Testcontainers spins up a MySQL 8.0 Docker container during test execution. It requires:

- Docker Desktop (or Docker Engine) running.
- Permissions to create/destroy containers.

If Docker is not available, `PersistenceTests` tests will fail. For CI/CD, ensure the runner has Docker enabled.

---

## 10. References

- **Milestone 01**: Solution setup and base structure.
- **Milestone 02**: Domain (DDD) with entities, VOs, events, factories, repos and specifications.
- **Milestone 03**: Application Core with CQRS, Result pattern, feature flags and handlers.
- **EF Core Value Conversions**: [Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- **Pomelo MySQL Provider**: [GitHub](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- **Testcontainers**: [Testcontainers for .NET](https://dotnet.testcontainers.org/)
