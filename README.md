# User Management API

REST API (.NET 8) para gestión de usuarios con registro (OTP + Cognito), Clean Architecture y CQRS.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Documentación por milestone

- **[Milestone 01 — Setup](docs/MILESTONE-01-SETUP.md)** — Referencia detallada del primer milestone (estructura, .env, DI, tests) para uso en curso.
- **[Milestone 02 — Domain](docs/MILESTONE-02-DOMAIN.md)** — Implementación completa del dominio (entidades, value objects, domain events, factory, repositories interfaces, specifications).

## Estructura (milestone 02-domain)

```
src/
  UserManagement.Domain/          # ✅ Domain completo (User, Email, UserOtp, Events, Factory, Repos, Specs)
  UserManagement.Application/      # Próximo: CQRS, MediatR, Commands/Queries
  UserManagement.Infrastructure/  # Próximo: EF Core, Repositorios, Cognito, SES
  UserManagement.API/              # Próximo: Controllers, DTOs, Middleware
tests/
  UserManagement.UnitTests/        # ✅ Tests del dominio (UserFactory, Email, Specifications)
  UserManagement.IntegrationTests/
```

## Build y tests

```bash
# Restaurar y compilar
dotnet restore
dotnet build

# Ejecutar todos los tests (unit + integración + arquitectura)
dotnet test

# Ejecutar la API
dotnet run --project src/UserManagement.API
```

## Pruebas

- **Unit tests (`UserManagement.UnitTests`)**  
  - `SmokeTests` usa **FluentAssertions** para comprobar que el proyecto de tests carga y que el ensamblado `UserManagement.Domain` puede cargarse vía `AssemblyReference`.

- **Integration tests (`UserManagement.IntegrationTests`)**  
  - `HealthCheckTests` (FluentAssertions) valida que la API responde en la raíz (`/`) y en `/health`.  
  - `ArquitectureTests` usa **NetArchTest.Rules** para garantizar las reglas de arquitectura entre proyectos (`Domain`, `Application`, `Infrastructure`, `API`) basadas en los tipos `AssemblyReference` de cada capa.

## Verificación milestone 02-domain

- La solución compila: `dotnet build`
- Domain compila: `dotnet build src/UserManagement.Domain`
- Unit tests del dominio: `dotnet test tests/UserManagement.UnitTests --filter "FullyQualifiedName~Domain"` (UserFactory, Email, User, UserOtp, Specifications)
- Todos los tests: `dotnet test`

## Configuración (.env)

La configuración se carga desde un archivo **.env** (DotNetEnv) antes de crear el host. Copia `.env.example` a `.env` dentro de `src/UserManagement.API/` y ajusta los valores.

- Usa doble guión bajo `__` para claves anidadas (ej. `Logging__LogLevel__Default=Information`).
- Las variables se inyectan en el entorno antes de arrancar el host; el resto de la app usa `IConfiguration` como siempre.
- `.env` está en `.gitignore`; no se versionan secretos.

## Dependency Injection

Los servicios se registran por capas en `Program.cs`:

- **API**: `AddWebServices(IServiceCollection, IConfiguration)` en `UserManagement.API.DependencyInjection` (Swagger, HealthChecks, EndpointsApiExplorer).
- **Application**: `AddApplication(IServiceCollection, IConfiguration)` en `UserManagement.Application.DependencyInjection`.
- **Infrastructure**: `AddInfrastructure(IServiceCollection, IConfiguration)` en `UserManagement.Infrastructure.DependencyInjection`.

Los tres se invocan al arranque en `Program.cs`; en cada milestone se irán añadiendo registros (MediatR, repositorios, DbContext, etc.).
