# Milestone 01 — Setup (referencia para curso)

Documentación de lo implementado en el primer milestone: solución .NET 8, Clean Architecture, configuración por .env, inyección de dependencias por capas y tests de humo e integración.

---

## 1. Objetivo del milestone

- Crear la estructura de la solución (src + tests) con Clean Architecture.
- Configurar la API mínima con .env (DotNetEnv), Swagger y health check.
- Registrar servicios por capas (API, Application, Infrastructure) desde `Program.cs`.
- Tener tests unitarios y de integración que verifiquen que la solución compila, carga el Domain y que la API responde.

```bash
# (Referencia) Crear un nuevo proyecto Web API (solo si partes de cero)
# dotnet new webapi -n UserManagement.API -o src/UserManagement.API
---

## 2. Estructura de la solución

```
UserManagement.sln
├── src/
│   ├── UserManagement.Domain/            # Entidades, reglas de negocio (sin dependencias)
│   ├── UserManagement.Application/       # Casos de uso, interfaces (depende de Domain)
│   ├── UserManagement.Infrastructure/    # Persistencia, servicios externos (depende de Application)
│   └── UserManagement.API/               # Host ASP.NET Core, endpoints (depende de Application + Infrastructure)
└── tests/
    ├── UserManagement.UnitTests/         # Referencia a Domain
    └── UserManagement.IntegrationTests/  # Referencia a API (WebApplicationFactory)
```

**Dependencias entre proyectos (Clean Architecture):**

- **Domain**: sin referencias a otros proyectos.
- **Application**: referencia solo a Domain; expone interfaces para persistencia y servicios externos.
- **Infrastructure**: referencia a Application (implementa interfaces).
- **API**: referencia a Application e Infrastructure; no referencia Domain directamente para forzar uso de Application.

---

## 3. Proyectos y referencias

| Proyecto | Sdk | Referencias |
|----------|-----|-------------|
| UserManagement.Domain | Microsoft.NET.Sdk | — |
| UserManagement.Application | Microsoft.NET.Sdk | Domain + Microsoft.Extensions.Configuration.Abstractions + DependencyInjection.Abstractions |
| UserManagement.Infrastructure | Microsoft.NET.Sdk | Application + mismos paquetes MS |
| UserManagement.API | Microsoft.NET.Sdk.Web | Application, Infrastructure + DotNetEnv, Swashbuckle, Microsoft.AspNetCore.OpenApi |
| UserManagement.UnitTests | Microsoft.NET.Sdk | Domain + xunit, Microsoft.NET.Test.Sdk |
| UserManagement.IntegrationTests | Microsoft.NET.Sdk | API (alias `Api`) + Microsoft.AspNetCore.Mvc.Testing, xunit |

**Detalle importante (tests de integración):** el proyecto API se referencia con **alias `Api`** para poder usar `WebApplicationFactory<Program>` sin conflicto con la clase `Program` del test. Se usa `extern alias Api` y `using Program = Api::UserManagement.API.Program`.

---

## 4. Configuración (.env)

- La configuración se carga desde un archivo **.env** en `src/UserManagement.API/`.
- Se usa el paquete **DotNetEnv**: `DotNetEnv.Env.Load()` se llama al inicio de `Main`, antes de `WebApplication.CreateBuilder(args)`, para inyectar variables en el entorno. ASP.NET Core las lee luego vía `IConfiguration`.
- **Plantilla:** copiar `src/UserManagement.API/.env.example` a `src/UserManagement.API/.env` y ajustar valores.
- **Claves anidadas:** doble guión bajo `__` (ej. `Logging__LogLevel__Default=Information`).
- **Seguridad:** `.env` está en `.gitignore`; no se versionan secretos.

Variables típicas en `.env.example`:

- `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`
- Opcionales: `Logging__*`, `ConnectionStrings__*`, `AWS__*`, `SES__*`, `Cognito__*` (para milestones posteriores).

---

## 5. Punto de entrada: Program.cs

Flujo resumido:

1. **Cargar .env:** `DotNetEnv.Env.Load()` (busca `.env` en el directorio de trabajo del proceso).
2. **Crear el host:** `WebApplication.CreateBuilder(args)` (lee configuración desde entorno + appsettings si existe).
3. **Registrar servicios por capas:**
   - `AddApplication(services, configuration)`
   - `AddInfrastructure(services, configuration)`
   - `AddWebServices(services, configuration)` (Swagger, HealthChecks, EndpointsApiExplorer).
4. **Build:** `var app = builder.Build()`.
5. **Middleware:** en Development, `UseSwagger()` y `UseSwaggerUI()`; luego `UseHttpsRedirection()`.
6. **Endpoints:**
   - `GET /` → redirección a `/swagger`.
   - `GET /health` → health check.
7. **Ejecutar:** `app.Run()`.

La clase `Program` es pública y sin `ImplicitUsings`/top-level para que **WebApplicationFactory** pueda usarla en los tests de integración.

---

## 6. Dependency Injection por capas

Cada capa expone un método de extensión sobre `IServiceCollection`:

- **UserManagement.API.DependencyInjection.AddWebServices**
  - `AddEndpointsApiExplorer()`, `AddSwaggerGen()`, `AddHealthChecks()`.
- **UserManagement.Application.DependencyInjection.AddApplication**
  - Por ahora no registra nada; en siguientes milestones: MediatR, FluentValidation, etc.
- **UserManagement.Infrastructure.DependencyInjection.AddInfrastructure**
  - Por ahora no registra nada; en siguientes milestones: DbContext, repositorios, SES, Cognito, etc.

Todos reciben `IConfiguration` para poder leer configuración cuando se añadan servicios que la necesiten.

---

## 7. Endpoints y middleware

| Ruta | Comportamiento |
|------|----------------|
| `GET /` | Redirección HTTP a `/swagger`. |
| `GET /health` | Health check (ASP.NET Core); responde 200 si el servicio está vivo. |
| Swagger | Disponible en Development en `/swagger` y `/swagger/v1/swagger.json`. |

---

## 8. Tests

### 8.1 Unit tests (UserManagement.UnitTests)

- **SmokeTests.Solution_compiles_and_UnitTests_project_loads:** comprueba, usando **FluentAssertions**, que el proyecto de tests carga correctamente.
- **SmokeTests.Domain_assembly_can_be_loaded:** comprueba, también con FluentAssertions, que se puede cargar el assembly `UserManagement.Domain` a través del tipo marcador `UserManagement.Domain.AssemblyReference`.

Objetivo: verificar que la solución compila y que las referencias al Domain funcionan.

### 8.2 Integration tests (UserManagement.IntegrationTests)

- **HealthCheckTests:** usa `IClassFixture<WebApplicationFactory<Program>>` para levantar la API en memoria y **FluentAssertions** para verificar las respuestas.
  - **Root_endpoint_returns_OK:** `GET /` → `EnsureSuccessStatusCode()` y cuerpo HTML que contiene `"Swagger UI"`, usando `body.Should().Contain("Swagger UI")` (la raíz redirige a `/swagger`).
  - **Health_endpoint_returns_Healthy:** `GET /health` → 200 (`response.StatusCode.Should().Be(HttpStatusCode.OK)`).
- **ArquitectureTests:** usa **NetArchTest.Rules** + FluentAssertions para validar las dependencias entre capas:
  - `domain_Should_not_HaveDependenciesOnOtherProjects` (Domain no depende de Application/Infrastructure/API).
  - `application_Should_not_HaveDependenciesOnOtherProjects` (Application no depende de Infrastructure/API).
  - `infrastructure_Should_not_HaveDependenciesOnOtherProjects` (Infrastructure no depende de API).
  - `api_Should_only_depend_on_Infrastructure_in_CompositionRoot` (la API solo puede depender de Infrastructure desde el composition root: `Program`/`DependencyInjection`/`AssemblyReference`), usando los tipos `AssemblyReference` de cada proyecto.

---

## 9. Paquetes NuGet por proyecto

- **Domain:** ninguno (solo SDK).
- **Application:** Microsoft.Extensions.Configuration.Abstractions, Microsoft.Extensions.DependencyInjection.Abstractions.
- **Infrastructure:** mismos que Application.
- **API:** DotNetEnv, Microsoft.AspNetCore.OpenApi, Swashbuckle.AspNetCore. Referencia a Application e Infrastructure. `InternalsVisibleTo` hacia UserManagement.IntegrationTests.
- **UnitTests:** xunit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, **FluentAssertions**. Referencia a Domain.
- **IntegrationTests:** Microsoft.AspNetCore.Mvc.Testing, xunit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, **FluentAssertions**, **NetArchTest.Rules**. Referencia al proyecto API con alias `Api`.

---

## 10. Comandos de verificación

```bash

# Restaurar y compilar
dotnet restore
dotnet build

# Tests
dotnet test
# O por proyecto:
dotnet test tests/UserManagement.UnitTests
dotnet test tests/UserManagement.IntegrationTests

# Ejecutar la API
dotnet run --project src/UserManagement.API
```

Comprobar en el navegador:

- https://localhost:7027/ o http://localhost:5042/ (redirección a `/swagger`).
- https://localhost:7027/health (o el puerto configurado en `.env` / launchSettings).

---

## 11. Archivos clave (referencia rápida)

| Archivo | Propósito |
|---------|-----------|
| `UserManagement.sln` | Solución con carpetas src/ y tests/. |
| `src/UserManagement.API/Program.cs` | Entrada, carga .env, registro DI, middleware, endpoints. |
| `src/UserManagement.API/DependencyInjection.cs` | AddWebServices (Swagger, HealthChecks). |
| `src/UserManagement.Application/DependencyInjection.cs` | AddApplication (vacío en M01). |
| `src/UserManagement.Infrastructure/DependencyInjection.cs` | AddInfrastructure (vacío en M01). |
| `src/UserManagement.Domain/AssemblyReference.cs` | Tipo marcador para cargar el assembly Domain en tests de unidad e integración. |
| `src/UserManagement.Application/AssemblyReference.cs` | Tipo marcador para cargar el assembly Application en tests de arquitectura. |
| `src/UserManagement.Infrastructure/AssemblyReference.cs` | Tipo marcador para cargar el assembly Infrastructure en tests de arquitectura. |
| `src/UserManagement.API/AssemblyReference.cs` | Tipo marcador para cargar el assembly API en tests de arquitectura (referenciado con alias `Api`). |
| `src/UserManagement.API/.env.example` | Plantilla de variables de entorno. |
| `src/UserManagement.API/Properties/launchSettings.json` | Perfiles de ejecución (http/https), URLs, ASPNETCORE_ENVIRONMENT. |
| `tests/UserManagement.UnitTests/SmokeTests.cs` | Smoke tests y carga del Domain. |
| `tests/UserManagement.IntegrationTests/HealthCheckTests.cs` | Tests de / y /health con WebApplicationFactory. |
| `.gitignore` | Incluye `.env`, `bin/`, `obj/`, etc. |

---

## 12. Notas para el curso

- **Clean Architecture:** las dependencias apuntan hacia dentro (Domain en el centro). La API orquesta Application e Infrastructure y no conoce detalles de persistencia.
- **.env:** evita secretos en appsettings y facilita distintos entornos; se carga antes del host para que `IConfiguration` ya tenga las variables.
- **DI por capas:** un método de extensión por capa mantiene `Program.cs` limpio y deja claro qué registra cada parte (API, Application, Infrastructure).
- **WebApplicationFactory:** permite tests de integración contra la API real sin desplegar; el alias `Api` evita colisiones con la clase `Program` del test.
- **Milestone 01** deja la base lista para añadir en siguientes hitos: entidades en Domain, CQRS/MediatR en Application, EF Core y repositorios en Infrastructure, y endpoints en la API.
- **xUnit + FluentAssertions:** stack de pruebas para unit e integration tests; xUnit como framework de pruebas y FluentAssertions para aserciones legibles y expresivas.

