# User Management API

REST API (.NET 8) for user management with registration (OTP + Cognito), Clean Architecture and CQRS.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Documentation by Milestone

- **[Milestone 01 — Setup](docs/MILESTONE-01-SETUP.md)** — Detailed reference for the first milestone (structure, .env, DI, tests) for course usage.
- **[Milestone 02 — Domain](docs/MILESTONE-02-DOMAIN.md)** — Complete domain implementation (entities, value objects, domain events, factory, repository interfaces, specifications).
- **[Milestone 03 — Application Core](docs/MILESTONE-03-APPLICATION-CORE.md)** — Application Core with CQRS (MediatR), validation (FluentValidation + pipeline), Result pattern, feature flags, abstractions and tests.
- **[Milestone 04 — Infrastructure/Data](docs/MILESTONE-04-INFRASTRUCTURE-DATA.md)** — Persistence layer with EF Core and MySQL, concrete repositories, entity mappings, soft delete global filter, and integration tests with Testcontainers.
- **[Milestone 05 — Outbox + Idempotency](docs/MILESTONE-05-OUTBOX-IDEMPOTENCY.md)** — Transactional Outbox pattern for reliable domain event publishing, Idempotency pattern for command deduplication, Unit of Work pattern, and background processor for event publishing.

## Build and Tests

```bash
# Restore and build
dotnet restore
dotnet build

# Run all tests (unit + integration + architecture)
dotnet test

# Run the API
dotnet run --project src/UserManagement.API
```

## Tests

- **Unit tests (`UserManagement.UnitTests`)**  
  - `SmokeTests` uses **FluentAssertions** to verify that the test project loads and that the `UserManagement.Domain` assembly can be loaded via `AssemblyReference`.

- **Integration tests (`UserManagement.IntegrationTests`)**  
  - `HealthCheckTests` (FluentAssertions) validates that the API responds at the root (`/`) and at `/health`.  
  - `ArquitectureTests` uses **NetArchTest.Rules** to enforce architecture rules between projects (`Domain`, `Application`, `Infrastructure`, `API`) based on the `AssemblyReference` types of each layer.

- **Application unit tests (`UserManagement.Application.UnitTests`)**  
  - Unit tests for handlers (`RegisterUser`, `VerifyOtp`) using fakes.
  - Test for the `ValidationBehavior` pipeline (FluentValidation + MediatR).

## Configuration (.env)

Configuration is loaded from a **.env** file (DotNetEnv) before creating the host. Copy `.env.example` to `.env` inside `src/UserManagement.API/` and adjust the values.

- Use double underscore `__` for nested keys (e.g., `Logging__LogLevel__Default=Information`).
- Variables are injected into the environment before starting the host; the rest of the app uses `IConfiguration` as usual.
- `.env` is in `.gitignore`; secrets are not versioned.

## Dependency Injection

Services are registered by layers in `Program.cs`:

- **API**: `AddWebServices(IServiceCollection, IConfiguration)` in `UserManagement.API.DependencyInjection` (Swagger, HealthChecks, EndpointsApiExplorer).
- **Application**: `AddApplication(IServiceCollection, IConfiguration)` in `UserManagement.Application.DependencyInjection`.
- **Infrastructure**: `AddInfrastructure(IServiceCollection, IConfiguration)` in `UserManagement.Infrastructure.DependencyInjection`.

All three are invoked at startup in `Program.cs`; in each milestone, registrations will be added (MediatR, repositories, DbContext, etc.).
