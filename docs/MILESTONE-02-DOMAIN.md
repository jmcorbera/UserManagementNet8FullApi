# Milestone 02 — Domain (referencia para curso)

Documentación de lo implementado en el segundo milestone: dominio completo con entidades, value objects, domain events, factory, repositories interfaces y specifications.

---

## 1. Objetivo del milestone

- Implementar el dominio completo siguiendo DDD (Domain-Driven Design).
- Crear el Aggregate Root `User` con sus reglas de negocio.
- Implementar Value Objects (`Email`).
- Crear entidades de dominio (`UserOtp`).
- Definir Domain Events (`UserRegistrationRequested`, `UserVerified`).
- Implementar Factory pattern (`UserFactory`).
- Definir interfaces de repositorios (`IUserRepository`, `IUserOtpRepository`).
- Crear Specifications para consultas y filtros.
- Verificar con tests unitarios que el dominio funciona correctamente.

---

## 2. Estructura del dominio

```
src/UserManagement.Domain/
├── Common/
│   ├── BaseEntity.cs                      # Entidad base con Id
│   ├── BaseAuditableEntity.cs             # Auditoría (Created/LastModified + *By)
│   └── DomainException.cs                 # Excepción de dominio genérica (ArgumentException)
├── Entities/
│   ├── User.cs                            # Aggregate Root
│   └── UserOtp.cs                         # Entidad para códigos OTP
├── Enums/
│   └── UserStatus.cs                      # Enum: PendingVerification, Active
├── Events/
│   ├── IDomainEvent.cs                    # Interfaz base para eventos
│   ├── UserRegistrationRequested.cs       # Evento: registro solicitado (OTP generado)
│   └── UserVerified.cs                    # Evento: usuario verificado y activado
├── Factories/
│   └── UserFactory.cs                     # Factory para crear usuarios
├── Repositories/
│   ├── IUserRepository.cs                 # Interfaz repositorio User
│   └── IUserOtpRepository.cs              # Interfaz repositorio UserOtp
├── Specifications/
│   ├── ISpecification.cs                  # Interfaz base para specifications
│   ├── UserByEmailSpec.cs                 # Buscar usuario por email
│   ├── ActiveUsersSpec.cs                 # Filtrar usuarios activos
│   ├── UserPendingVerificationSpec.cs     # Usuarios pendientes de verificación
│   └── UsersPaginatedSpec.cs              # Paginación con filtros opcionales
└── ValueObjects/
    └── Email.cs                           # Value Object con validación
```

---

## 3. Elementos del dominio

### 3.1 UserStatus (Enum)

```csharp
public enum UserStatus
{
    PendingVerification = 0,  // Usuario registrado pero pendiente de verificación (OTP)
    Active = 1                // Usuario activo y verificado
}
```

**Uso**: Representa el estado del usuario en el sistema.

---

### 3.2 Email (Value Object)

**Características**:
- Validación de formato de email mediante regex.
- Normalización a minúsculas y trim.
- Inmutabilidad (solo lectura después de creación).
- Igualdad por valor (al ser `record`).
- Conversión implícita a `string`.

**Métodos**:
- `Create(string value)`: Factory method estático que valida y crea el email.
- `Equals`, `GetHashCode`, `ToString`: Implementaciones estándar.

**Validaciones**:
- No puede ser null, vacío o solo espacios.
- Debe tener formato de email válido.

**Ejemplo**:
```csharp
var email = Email.Create("Test@Example.COM"); // Normaliza a "test@example.com"
```

---

### 3.3 User (Aggregate Root)

**Propiedades**:
- `Id` (Guid): Identificador único.
- `Email` (Email): Value Object de email.
- `Name` (string): Nombre del usuario.
- `Status` (UserStatus): Estado del usuario.
- `CognitoSub` (string?): Sub de Cognito (nullable hasta que se cree en Cognito).
- Auditoría (heredada de `BaseAuditableEntity<Guid>`):
  - `Created`, `CreatedBy`
  - `LastModified`, `LastModifiedBy`
- `IsDeleted` (bool): Soft delete flag.

**Métodos de dominio**:
- `Activate()`: Cambia el estado a `Active`.
- `SetCognitoSub(string cognitoSub)`: Asigna el CognitoSub después de crear en Cognito.
- `Delete()`: Realiza soft delete.
- `UpdateName(string name)`: Actualiza el nombre.

**Factory methods estáticos**:
- `CreatePending(Guid id, Email email, string name)`: Crea usuario pendiente de verificación.
- `FromCognito(Guid id, Email email, string name, string cognitoSub)`: Crea usuario desde Cognito (sync).

**Reglas de negocio**:
- No se puede activar dos veces (idempotente).
- No se puede eliminar dos veces (idempotente).
- El CognitoSub no puede ser null o vacío cuando se asigna.
- El nombre no puede ser null o vacío cuando se actualiza.

---

### 3.4 UserOtp (Entidad)

**Propiedades**:
- `Id` (Guid): Identificador único.
- `Email` (Email): Email asociado al OTP.
- `Code` (string): Código OTP.
- `ExpiresAt` (DateTimeOffset): Fecha de expiración.
- `Used` (bool): Indica si el OTP ya fue usado.
- `CreatedAt` (DateTimeOffset): Timestamp de creación.

**Métodos**:
- `Create(Guid id, Email email, string code, TimeSpan validFor)`: Crea un OTP con expiración calculada.
- `MarkAsUsed()`: Marca el OTP como usado (lanza `InvalidOperationException` si ya estaba usado).
- `IsValid() / IsValid(DateTimeOffset now)`: Verifica si el OTP es válido (no usado y no expirado).

**Reglas de negocio**:
- Un OTP no puede usarse dos veces.
- Un OTP expirado no es válido.

---

### 3.5 Domain Events

**IDomainEvent**:
- Interfaz marcadora para todos los eventos de dominio.
- Propiedad `OccurredAt` (DateTime).

**UserRegistrationRequested**:
- Se publica cuando se solicita el registro de un usuario (OTP generado).
- Contiene: `UserId`, `Email`, `Name`, `OtpCode`, `OccurredAt`.

**UserVerified**:
- Se publica cuando un usuario es verificado y activado (OTP validado, Cognito creado).
- Contiene: `UserId`, `Email`, `CognitoSub`, `OccurredAt`.

**Nota**: Los eventos se publicarán en el milestone 05 (Outbox) cuando se implemente el patrón Outbox.

---

### 3.6 UserFactory

**Métodos estáticos**:
- `CreatePending(Email email, string name)`: Crea usuario pendiente de verificación con ID generado automáticamente.
- `FromCognito(Email email, string name, string cognitoSub)`: Crea usuario activo desde Cognito con ID generado automáticamente.

**Uso**: Facilita la creación de usuarios sin exponer los constructores internos del agregado.

---

### 3.7 Interfaces de Repositorios

**IUserRepository**:
- `GetByIdAsync(Guid id)`: Obtiene usuario por ID.
- `GetByEmailAsync(Email email)`: Obtiene usuario por email.
- `GetByCognitoSubAsync(string cognitoSub)`: Obtiene usuario por CognitoSub.
- `GetAllAsync()`: Obtiene todos los usuarios.
- `AddAsync(User user)`: Agrega un usuario.
- `UpdateAsync(User user)`: Actualiza un usuario.
- `ExistsByEmailAsync(Email email)`: Verifica si existe un usuario con ese email.

**IUserOtpRepository**:
- `GetByEmailAndCodeAsync(Email email, string code)`: Obtiene OTP por email y código.
- `GetLatestByEmailAsync(Email email)`: Obtiene el último OTP generado para un email.
- `AddAsync(UserOtp otp)`: Agrega un OTP.
- `UpdateAsync(UserOtp otp)`: Actualiza un OTP (ej. marcar como usado).

**Nota**: Las implementaciones concretas se crearán en el milestone 04 (Infrastructure).

---

### 3.8 Specifications

**ISpecification<T>**:
- Interfaz base para el patrón Specification.
- Método `IsSatisfiedBy(T entity)`: Evalúa si una entidad cumple la especificación.

**UserByEmailSpec**:
- Busca un usuario por email (excluye eliminados).

**ActiveUsersSpec**:
- Filtra usuarios activos (excluye eliminados).

**UserPendingVerificationSpec**:
- Busca usuarios pendientes de verificación por email (excluye eliminados).

**UsersPaginatedSpec**:
- Especificación para paginación con filtros opcionales:
  - `statusFilter` (UserStatus?): Filtro opcional por estado.
  - `includeDeleted` (bool): Si incluir usuarios eliminados (por defecto false).

**Uso**: Las specifications se usarán en queries y repositorios para encapsular lógica de filtrado.

---

## 4. Tests unitarios

### 4.1 Estructura de tests

```
tests/UserManagement.UnitTests/Domain/
├── UserFactoryTests.cs
├── EmailTests.cs
├── UserTests.cs
├── UserOtpTests.cs
└── Specifications/
    ├── UserByEmailSpecTests.cs
    ├── ActiveUsersSpecTests.cs
    ├── UserPendingVerificationSpecTests.cs
    └── UsersPaginatedSpecTests.cs
```

### 4.2 Cobertura de tests

**UserFactoryTests**:
- `CreatePending_Should_Create_User_With_PendingVerification_Status`
- `FromCognito_Should_Create_User_With_Active_Status_And_CognitoSub`

**EmailTests**:
- Validación de emails válidos.
- Validación de emails inválidos (null, vacío, formato incorrecto).
- Normalización a minúsculas.
- Comparación de igualdad.
- Conversión implícita a string.

**UserTests**:
- `Activate()`: Cambio de estado a Active.
- `SetCognitoSub()`: Asignación de CognitoSub.
- `Delete()`: Soft delete.
- `UpdateName()`: Actualización de nombre.
- Validaciones de parámetros inválidos.

**UserOtpTests**:
- `IsValid()`: Validación de OTP (no usado y no expirado).
- `MarkAsUsed()`: Marcado como usado.
- Validación de OTP ya usado.

**Specifications Tests**:
- Cada specification tiene tests que verifican:
  - Casos positivos (cumple la especificación).
  - Casos negativos (no cumple).
  - Manejo de usuarios eliminados (soft delete).

---

## 5. Verificación del milestone

### 5.1 Compilación

```bash
# Compilar el proyecto Domain
dotnet build src/UserManagement.Domain

# Compilar todos los proyectos
dotnet build
```

### 5.2 Tests unitarios

```bash
# Ejecutar tests del dominio
dotnet test tests/UserManagement.UnitTests --filter "FullyQualifiedName~Domain"

# Ejecutar todos los tests unitarios
dotnet test tests/UserManagement.UnitTests
```

### 5.3 Verificaciones manuales

- ✅ El proyecto Domain compila sin errores.
- ✅ Todos los tests unitarios pasan.
- ✅ Las entidades respetan las reglas de negocio.
- ✅ Los Value Objects validan correctamente.
- ✅ Las Specifications funcionan como se espera.

---

## 6. Próximos pasos (Milestone 03)

En el milestone 03 (Application Core) se implementará:
- Result Pattern para manejo de errores.
- MediatR para CQRS.
- FluentValidation para validación de comandos/queries.
- Commands y Queries (RegisterUser, VerifyOtp, SyncUser, GetUsers).
- Handlers para cada comando/query.
- FeatureFlags (EnableOtp).
- Interfaces de servicios (IEmailSender) en Application.

---

## 7. Notas para el curso

- **DDD**: El dominio está completamente aislado, sin dependencias externas.
- **Value Objects**: `Email` es inmutable y encapsula validación.
- **Aggregate Root**: `User` controla todas las operaciones sobre el agregado.
- **Domain Events**: Preparados para ser publicados vía Outbox en milestone 05.
- **Specifications**: Encapsulan lógica de consulta reutilizable.
- **Factory**: Facilita la creación de entidades sin exponer constructores internos.
- **Soft Delete**: Implementado en `User` para mantener integridad referencial.
- **Excepciones de dominio**: `DomainException` es genérica y permite mensajes específicos según el ámbito de validación.
- **Tests**: Cobertura completa del dominio con FluentAssertions para aserciones legibles.

---

## 8. Archivos clave creados

| Archivo | Propósito |
|---------|-----------|
| `UserStatus.cs` | Enum de estados del usuario |
| `Email.cs` | Value Object con validación |
| `User.cs` | Aggregate Root con reglas de negocio |
| `UserOtp.cs` | Entidad para códigos OTP |
| `UserFactory.cs` | Factory para crear usuarios |
| `IDomainEvent.cs` | Interfaz base para eventos |
| `UserRegistrationRequested.cs` | Evento de registro solicitado |
| `UserVerified.cs` | Evento de usuario verificado |
| `IUserRepository.cs` | Interfaz repositorio User |
| `IUserOtpRepository.cs` | Interfaz repositorio UserOtp |
| `ISpecification.cs` | Interfaz base para specifications |
| `UserByEmailSpec.cs` | Specification: usuario por email |
| `ActiveUsersSpec.cs` | Specification: usuarios activos |
| `UserPendingVerificationSpec.cs` | Specification: usuarios pendientes |
| `UsersPaginatedSpec.cs` | Specification: paginación con filtros |

---

## 9. Comandos de verificación

```bash
# Compilar solo Domain
dotnet build src/UserManagement.Domain

# Ejecutar tests del dominio
dotnet test tests/UserManagement.UnitTests --filter "FullyQualifiedName~Domain"

# Ejecutar todos los tests
dotnet test
```

---

## 10. Referencias

- **Milestone 01**: Setup de la solución y estructura base.
- **Plan original**: `user_management_api_cognito_bd315254.plan.md` (sección Domain).
- **DDD**: Domain-Driven Design patterns aplicados (Aggregate Root, Value Objects, Domain Events, Specifications, Factory).
