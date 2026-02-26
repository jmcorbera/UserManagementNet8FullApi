# Milestone 04 — Infrastructure/Data (MySQL)

Documentación de lo implementado en el cuarto milestone: capa de persistencia con EF Core y MySQL, repositorios concretos, mapeos para entidades de dominio (incluyendo Value Objects), filtro global de soft delete, provider de fecha/hora, y tests de integración con Testcontainers.

---

## 1. Objetivo del milestone

- Implementar la capa **Infrastructure/Persistence** con EF Core y MySQL (Pomelo provider).
- Crear `MySqlDbContext` con mapeos (Fluent API) para `User` y `UserOtp`.
- Mapear **directamente** a las entidades de Dominio usando `ValueConverter` para `Email`.
- Implementar **repositorios concretos** (`UserRepository`, `UserOtpRepository`) que persisten con `SaveChangesAsync`.
- Configurar **filtro global de soft delete** para `User` (`IsDeleted`).
- Implementar **`DateTimeProvider`** concreto para `IDateTimeProvider`.
- **Registrar** repositorios y `IDateTimeProvider` en Infrastructure DI.
- **Mantener fakes** en Application para servicios externos (`IEmailSender`, `IOtpGenerator`, `ICognitoIdentityService`) que se implementarán en milestones 06-07.
- Agregar **integration tests con Testcontainers MySQL** para validar persistencia y soft delete.

---

## 2. Estructura (Infrastructure)

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

## 3. Elementos principales

### 3.1 Paquetes

Paquetes agregados en `UserManagement.Infrastructure.csproj`:

- `Microsoft.EntityFrameworkCore` (8.0.2)
- `Pomelo.EntityFrameworkCore.MySql` (8.0.2)
- `Microsoft.EntityFrameworkCore.Design` (8.0.2)

### 3.2 MySqlDbContext

`MySqlDbContext` hereda de `DbContext` y expone:

- `DbSet<User> Users`
- `DbSet<UserOtp> UserOtps`

Aplica configuraciones desde el assembly usando `ApplyConfigurationsFromAssembly`.

### 3.3 Configuraciones EF Core (Fluent API)

**UserConfiguration**

- Tabla: `Users`
- PK: `Id` (Guid)
- `Email`: Value Object convertido a string (max 320 chars), índice único
- `Name`: string (max 200 chars)
- `Status`: enum `UserStatus` (int)
- `CognitoSub`: string nullable (max 100 chars), índice
- `IsDeleted`: bool
- Propiedades de auditoría: `Created`, `LastModified`
- **Query filter global**: `HasQueryFilter(u => !u.IsDeleted)` para soft delete

**UserOtpConfiguration**

- Tabla: `UserOtps`
- PK: `Id` (Guid)
- `Email`: Value Object convertido a string (max 320 chars)
- `Code`: string (max 20 chars)
- `ExpiresAt`: DateTimeOffset
- `Used`: bool
- `CreatedAt`: DateTimeOffset
- Índices compuestos: `(Email, Code)` y `(Email, CreatedAt)`

**ValueConverter para Email**

```csharp
var emailConverter = new ValueConverter<Email, string>(
    v => v.Value,
    v => Email.Create(v));
```

Permite que EF Core persista el Value Object `Email` como string en la BD.

---

### 3.4 Repositorios

**UserRepository**

Implementa `IUserRepository` con:

- `GetByIdAsync`
- `GetByEmailAsync`
- `GetByCognitoSubAsync`
- `GetAllAsync` (ordenado por `Created` descendente)
- `AddAsync` (con `SaveChangesAsync`)
- `UpdateAsync` (con `SaveChangesAsync`)
- `ExistsByEmailAsync`

Todos los métodos de lectura respetan el **filtro global de soft delete**.

**UserOtpRepository**

Implementa `IUserOtpRepository` con:

- `GetByEmailAndCodeAsync`
- `GetLatestByEmailAsync` (ordenado por `CreatedAt` descendente)
- `AddAsync` (con `SaveChangesAsync`)
- `UpdateAsync` (con `SaveChangesAsync`)

---

### 3.5 DateTimeProvider

**Implementación concreta**

`DateTimeProvider` implementa `IDateTimeProvider` retornando `DateTimeOffset.UtcNow`. Se registra como Singleton en Infrastructure.

```csharp
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

---

### 3.6 Dependency Injection

**Cambios en `UserManagement.Application.DependencyInjection`**

- Registra MediatR, FluentValidation, pipeline behaviors y options.
- **Mantiene fakes** para servicios externos que se implementarán en milestones posteriores:
  - `IEmailSender` → `EmailSender` (fake, milestone 06)
  - `IOtpGenerator` → `OtpGenerator` (fake, milestone 06)
  - `ICognitoIdentityService` → `CognitoIdentityService` (fake, milestone 07)

**Cambios en `UserManagement.Infrastructure.DependencyInjection`**

- Registra `MySqlDbContext` con MySQL (Pomelo) usando `ConnectionStrings:MySqlServerConnectionString`.
- Usa `ServerVersion.AutoDetect` para detectar automáticamente la versión de MySQL.
- Registra repositorios concretos:
  - `IUserRepository` → `UserRepository` (Scoped)
  - `IUserOtpRepository` → `UserOtpRepository` (Scoped)
- Registra provider de fecha/hora:
  - `IDateTimeProvider` → `DateTimeProvider` (Singleton)
- **Condicional**: solo registra DbContext si existe `ConnectionStrings:MySqlServerConnectionString` (para no romper tests que no usan DB).

---

## 4. Tests de integración

### 4.1 Paquetes de test

Agregado en `UserManagement.IntegrationTests.csproj`:

- `Testcontainers.MySql` (3.10.0)
- Referencia a proyecto `UserManagement.Infrastructure`

### 4.2 PersistenceTests

Clase de test que valida:

1. **Persistencia básica**: `AddAsync` + `GetByEmailAsync` funciona correctamente.
2. **Soft delete**: usuarios marcados como `IsDeleted` no se devuelven en `GetByEmailAsync`, `GetAllAsync` ni `ExistsByEmailAsync`.
3. **UserOtpRepository**: `AddAsync` + `GetByEmailAndCodeAsync` funciona.

**Infraestructura de test**

- Usa `Testcontainers.MySql` para levantar un contenedor MySQL efímero.
- Implementa `IAsyncLifetime` (xUnit) para inicializar/destruir el contenedor por clase de test.
- Llama a `EnsureCreatedAsync()` para crear el schema antes de ejecutar tests.

---

## 5. Verificación del milestone

### 5.1 Compilación

```bash
dotnet build
```

### 5.2 Tests

```bash
dotnet test
```

**Tests que deben pasar**:

- `HealthCheckTests` (milestone 01)
- `ArquitectureTests` (milestone 01-03)
- `PersistenceTests` (milestone 04):
  - `UserRepository_AddAndGetByEmail_ShouldPersistAndRetrieve`
  - `UserRepository_SoftDelete_ShouldFilterDeletedUsers`
  - `UserRepository_ExistsByEmail_ShouldRespectSoftDelete`
  - `UserOtpRepository_AddAndGetByEmailAndCode_ShouldWork`

### 5.3 Verificaciones manuales

- ✅ Infrastructure compila con EF Core + Pomelo MySQL.
- ✅ `AppDbContext` tiene configuraciones Fluent API para `User` y `UserOtp`.
- ✅ `Email` (Value Object) se persiste como string usando `ValueConverter`.
- ✅ Filtro global de soft delete funciona (usuarios con `IsDeleted = true` no se devuelven).
- ✅ Repositorios concretos funcionan con `SaveChangesAsync`.
- ✅ Application **no** depende de Infrastructure (se mantiene regla de arquitectura).
- ✅ Tests de persistencia pasan con Testcontainers MySQL.

---

## 6. Próximos pasos (Milestone 05)

En el milestone 05 (Outbox + Idempotencia) se implementará:

- Tabla `OutboxMessages` para eventos de dominio.
- Persistencia de eventos en la misma transacción (Unit of Work).
- Background processor para publicar eventos.
- Tabla `IdempotencyKeys` para comandos idempotentes.
- Decorator de idempotencia para `RegisterUser` y `VerifyOtp`.

---

## 7. Archivos clave creados/modificados

| Archivo | Propósito |
|---------|-----------|
| `Persistence/MySqlDbContext.cs` | DbContext con DbSets de User y UserOtp |
| `Persistence/Configurations/UserConfiguration.cs` | Fluent API para User (Email converter, soft delete filter, índices) |
| `Persistence/Configurations/UserOtpConfiguration.cs` | Fluent API para UserOtp (Email converter, índices compuestos) |
| `Persistence/Repositories/UserRepository.cs` | Implementación concreta de IUserRepository con EF Core |
| `Persistence/Repositories/UserOtpRepository.cs` | Implementación concreta de IUserOtpRepository con EF Core |
| `Services/DateTimeProvider.cs` | Implementación concreta de IDateTimeProvider |
| `DependencyInjection.cs` (Infrastructure) | Registro de DbContext + repositorios + DateTimeProvider |
| `DependencyInjection.cs` (Application) | Registro de fakes para servicios externos (EmailSender, OtpGenerator, CognitoIdentityService) |
| `tests/UserManagement.IntegrationTests/PersistenceTests.cs` | Tests de persistencia con Testcontainers MySQL |

---

## 8. Comandos de verificación

```bash
# Compilar
dotnet build

# Ejecutar tests
dotnet test

# Ejecutar solo tests de persistencia
dotnet test --filter FullyQualifiedName~PersistenceTests
```

---

## 9. Notas técnicas

### Soft delete en MySQL

El filtro global `HasQueryFilter(u => !u.IsDeleted)` se aplica automáticamente a todas las queries de EF Core sobre `User`. Para incluir usuarios eliminados (ej. auditoría), se puede usar `IgnoreQueryFilters()`.

### ValueConverter para Email

El `ValueConverter` permite que EF Core:

- **Persista**: convierte `Email` → `string` (usando `email.Value`).
- **Hidrate**: convierte `string` → `Email` (usando `Email.Create(value)`).

Esto mantiene el Value Object en el dominio sin crear entidades de persistencia separadas.

### Índice único en Email

MySQL respeta collation (case-sensitivity) en índices únicos. La configuración actual usa collation por defecto de MySQL (generalmente `utf8mb4_0900_ai_ci` en MySQL 8.0, que es case-insensitive). El Value Object `Email` normaliza a lowercase antes de persistir, evitando duplicados por mayúsculas/minúsculas.

### Testcontainers

Testcontainers levanta un contenedor Docker de MySQL 8.0 durante la ejecución de tests. Requiere:

- Docker Desktop (o Docker Engine) corriendo.
- Permisos para crear/destruir contenedores.

Si Docker no está disponible, los tests de `PersistenceTests` fallarán. Para CI/CD, asegurar que el runner tenga Docker habilitado.

---

## 10. Referencias

- **Milestone 01**: Setup de la solución y estructura base.
- **Milestone 02**: Dominio (DDD) con entidades, VOs, events, factories, repos y specifications.
- **Milestone 03**: Application Core con CQRS, Result pattern, feature flags y handlers.
- **EF Core Value Conversions**: [Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions)
- **Pomelo MySQL Provider**: [GitHub](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- **Testcontainers**: [Testcontainers for .NET](https://dotnet.testcontainers.org/)
