# Milestone 03 — Application Core (referencia para curso)

Documentación de lo implementado en el tercer milestone: Application Core con CQRS (MediatR), FluentValidation con pipeline behavior, Result pattern, feature flags, abstracciones (ports) y vertical slices para el caso de uso de usuarios, junto con tests unitarios e integración mínima para arrancar la API sin Infrastructure real.

---

## 1. Objetivo del milestone

- Implementar la capa **Application** siguiendo Clean Architecture (sin dependencias hacia API/Infrastructure).
- Introducir **CQRS** con **MediatR** (Commands/Queries + Handlers).
- Implementar validación con **FluentValidation** y un **pipeline behavior** para no repetir validaciones en cada handler.
- Implementar **Result pattern** (`Result` / `Result<T>` + `Error`) para devolver errores controlados sin excepciones.
- Incorporar **Feature Flags** (`FeatureFlags:EnableOtp`) para habilitar/deshabilitar el flujo OTP.
- Definir abstracciones (ports) para integraciones externas:
  - Email
  - Generación OTP
  - Tiempo (DateTime)
  - Cognito
- Implementar vertical slices del módulo Users:
  - `RegisterUser`
  - `VerifyOtp`
  - `SyncUser`
  - `GetUsers`
- Agregar tests:
  - Unit tests de Application con fakes.
  - Integration tests con stubs para que la API arranque sin implementaciones reales.

---

## 2. Estructura (Application)

```
src/UserManagement.Application/
├── Common/
│   ├── Abstractions/
│   │   ├── ICognitoIdentityService.cs
│   │   ├── IDateTimeProvider.cs
│   │   ├── IEmailSender.cs
│   │   └── IOtpGenerator.cs
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Options/
│   │   └── FeatureFlagsOptions.cs
│   ├── Pagination/
│   │   └── PagedResult.cs
│   └── Results/
│       ├── Error.cs
│       ├── Result.cs
│       └── ResultT.cs
├── Features/
│   └── Users/
│       ├── Commands/
│       │   ├── RegisterUser/
│       │   │   ├── RegisterUserCommand.cs
│       │   │   ├── RegisterUserHandler.cs
│       │   │   └── RegisterUserValidator.cs
│       │   ├── SyncUser/
│       │   │   ├── SyncUserCommand.cs
│       │   │   ├── SyncUserHandler.cs
│       │   │   └── SyncUserValidator.cs
│       │   └── VerifyOtp/
│       │       ├── VerifyOtpCommand.cs
│       │       ├── VerifyOtpHandler.cs
│       │       └── VerifyOtpValidator.cs
│       ├── Models/
│       │   └── UserDto.cs
│       └── Queries/
│           └── GetUsers/
│               ├── GetUsersQuery.cs
│               ├── GetUsersHandler.cs
│               └── GetUsersValidator.cs
├── AssemblyReference.cs
├── DependencyInjection.cs
└── UserManagement.Application.csproj
```

---

## 3. Elementos principales

### 3.1 Paquetes

Paquetes agregados en `UserManagement.Application`:

- `MediatR`
- `FluentValidation`
- `FluentValidation.DependencyInjectionExtensions`
- `Microsoft.Extensions.Options.ConfigurationExtensions`

---

### 3.2 Result pattern (Common/Results)

Se incorpora un patrón de resultados para que comandos/queries retornen:

- `Result`: éxito o error.
- `Result<T>`: éxito con payload (`T`) o error.
- `Error`: describe el error con:
  - `Code`
  - `Message`
  - `ValidationErrors` (opcional) para errores de validación.

Códigos de error disponibles (según `Error.Codes`):

- `Validation`
- `NotFound`
- `Conflict`
- `OtpInvalid`
- `OtpExpired`
- `FeatureDisabled`
- `Unexpected`

---

### 3.3 Feature flags (Common/Options)

- `FeatureFlagsOptions` contiene el flag `EnableOtp`.
- Se bindea desde configuración bajo la sección `FeatureFlags`.

Uso principal:

- Habilitar/inhabilitar flujo OTP en `RegisterUser` y `VerifyOtp`.

---

### 3.4 Abstracciones (Common/Abstractions)

Interfaces definidas en Application para desacoplar integraciones externas:

- `IEmailSender`: envío de emails (OTP).
- `IOtpGenerator`: generación de códigos OTP.
- `IDateTimeProvider`: acceso al tiempo actual (testable).
- `ICognitoIdentityService`: operaciones contra Cognito (crear/obtener/validar identidad).

**Nota**: Las implementaciones concretas se agregarán en el milestone 04 (Infrastructure).

---

### 3.5 DI y pipeline (DependencyInjection + ValidationBehavior)

`AddApplication(...)` registra:

- MediatR (handlers desde el assembly de Application).
- FluentValidation (validators desde el mismo assembly).
- `ValidationBehavior<,>` como `IPipelineBehavior<,>`.
- Configuración de `FeatureFlagsOptions` desde `IConfiguration`.

`ValidationBehavior`:

- Ejecuta todos los validators aplicables al request.
- Si hay errores, devuelve `Result` / `Result<T>` con `Error.Validation(...)` y `ValidationErrors`.
- Evita duplicación de validación en cada handler.

---

## 4. CQRS (vertical slices) — Features/Users

Estructura escalable aplicada al módulo Users:

- `Features/Users/Commands/*`: Commands (write side) organizados por caso de uso.
- `Features/Users/Queries/*`: Queries (read side) organizadas por caso de uso.

Namespaces asociados:

- `UserManagement.Application.Features.Users.Commands.*`
- `UserManagement.Application.Features.Users.Queries.*`

### 4.1 RegisterUser

- **Command**: inicia el registro de usuario.
- **Validación**: valida email y datos obligatorios.
- **Handler**:
  - Evalúa `FeatureFlags:EnableOtp`.
  - Maneja duplicados (conflicto).
  - Crea el usuario usando `UserFactory`.
  - Genera y persiste OTP.
  - Envía email con OTP usando `IEmailSender`.

Resultados típicos:

- `Success`: usuario registrado (pendiente de verificación) y OTP enviado.
- `Failure(Conflict)`: ya existe usuario.
- `Failure(FeatureDisabled)`: OTP deshabilitado.

---

### 4.2 VerifyOtp

- **Command**: verifica OTP y activa el usuario.
- **Validación**: valida email y code.
- **Handler**:
  - Obtiene OTP por email+code.
  - Verifica validez (no usado / no expirado).
  - Marca OTP como usado.
  - Activa usuario.
  - Opera contra Cognito mediante `ICognitoIdentityService`.
  - Persiste cambios.

Resultados típicos:

- `Success`: usuario activado.
- `Failure(OtpInvalid)`: OTP inexistente o inválido.
- `Failure(OtpExpired)`: OTP expirado.

---

### 4.3 SyncUser

- **Command**: sincroniza (upsert) un usuario desde Cognito.
- **Validación**: valida parámetros de identidad.
- **Handler**:
  - Busca por `CognitoSub` o email.
  - Si no existe, crea usuario desde Cognito.
  - Si existe, actualiza información relevante.

---

### 4.4 GetUsers

- **Query**: retorna usuarios paginados.
- **Validación**: parámetros de paging.
- **Handler**:
  - Obtiene usuarios desde repositorio.
  - Aplica `UsersPaginatedSpec`.
  - Mapea a `UserDto`.
  - Retorna `PagedResult<UserDto>`.

---

## 5. Tests

### 5.1 Unit tests (UserManagement.Application.UnitTests)

Estructura:

```
tests/UserManagement.Application.UnitTests/
├── Common/
├── Fakes/
├── RegisterUser/
└── VerifyOtp/
```

Se usan fakes para:

- Repositorios
- Envío de email
- Generación OTP
- Proveedor de fecha/hora
- Cognito

Cobertura destacada:

- `RegisterUser`
  - Conflict (duplicado)
  - Éxito + verificación de envío de email
  - FeatureDisabled
- `VerifyOtp`
  - OtpInvalid
  - OtpExpired
- `ValidationBehavior`
  - Retorna `ValidationErrors` cuando falla FluentValidation

---

### 5.2 Integration tests (UserManagement.IntegrationTests)

- `CustomWebApplicationFactory` + `Stubs/` permiten levantar la API sin implementaciones reales en Infrastructure.
- `HealthCheckTests` validan que la API responde en `/` y `/health`.
- `ArquitectureTests` mantienen reglas de referencia entre capas.

---

## 6. Verificación del milestone

### 6.1 Compilación

```bash
# Compilar toda la solución
dotnet build
```

### 6.2 Tests

```bash
# Ejecutar todos los tests (unit + integración + arquitectura)
dotnet test
```

### 6.3 Verificaciones manuales

- ✅ Application compila y registra MediatR + validators + pipeline.
- ✅ Commands/Queries retornan `Result` / `Result<T>`.
- ✅ Feature flag `EnableOtp` afecta el flujo OTP.
- ✅ Tests unitarios e integración pasan.
- ✅ Reglas de arquitectura se mantienen (Application sin dependencias hacia Infrastructure/API).

---

## 7. Próximos pasos (Milestone 04)

En el milestone 04 (Infrastructure) se implementará:

- Persistencia real (EF Core + repositorios).
- Providers concretos de`IDateTimeProvider`.
- Integration tests con Testcontainers MySQL.

---

## 8. Archivos clave creados

| Archivo | Propósito |
|---------|-----------|
| `Common/Results/Error.cs` | Error application-level (code/message/validation details) |
| `Common/Results/Result.cs` | Result sin payload (Success/Failure) |
| `Common/Results/ResultT.cs` | Result con payload (`Result<T>`) |
| `Common/Behaviors/ValidationBehavior.cs` | Pipeline de validación (FluentValidation) |
| `Common/Options/FeatureFlagsOptions.cs` | Feature flags (`EnableOtp`) |
| `Common/Abstractions/*` | Ports para integraciones externas |
| `DependencyInjection.cs` | Registro de MediatR/Validators/Pipeline/Options |
| `Features/Users/Commands/*` | Write side (Commands) del módulo Users |
| `Features/Users/Queries/*` | Read side (Queries) del módulo Users |
| `tests/UserManagement.Application.UnitTests/*` | Tests unitarios de Application con fakes |
| `tests/UserManagement.IntegrationTests/*` | Factory/stubs para levantar API + health/architecture tests |

---

## 9. Comandos de verificación

```bash
# Compilar
dotnet build

# Ejecutar tests
dotnet test
```

---

## 10. Referencias

- **Milestone 01**: Setup de la solución y estructura base.
- **Milestone 02**: Dominio (DDD) con entidades, VOs, events, factories, repos y specifications.
