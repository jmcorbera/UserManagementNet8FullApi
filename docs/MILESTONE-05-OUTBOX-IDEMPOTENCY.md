# Milestone 05 — Outbox Pattern + Idempotency

Documentation of what was implemented in the fifth milestone: Transactional Outbox pattern for reliable domain event publishing, Idempotency pattern for command deduplication, Unit of Work pattern for transaction coordination, and background processor for event publishing.

---

## 1. Milestone Objective

- Implement the **Transactional Outbox Pattern** to persist domain events atomically with business data.
- Create a **Background Processor** to publish outbox messages reliably.
- Implement the **Idempotency Pattern** to prevent duplicate command execution.
- Introduce **Unit of Work** pattern to coordinate persistence of entities and domain events.
- Update **Domain entities** to raise domain events when business operations occur.
- Ensure **all existing tests continue to pass** with the new architecture.

---

## 2. Structure

### 2.1 Domain Layer

```
src/UserManagement.Domain/
├── Common/
│   └── BaseEntity.cs                    # Updated: domain events collection
├── Entities/
│   ├── User.cs                          # Updated: raises domain events
│   ├── OutboxMessage.cs                 # NEW: outbox message entity
│   └── IdempotencyKey.cs                # NEW: idempotency key entity
├── Enums/
│   └── IdempotencyStatus.cs             # NEW: idempotency status enum
└── Repositories/
    └── IOutboxMessageRepository.cs      # NEW: outbox repository interface
```

### 2.2 Application Layer

```
src/UserManagement.Application/
├── Common/
│   ├── Abstractions/
│   │   ├── IUnitOfWork.cs               # NEW: unit of work interface
│   │   ├── IIdempotentCommand.cs        # NEW: marker interface for idempotent commands
│   │   └── IIdempotencyRepository.cs    # NEW: idempotency repository interface
│   ├── Behaviors/
│   │   └── IdempotencyBehavior.cs       # NEW: idempotency pipeline behavior
│   ├── Options/
│   │   └── IdempotencyOptions.cs        # NEW: idempotency configuration
│   └── Validators/
│       └── IdempotentCommandValidator.cs # NEW: validates IdempotencyKey GUID
└── Features/Users/Commands/
    ├── RegisterUser/
    │   ├── RegisterUserCommand.cs       # Updated: implements IIdempotentCommand
    │   ├── RegisterUserCommandHandler.cs # Updated: uses IUnitOfWork, raises events, refactored
    │   └── RegisterUserCommandValidator.cs # Updated: includes IdempotentCommandValidator
    └── VerifyOtp/
        ├── VerifyOtpCommand.cs          # Updated: implements IIdempotentCommand
        ├── VerifyOtpCommandHandler.cs   # Updated: uses IUnitOfWork, raises events
        └── VerifyOtpCommandValidator.cs # Updated: includes IdempotentCommandValidator
```

### 2.3 Infrastructure Layer

```
src/UserManagement.Infrastructure/
├── BackgroundServices/
│   ├── OutboxProcessor.cs               # NEW: background service for publishing events
│   └── OutboxProcessorOptions.cs        # NEW: outbox processor configuration
├── Persistence/
│   ├── MySqlDbContext.cs                # Updated: intercepts domain events, creates outbox messages
│   ├── UnitOfWork.cs                    # NEW: unit of work implementation
│   ├── Configurations/
│   │   ├── OutboxMessageConfiguration.cs # NEW: EF Core config for OutboxMessage
│   │   └── IdempotencyKeyConfiguration.cs # NEW: EF Core config for IdempotencyKey
│   └── Repositories/
│       ├── UserRepository.cs            # Updated: removed SaveChangesAsync calls
│       ├── UserOtpRepository.cs         # Updated: removed SaveChangesAsync calls
│       ├── OutboxMessageRepository.cs   # NEW: outbox message repository
│       └── IdempotencyRepository.cs     # NEW: idempotency repository
└── DependencyInjection.cs               # Updated: registers new services
```

---

## 3. Main Elements

### 3.1 Domain Events Infrastructure

**BaseEntity Updates**

Added domain events collection and methods to `BaseEntity<T>`:

```csharp
private readonly List<IDomainEvent> _domainEvents = new();

public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();
public void ClearDomainEvents() => _domainEvents.Clear();
protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

**User Entity Updates**

Added methods to raise domain events:

- `RaiseRegistrationRequestedEvent(string otpCode)`: Raises `UserRegistrationRequested` event when user registers.
- `ActivateAndRaiseEvent(string cognitoSub)`: Raises `UserVerified` event when user is verified and activated.

---

### 3.2 Outbox Pattern

**OutboxMessage Entity**

Represents a domain event persisted for reliable publishing:

- **Properties**: `Id`, `Type`, `Content` (JSON), `OccurredAt`, `ProcessedAt`, `Error`
- **Methods**: `MarkAsProcessed()`, `MarkAsFailed(string error)`, `IsProcessed()`

**MySqlDbContext Override**

Intercepts domain events during `SaveChangesAsync`:

1. Collects domain events from all tracked entities (`BaseEntity<Guid>`).
2. Serializes events to JSON.
3. Creates `OutboxMessage` entities.
4. Persists outbox messages in the same transaction as business data.
5. Clears domain events from entities.

**OutboxProcessor Background Service**

Polls and publishes unprocessed outbox messages:

- **Polling Interval**: Configurable (default: 10 seconds).
- **Batch Size**: Configurable (default: 20 messages).
- **Processing**: Logs events (future: publish to message bus).
- **Error Handling**: Marks failed messages with error details.

---

### 3.3 Unit of Work Pattern

**IUnitOfWork Interface**

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**UnitOfWork Implementation**

Wraps `MySqlDbContext.SaveChangesAsync` to coordinate transaction boundaries.

**Repository Updates**

Repositories no longer call `SaveChangesAsync` directly:

- `AddAsync` and `UpdateAsync` methods only add/update entities to the change tracker.
- Handlers control transaction boundaries by calling `IUnitOfWork.SaveChangesAsync`.

---

### 3.4 Idempotency Pattern

**IdempotencyKey Entity**

Tracks processed commands to prevent duplicates:

- **Properties**: `Id`, `Key` (Guid), `CommandName`, `CreatedAt`, `CompletedAt`, `Result` (JSON), `Status` (enum: InProgress, Completed, Failed)
- **Methods**: `SetResult(string result)`, `SetFailed()`, `IsCompleted()`, `IsExpired(TimeSpan ttl)`

**IIdempotentCommand Interface**

Marker interface for commands that support idempotency:

```csharp
public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }  // Required, not optional
}
```

**IdempotencyBehavior**

MediatR pipeline behavior that uses an **insert-first pattern**:

1. Checks if command implements `IIdempotentCommand`.
2. Attempts to create idempotency key in database (TryCreateAsync).
3. If creation succeeds, executes command and stores result.
4. If creation fails (key exists), checks status:
   - **Completed**: Returns cached result
   - **InProgress**: Throws exception (concurrent request)
   - **Failed**: Allows retry
5. Handles key expiration (default: 24 hours).
6. Marks as Failed if command execution throws exception.

**Command Updates**

`RegisterUserCommand` and `VerifyOtpCommand` now require idempotency keys:

```csharp
public sealed record RegisterUserCommand(
    string Email, 
    string Name, 
    Guid IdempotencyKey  // Required GUID
) : IRequest<Result>, IIdempotentCommand;
```

**Validation**: Commands include `IdempotentCommandValidator` to ensure valid GUID.

---

### 3.5 EF Core Configurations

**OutboxMessageConfiguration**

- Table: `OutboxMessages`
- Indexes: `ProcessedAt`, `OccurredAt`
- Max lengths: `Type` (500), `Error` (2000)

**IdempotencyKeyConfiguration**

- Table: `IdempotencyKeys`
- Unique index on `Key`
- Index on `CreatedAt` for cleanup queries

---

### 3.6 Dependency Injection

**Infrastructure DI Updates**

```csharp
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
services.Configure<OutboxProcessorOptions>(configuration.GetSection("OutboxProcessor"));
services.AddHostedService<OutboxProcessor>();
```

**Application DI Updates**

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
services.Configure<IdempotencyOptions>(configuration.GetSection("Idempotency"));
```

**Pipeline Order**

1. `ValidationBehavior` (validates command including IdempotencyKey)
2. `IdempotencyBehavior` (checks for duplicate commands)
3. Handler execution

**Note**: Validation runs first to ensure IdempotencyKey is valid before checking duplicates.

---

## 4. Configuration

### 4.1 OutboxProcessor Configuration

Add to `appsettings.json`:

```json
{
  "OutboxProcessor": {
    "PollingIntervalSeconds": 10,
    "BatchSize": 20
  }
}
```

### 4.2 Idempotency Configuration

Add to `appsettings.json`:

```json
{
  "Idempotency": {
    "TtlHours": 24
  }
}
```

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

**All tests should pass**:

- ✅ Unit tests (51 tests)
- ✅ Application unit tests (6 tests)
- ✅ Integration tests (10 tests)

### 5.3 Manual Verifications

- ✅ Domain events are raised when users are created/verified.
- ✅ Events are persisted in `OutboxMessages` table atomically with entities.
- ✅ Background processor polls and processes outbox messages.
- ✅ Duplicate commands with same idempotency key return cached results.
- ✅ Unit of Work coordinates transaction boundaries.
- ✅ All existing tests continue to pass.
- ✅ Repositories no longer call `SaveChangesAsync` directly.

---

## 6. Key Implementation Details

### 6.1 Domain Event Flow

1. **Handler executes business logic** → User entity raises domain event via `RaiseDomainEvent()`.
2. **Handler calls `UnitOfWork.SaveChangesAsync()`** → Triggers `MySqlDbContext.SaveChangesAsync()`.
3. **DbContext intercepts events** → Collects events from all tracked entities.
4. **Events serialized to JSON** → Creates `OutboxMessage` entities.
5. **Atomic persistence** → Entities + outbox messages saved in single transaction.
6. **Background processor** → Polls unprocessed messages and publishes them.

### 6.2 Idempotency Flow

1. **Command received with idempotency key** → `IdempotencyBehavior` intercepts.
2. **Check for existing key** → Query `IdempotencyKeys` table.
3. **If key exists and completed** → Deserialize and return cached result.
4. **If key doesn't exist** → Create new key, execute command.
5. **Store result** → Serialize result to JSON, update idempotency key.
6. **Future requests** → Return cached result without re-execution.

### 6.3 Transaction Coordination

**Before (Milestone 04)**:
```csharp
await _userRepository.AddAsync(user);  // Calls SaveChangesAsync internally
await _otpRepository.AddAsync(otp);    // Calls SaveChangesAsync internally
```

**After (Milestone 05)**:
```csharp
await _userRepository.AddAsync(user);   // Only adds to change tracker
await _otpRepository.AddAsync(otp);     // Only adds to change tracker
await _unitOfWork.SaveChangesAsync();   // Single transaction for all changes + events
```

---

## 7. Database Schema

### 7.1 OutboxMessages Table

```sql
CREATE TABLE OutboxMessages (
    Id CHAR(36) PRIMARY KEY,
    Type VARCHAR(500) NOT NULL,
    Content LONGTEXT NOT NULL,
    OccurredAt DATETIME(6) NOT NULL,
    ProcessedAt DATETIME(6) NULL,
    Error VARCHAR(2000) NULL,
    INDEX IX_OutboxMessages_ProcessedAt (ProcessedAt),
    INDEX IX_OutboxMessages_OccurredAt (OccurredAt)
);
```

### 7.2 IdempotencyKeys Table

```sql
CREATE TABLE IdempotencyKeys (
    Id CHAR(36) PRIMARY KEY,
    Key VARCHAR(200) NOT NULL,
    CommandName VARCHAR(500) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,
    CompletedAt DATETIME(6) NULL,
    Result LONGTEXT NULL,
    UNIQUE INDEX IX_IdempotencyKeys_Key (Key),
    INDEX IX_IdempotencyKeys_CreatedAt (CreatedAt)
);
```

---

## 8. Testing Strategy

### 8.1 Unit Tests

**FakeUnitOfWork** created for testing:

```csharp
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }
    
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}
```

All existing unit tests updated to inject `FakeUnitOfWork`.

### 8.2 Integration Tests

**StubUnitOfWork** created for integration tests without database:

```csharp
public sealed class StubUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }
}
```

**PersistenceTests** updated to call `DbContext.SaveChangesAsync()` directly since repositories no longer save.

---

## 9. Next Steps (Milestone 06)

In milestone 06 (External Services) the following will be implemented:

- **Email Service**: Real implementation using SendGrid/SMTP for OTP delivery.
- **OTP Generator**: Secure random code generation.
- **Message Bus Integration**: Publish outbox events to RabbitMQ/Azure Service Bus.
- **Retry Logic**: Exponential backoff for failed outbox messages.
- **Cleanup Jobs**: Remove old outbox messages and expired idempotency keys.

---

## 10. Key Files Created/Modified

### Created Files

| File | Purpose |
|------|---------|
| `Domain/Entities/OutboxMessage.cs` | Outbox message entity for event persistence |
| `Domain/Entities/IdempotencyKey.cs` | Idempotency key entity for deduplication |
| `Domain/Enums/IdempotencyStatus.cs` | Enum for idempotency states (InProgress, Completed, Failed) |
| `Domain/Repositories/IOutboxMessageRepository.cs` | Outbox repository interface |
| `Application/Common/Abstractions/IUnitOfWork.cs` | Unit of work interface |
| `Application/Common/Abstractions/IIdempotentCommand.cs` | Marker interface for idempotent commands |
| `Application/Common/Abstractions/IIdempotencyRepository.cs` | Idempotency repository interface |
| `Application/Common/Behaviors/IdempotencyBehavior.cs` | Idempotency pipeline behavior with insert-first pattern |
| `Application/Common/Options/IdempotencyOptions.cs` | Idempotency configuration options |
| `Application/Common/Validators/IdempotentCommandValidator.cs` | FluentValidation validator for IdempotencyKey |
| `Infrastructure/Persistence/UnitOfWork.cs` | Unit of work implementation |
| `Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs` | EF Core config for OutboxMessage |
| `Infrastructure/Persistence/Configurations/IdempotencyKeyConfiguration.cs` | EF Core config for IdempotencyKey with Status enum |
| `Infrastructure/Persistence/Repositories/OutboxMessageRepository.cs` | Outbox message repository |
| `Infrastructure/Persistence/Repositories/IdempotencyRepository.cs` | Idempotency repository with TryCreateAsync |
| `Infrastructure/BackgroundServices/OutboxProcessor.cs` | Background service for event publishing |
| `Infrastructure/BackgroundServices/OutboxProcessorOptions.cs` | Outbox processor configuration |
| `tests/.../Fakes/FakeUnitOfWork.cs` | Fake UnitOfWork for unit tests |
| `tests/.../Stubs/StubUnitOfWork.cs` | Stub UnitOfWork for integration tests |

### Modified Files

| File | Changes |
|------|---------|
| `Domain/Common/BaseEntity.cs` | Added domain events collection and methods |
| `Domain/Entities/User.cs` | Added methods to raise domain events |
| `Application/Features/.../RegisterUserCommand.cs` | Implements IIdempotentCommand with required Guid |
| `Application/Features/.../RegisterUserCommandHandler.cs` | Uses IUnitOfWork, raises events, refactored with private methods |
| `Application/Features/.../RegisterUserCommandValidator.cs` | Includes IdempotentCommandValidator |
| `Application/Features/.../VerifyOtpCommand.cs` | Implements IIdempotentCommand with required Guid |
| `Application/Features/.../VerifyOtpCommandHandler.cs` | Uses IUnitOfWork, raises events |
| `Application/Features/.../VerifyOtpCommandValidator.cs` | Includes IdempotentCommandValidator |
| `Application/Features/.../SyncUserCommandHandler.cs` | Uses IUnitOfWork |
| `Application/DependencyInjection.cs` | Registers behaviors in correct order (Validation → Idempotency) |
| `Application/Fakes/FakeInstances.cs` | Removed fake repository implementations |
| `Infrastructure/Persistence/MySqlDbContext.cs` | Overrides SaveChangesAsync to handle events |
| `Infrastructure/Persistence/Repositories/UserRepository.cs` | Removed SaveChangesAsync calls |
| `Infrastructure/Persistence/Repositories/UserOtpRepository.cs` | Removed SaveChangesAsync calls |
| `Infrastructure/DependencyInjection.cs` | Registers UoW, repositories, OutboxProcessor |
| `Infrastructure/UserManagement.Infrastructure.csproj` | Added Microsoft.Extensions.Hosting.Abstractions |
| `Application/UserManagement.Application.csproj` | Added Microsoft.Extensions.Logging.Abstractions |
| `tests/.../PersistenceTests.cs` | Updated to call DbContext.SaveChangesAsync |
| `tests/.../RegisterUserHandlerTests.cs` | Updated to inject FakeUnitOfWork and use Guid keys |
| `tests/.../VerifyOtpHandlerTests.cs` | Updated to inject FakeUnitOfWork and use Guid keys |
| `tests/.../CustomWebApplicationFactory.cs` | Registers StubUnitOfWork |

---

## 11. Technical Notes

### 11.1 Event Serialization

Domain events are serialized using `System.Text.Json`:

```csharp
var outboxMessage = OutboxMessage.Create(
    domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
    JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
    domainEvent.OccurredAt
);
```

### 11.2 Idempotency Key Format

Idempotency keys are:

- **Client-provided**: Required GUID values.
- **Validated**: FluentValidation ensures non-empty valid GUIDs.
- **Unique per command**: Same key for different commands is allowed.
- **Status tracking**: InProgress, Completed, or Failed states prevent concurrent execution.

### 11.3 Outbox Cleanup

Currently, processed outbox messages are not automatically deleted. Future enhancements:

- Scheduled job to delete messages older than X days.
- Archival to separate table for audit purposes.

### 11.4 Idempotency Key Expiration

Keys expire after 24 hours (configurable). Expired keys allow command re-execution.

### 11.5 Background Service Lifecycle

`OutboxProcessor` runs as a hosted service:

- Starts when application starts.
- Stops gracefully when application shuts down.
- Logs all processing activities.

---

## 12. Verification Commands

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter FullyQualifiedName~IntegrationTests

# Run only persistence tests
dotnet test --filter FullyQualifiedName~PersistenceTests

# Run the API (OutboxProcessor will start automatically)
dotnet run --project src/UserManagement.API
```

---

## 13. References

- **Milestone 01**: Solution setup and base structure.
- **Milestone 02**: Domain (DDD) with entities, VOs, events, factories, repos and specifications.
- **Milestone 03**: Application Core with CQRS, Result pattern, feature flags and handlers.
- **Milestone 04**: Infrastructure/Data with EF Core, MySQL, repositories and integration tests.
- **Transactional Outbox Pattern**: [Microservices.io](https://microservices.io/patterns/data/transactional-outbox.html)
- **Idempotency Pattern**: [Microsoft Docs](https://learn.microsoft.com/en-us/azure/architecture/patterns/idempotent-consumer)
- **Unit of Work Pattern**: [Martin Fowler](https://martinfowler.com/eaaCatalog/unitOfWork.html)
- **Domain Events**: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
