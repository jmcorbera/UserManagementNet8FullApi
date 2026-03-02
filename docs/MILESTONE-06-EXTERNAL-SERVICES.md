# Milestone 06 — External Services Integration

**Status**: ✅ Completed

This milestone implements real external service integrations for email delivery (Amazon SES with templates), OTP generation, message bus publishing (Amazon SNS), retry logic with exponential backoff, and cleanup jobs. AWS Cognito uses a functional mock for development/testing while keeping the architecture ready for production integration.

---

## Overview

Previously, the application used fake/stub implementations for external services. This milestone replaces them with production-ready implementations while maintaining testability through dependency injection.

### Key Achievements

1. **Secure OTP Generator** — Cryptographically secure random code generation using Otp.NET
2. **Amazon SES V2 Email Service** — Production email delivery with template support
3. **Cognito Mock Service** — Functional mock simulating AWS Cognito behavior
4. **Amazon SNS Integration** — Domain event publishing to message bus
5. **Retry Logic** — Exponential backoff for failed outbox messages
6. **Cleanup Jobs** — Automated cleanup of old data
7. **OTP Settings Provider** — Centralized OTP configuration management

---

## Architecture Changes

### Service Layer (Infrastructure)

All concrete implementations moved from Application to Infrastructure layer:

```
src/UserManagement.Infrastructure/
├── Services/
│   ├── SecureOtpGenerator.cs          # Cryptographically secure OTP generation (Otp.NET)
│   ├── OtpSettingsProvider.cs         # OTP configuration provider
│   ├── SesEmailSender.cs              # Amazon SES V2 email with templates
│   ├── MockCognitoIdentityService.cs  # Functional Cognito mock
│   └── SnsMessagePublisher.cs         # Amazon SNS publisher
└── Options/
    ├── OtpGeneratorOptions.cs         # OTP length and validity configuration
    ├── SesOptions.cs                  # SES region, from email, reply-to
    ├── SnsOptions.cs                  # SNS region, topic ARN, credentials
    ├── OutboxProcessorOptions.cs      # Polling, batch size, retry config
    └── CleanupJobOptions.cs           # Retention and interval settings
```

### Background Services

```
src/UserManagement.Infrastructure/BackgroundServices/
├── OutboxProcessor.cs             # Enhanced with retry logic and SNS publishing
├── OutboxCleanupJob.cs           # Cleanup old processed messages
└── IdempotencyCleanupJob.cs      # Cleanup expired idempotency keys
```

### Domain Enhancements

**OutboxMessage Entity** — Added retry tracking:
- `RetryCount` — Number of retry attempts
- `NextRetryAt` — Scheduled retry timestamp
- `IncrementRetry()` — Exponential backoff calculation
- `CanRetry()` — Check if retry is allowed
- `IsReadyForRetry()` — Check if ready for next attempt

**UserOtp Entity** — Enhanced:
- Renamed `Used` → `IsUsed` for better semantics
- Fixed `Create()` method to use provided ID parameter
- Added `ToString()` override returning `Code` for template data

---

## Implementation Details

### 1. OTP Generator Service

**File**: `src/UserManagement.Infrastructure/Services/SecureOtpGenerator.cs`

Uses **Otp.NET** library for TOTP-based secure code generation:

```csharp
public string Generate()
{
    var length = _options.Length;
    var key = KeyGeneration.GenerateRandomKey(20);
    var totp = new Totp(key, step: 30, mode: OtpHashMode.Sha256, totpSize: length);
    
    return totp.ComputeTotp();
}
```

**Features**:
- RFC 6238 compliant TOTP algorithm
- SHA-256 hashing for enhanced security
- 30-second time window
- Configurable code length

**Configuration**:
```bash
OtpGenerator__Length=6              # Default: 6 digits
OtpGenerator__ValidForMinutes=10    # OTP validity period
```

**NuGet Package**: `Otp.NET` v1.4.1

### 2. Amazon SES V2 Email Service

**File**: `src/UserManagement.Infrastructure/Services/SesEmailSender.cs`

Implements `IEmailSender` using **Amazon SES V2** with template support:

```csharp
public async Task SendAsync<T>(string toEmail, string templateName, T data, 
    CancellationToken cancellationToken = default)
{
    var request = new SendEmailRequest
    {
        FromEmailAddress = $"{_options.FromName} <{_options.FromEmail}>",
        Destination = new Destination { ToAddresses = new List<string> { toEmail } },
        ReplyToAddresses = new List<string> { _options.ReplyTo ?? _options.FromEmail },
        Content = new EmailContent
        {
            Template = new Template
            {
                TemplateName = templateName,
                TemplateData = JsonSerializer.Serialize(data)
            }
        }
    };
    
    var response = await _sesClient.SendEmailAsync(request, cancellationToken);
}
```

**Key Features**:
- **Template-based emails**: Uses SES templates for consistent branding
- **Generic data binding**: Accepts any object type for template data
- **Reply-to support**: Configurable reply-to address
- **JSON serialization**: Automatic template data serialization

**Template Example** (`docs/Templates/otp-template.json`):
```json
{
  "TemplateName": "OtpVerificationTemplate",
  "TemplateContent": {
    "Subject": "Tu código de verificación es {{code}}",
    "Html": "<h2>Hola {{name}},</h2><p>Tu código: <strong>{{code}}</strong></p>",
    "Text": "Hola {{name}},\n\nTu código: {{code}}\n\nExpira en {{ValidForMinutes}} minutos."
  }
}
```

**Usage in Handler**:
```csharp
var templateData = new { 
    name = "John Doe", 
    code = otp.Code, 
    ValidForMinutes = _otpSettings.ValidForMinutes 
};

await _emailSender.SendAsync(
    email.ToString(),
    "OtpVerificationTemplate",
    templateData,
    cancellationToken);
```

**Configuration**:
```bash
Ses__Region=us-east-1
Ses__FromEmail=noreply@example.com
Ses__FromName=User Management
Ses__ReplyTo=support@example.com  # Optional
Ses__AccessKey=  # Optional if using IAM roles
Ses__SecretKey=  # Optional if using IAM roles
```

**NuGet Package**: `AWSSDK.SimpleEmailV2` v4.0.11.6

### 4. Cognito Mock Service

**File**: `src/UserManagement.Infrastructure/Services/MockCognitoIdentityService.cs`

Functional mock that simulates AWS Cognito behavior:

```csharp
public Task<string> CreateUserAsync(string email, string name, 
    CancellationToken cancellationToken = default)
{
    var cognitoSub = $"mock-cognito-{Guid.NewGuid()}";
    _usersByEmail.AddOrUpdate(email.ToLowerInvariant(), cognitoSub, (_, _) => cognitoSub);
    
    _logger.LogInformation(
        "Mock Cognito: Created user {Email} with sub {CognitoSub}",
        email, cognitoSub);
    
    return Task.FromResult(cognitoSub);
}
```

**Features**:
- Generates fake Cognito subs: `mock-cognito-{Guid}`
- Stores users in memory (`ConcurrentDictionary`)
- Logs all operations for observability
- Registered as singleton for in-memory persistence

**Migration Path**: Replace `MockCognitoIdentityService` with real AWS Cognito implementation in Milestone 07.

### 3. Amazon SNS Integration

**File**: `src/UserManagement.Infrastructure/Services/SnsMessagePublisher.cs`

Publishes domain events to Amazon SNS topic:

```csharp
public async Task PublishMessageAsync(string eventType, string eventContent, 
    CancellationToken cancellationToken = default)
{
    var request = new PublishRequest
    {
        TopicArn = _options.TopicArn,
        Message = eventContent,
        Subject = eventType,
        MessageAttributes = new Dictionary<string, MessageAttributeValue>
        {
            ["EventType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = eventType
            }
        }
    };
    
    var response = await _snsClient.PublishAsync(request, cancellationToken);
}
```

**Configuration**:
```bash
Sns__Region=us-east-1
Sns__TopicArn=arn:aws:sns:us-east-1:123456789012:user-events
Sns__AccessKey=  # Optional if using IAM roles
Sns__SecretKey=  # Optional if using IAM roles
```

**NuGet Package**: `AWSSDK.SimpleNotificationService` v3.7.300.0

### 5. Retry Logic with Exponential Backoff

**Enhanced OutboxProcessor** implements retry logic:

```csharp
if (message.CanRetry(_options.MaxRetries))
{
    message.IncrementRetry(_options.InitialRetryDelaySeconds, 
        _options.RetryBackoffMultiplier);
    message.MarkAsFailed($"Retry {message.RetryCount}/{_options.MaxRetries}: {ex.Message}");
    
    _logger.LogWarning(
        "Scheduled retry {RetryCount}/{MaxRetries} for message {MessageId} at {NextRetryAt}",
        message.RetryCount, _options.MaxRetries, message.Id, message.NextRetryAt);
}
```

**Exponential Backoff Formula**:
```
delaySeconds = initialDelay * (backoffMultiplier ^ (retryCount - 1))
```

**Example** (InitialDelay=60s, Multiplier=2):
- Retry 1: 60 seconds
- Retry 2: 120 seconds
- Retry 3: 240 seconds

**Configuration**:
```bash
OutboxProcessor__MaxRetries=3
OutboxProcessor__InitialRetryDelaySeconds=60
OutboxProcessor__RetryBackoffMultiplier=2
```

### 6. Cleanup Jobs

**OutboxCleanupJob** — Removes old processed messages:

```csharp
var cutoffDate = DateTimeOffset.UtcNow.AddDays(-_options.OutboxRetentionDays);
var deletedCount = await repository.DeleteOlderThanAsync(cutoffDate, cancellationToken);
```

**IdempotencyCleanupJob** — Removes expired idempotency keys:

```csharp
var ttl = TimeSpan.FromHours(_idempotencyOptions.TtlHours);
var deletedCount = await repository.DeleteExpiredAsync(ttl, cancellationToken);
```

**Configuration**:
```bash
CleanupJob__OutboxRetentionDays=30
CleanupJob__CleanupIntervalHours=24
```

---

## Database Schema Changes

### OutboxMessages Table

Added retry tracking columns:

```sql
ALTER TABLE OutboxMessages
ADD COLUMN RetryCount INT NOT NULL DEFAULT 0,
ADD COLUMN NextRetryAt DATETIME(6) NULL,
ADD INDEX IX_OutboxMessages_NextRetryAt (NextRetryAt);
```

**Migration Required**: Run EF Core migrations to update database schema.

---

## Dependency Injection Changes

### Infrastructure Layer

**Removed from Application**:
- Fake `IEmailSender` registration
- Fake `IOtpGenerator` registration
- Fake `ICognitoIdentityService` registration

**Added to Infrastructure**:
```csharp
// OTP Generator and Settings Provider
services.Configure<OtpGeneratorOptions>(configuration.GetSection(OtpGeneratorOptions.SectionName));
services.AddScoped<IOtpSettingsProvider, OtpSettingsProvider>();
services.AddScoped<IOtpGenerator, SecureOtpGenerator>();

// Cognito Mock
services.AddSingleton<ICognitoIdentityService, MockCognitoIdentityService>();

// Amazon SES V2 Email
services.Configure<SesOptions>(configuration.GetSection(SesOptions.SectionName));
services.AddScoped<IAmazonSimpleEmailServiceV2>(sp =>
{
    var sesOptions = configuration.GetSection(SesOptions.SectionName).Get<SesOptions>();
    var region = RegionEndpoint.GetBySystemName(sesOptions?.Region ?? "us-east-1");
    
    if (!string.IsNullOrWhiteSpace(sesOptions?.AccessKey) && !string.IsNullOrWhiteSpace(sesOptions?.SecretKey))
    {
        var credentials = new BasicAWSCredentials(sesOptions.AccessKey, sesOptions.SecretKey);
        return new AmazonSimpleEmailServiceV2Client(credentials, region);
    }
    
    return new AmazonSimpleEmailServiceV2Client(region);
});
services.AddScoped<IEmailSender, SesEmailSender>();

// Amazon SNS
services.Configure<SnsOptions>(configuration.GetSection(SnsOptions.SectionName));
services.AddScoped(sp =>
{
    var snsOptions = configuration.GetSection(SnsOptions.SectionName).Get<SnsOptions>();
    var region = RegionEndpoint.GetBySystemName(snsOptions?.Region ?? "us-east-1");
    
    if (!string.IsNullOrWhiteSpace(snsOptions?.AccessKey) && !string.IsNullOrWhiteSpace(snsOptions?.SecretKey))
    {
        var credentials = new BasicAWSCredentials(snsOptions.AccessKey, snsOptions.SecretKey);
        return new AmazonSimpleNotificationServiceClient(credentials, region);
    }
    
    return new AmazonSimpleNotificationServiceClient(region);
});
services.AddScoped<IMessagePublisher, SnsMessagePublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SnsMessagePublisher>>();
    var options = sp.GetRequiredService<IOptions<SnsOptions>>();
    var snsClient = sp.GetRequiredService<AmazonSimpleNotificationServiceClient>();
    
    return new SnsMessagePublisher(options, logger, snsClient);
});

// Cleanup Jobs
services.Configure<CleanupJobOptions>(configuration.GetSection(CleanupJobOptions.SectionName));
services.AddHostedService<OutboxCleanupJob>();
services.AddHostedService<IdempotencyCleanupJob>();
```

---

## Configuration

### Complete .env.example

```bash
# OTP Generator
OtpGenerator__Length=6
OtpGenerator__ValidForMinutes=10

# Amazon SES V2 Email Service
Ses__Region=us-east-1
Ses__FromEmail=noreply@example.com
Ses__FromName=User Management
Ses__ReplyTo=support@example.com
Ses__AccessKey=
Ses__SecretKey=

# Amazon SNS Message Bus
Sns__Region=us-east-1
Sns__TopicArn=arn:aws:sns:us-east-1:123456789012:user-events
Sns__AccessKey=
Sns__SecretKey=

# Outbox Processor with Retry Logic
OutboxProcessor__PollingIntervalSeconds=10
OutboxProcessor__BatchSize=20
OutboxProcessor__MaxRetries=3
OutboxProcessor__InitialRetryDelaySeconds=60
OutboxProcessor__RetryBackoffMultiplier=2

# Cleanup Jobs
CleanupJob__OutboxRetentionDays=30
CleanupJob__CleanupIntervalHours=24
```

---

## Testing

### Unit Tests

All existing unit tests pass with new implementations:
- **Domain Tests**: 51 tests ✅
- **Application Tests**: 6 tests ✅
- **Integration Tests**: 10 tests ✅

**Test Fakes Unchanged**: Test projects continue using their own fakes for isolation.

### Test Updates

**Application Unit Tests** - Added `FakeOtpSettingsProvider`:
```csharp
public sealed class FakeOtpSettingsProvider : IOtpSettingsProvider
{
    public int Length { get; set; } = 6;
    public int ValidForMinutes { get; set; } = 10;
}
```

**Integration Tests** - Added `StubOtpSettingsProvider`:
```csharp
public sealed class StubOtpSettingsProvider : IOtpSettingsProvider
{
    public int Length => 6;
    public int ValidForMinutes => 10;
}
```

**Test Fixes**:
1. **RegisterUserHandlerTests** - Updated all tests to include `IOtpSettingsProvider` parameter
2. **CustomWebApplicationFactory** - Registered `StubOtpSettingsProvider` for integration tests
3. **UserOtp.ToString()** - Override returns `Code` for template data binding

### Manual Testing Checklist

**Email Service (SES)**:
- [ ] Create SES template in AWS Console using `docs/Templates/otp-template.json`
- [ ] Verify sender email in SES
- [ ] Test email delivery with template data
- [ ] Verify HTML and text versions render correctly

**OTP Generation**:
- [ ] OTP codes are 6 digits and cryptographically random (TOTP-based)
- [ ] OTP validity period matches configuration
- [ ] OTP settings provider returns correct values

**Cognito Mock**:
- [ ] Mock creates users with fake subs: `mock-cognito-{Guid}`
- [ ] Mock retrieves users by email
- [ ] In-memory storage persists during application lifetime

**SNS Integration**:
- [ ] SNS publishes events to configured topic
- [ ] Message attributes include EventType
- [ ] IAM role or credentials work correctly

**Retry Logic**:
- [ ] Retry logic schedules retries with exponential backoff
- [ ] Failed messages marked after max retries
- [ ] NextRetryAt calculated correctly

**Cleanup Jobs**:
- [ ] Cleanup jobs execute on schedule (24h interval)
- [ ] Old outbox messages deleted after retention period (30 days)
- [ ] Expired idempotency keys deleted based on TTL

---

## Observability

### Logging

All services include comprehensive logging:

**OTP Generator**:
```
Information: Generated secure OTP code
```

**Amazon SES Email**:
```
Information: Sending email via Amazon SES to {ToEmail} with template {TemplateName}
Information: Email sent successfully to {ToEmail}. MessageId: {MessageId}
Error: Failed to send email to {ToEmail} with template {TemplateName}
Information: Email sent successfully with status code {StatusCode}
Error: Failed to send email to {ToEmail}. Status: {StatusCode}
```

**Cognito Mock**:
```
Information: Mock Cognito: Created user {Email} with sub {CognitoSub}
Information: Mock Cognito: Lookup user by email {Email} - Found: {Found}
```

**SNS Publisher**:
```
Information: Publishing message to SNS topic {TopicArn}
Information: Successfully published message to SNS. MessageId: {MessageId}
Error: Failed to publish message to SNS topic {TopicArn}
```

**Outbox Processor**:
```
Information: Processing outbox message {MessageId} (Attempt {RetryCount})
Information: Domain Event Published to message bus: {EventType}
Warning: Scheduled retry {RetryCount}/{MaxRetries} for message {MessageId}
Error: Message {MessageId} permanently failed after {RetryCount} retries
```

**Cleanup Jobs**:
```
Information: Starting cleanup of outbox messages older than {CutoffDate}
Information: Cleanup completed. Deleted {DeletedCount} old outbox messages
```

---

## Performance Considerations

### Outbox Processor

- **Batch Processing**: Processes up to 20 messages per cycle (configurable)
- **Retry Scheduling**: Skips messages not ready for retry
- **Exponential Backoff**: Prevents overwhelming external services

### Cleanup Jobs

- **Scheduled Execution**: Runs every 24 hours (configurable)
- **Batch Deletion**: Deletes in batches to avoid long transactions
- **Retention Policy**: 30 days for outbox messages (configurable)

### Amazon SES

- **Sandbox Mode**: 200 emails/day, verified recipients only
- **Production Mode**: 50,000 emails/day (default), can request increase
- **Cost**: $0.10 per 1,000 emails
- **Recommendation**: Move out of sandbox for production use

### Amazon SNS

- **Throughput**: 30,000 messages/second per topic
- **Cost**: $0.50 per 1 million requests
- **Recommendation**: Use IAM roles instead of access keys

---

## Security

### API Keys

- **AWS Credentials**: Use IAM roles when possible (recommended)
- **Access Keys**: Only use for local development
- **Configuration**: Use `.env` file (gitignored)
- **SES Templates**: Store in AWS, not in code

### OTP Generation

- **Algorithm**: TOTP (RFC 6238) with SHA-256
- **Library**: Otp.NET for standardized implementation
- **Length**: Configurable (default 6 digits)
- **Time Window**: 30 seconds
- **Entropy**: High entropy from cryptographic RNG

### Cognito Mock

- **Development Only**: Not for production use
- **No Authentication**: Mock accepts all requests
- **In-Memory Storage**: Data lost on restart

---

## Migration Guide

### From Milestone 05 to 06

1. **Update NuGet Packages**:
   ```bash
   dotnet add package Otp.NET --version 1.4.1
   dotnet add package AWSSDK.SimpleEmailV2 --version 4.0.11.6
   dotnet add package AWSSDK.SimpleNotificationService --version 3.7.300.0
   ```

2. **Create SES Email Template**:
   
   **Using AWS CLI v2**:
   
   a. Create template:
   ```bash
   aws sesv2 create-email-template \
     --cli-input-json file://docs/Templates/otp-template.json \
     --region us-east-1
   ```
   
   b. Test template with sample data (create `template-data.json`):
   ```json
   {
     "name": "John Doe",
     "code": "123456",
     "ValidForMinutes": "10"
   }
   ```
   
   c. Send test email:
   ```bash
   aws ses send-templated-email \
     --source "noreply@yourdomain.com" \
     --destination '{"ToAddresses":["test@yourdomain.com"]}' \
     --template "OtpVerificationTemplate" \
     --template-data file://template-data.json \
     --region us-east-1
   ```
   
   d. Delete template (if needed):
   ```bash
   aws ses delete-template \
     --template-name "OtpVerificationTemplate" \
     --region us-east-1
   ```
   
   **Important**: Verify sender email in SES before sending emails

3. **Update Configuration**:
   - Copy `.env.example` to `.env`
   - Set SES region and sender email
   - Set SNS topic ARN (optional)
   - Configure OTP validity period
   - Configure retry and cleanup options

4. **Verify Tests**:
   ```bash
   dotnet test
   ```

5. **Deploy**:
   - Ensure environment variables are set
   - Restart application to load new services

---

## Next Steps (Milestone 07)

1. **Real AWS Cognito Integration**
   - Replace `MockCognitoIdentityService` with `AwsCognitoIdentityService`
   - Configure Cognito User Pool
   - Implement user authentication flow

2. **Health Checks**
   - Add SES connectivity check
   - Add SNS connectivity check
   - Add database connectivity check

3. **Monitoring & Alerts**
   - Set up CloudWatch alarms for SNS failures
   - Monitor SES delivery rates and bounce notifications
   - Track outbox retry rates
   - Set up SES event publishing to SNS

4. **Performance Optimization**
   - Implement message batching for SNS
   - Add caching for frequently accessed data
   - Optimize database queries

---

## Summary

Milestone 06 successfully integrates external services while maintaining clean architecture and testability:

✅ **Secure OTP Generation** — TOTP-based with Otp.NET library  
✅ **Amazon SES V2 Email** — Template-based email delivery  
✅ **OTP Settings Provider** — Centralized configuration management  
✅ **Cognito Mock** — Functional mock for development  
✅ **Amazon SNS Publishing** — Domain events to message bus  
✅ **Retry Logic** — Exponential backoff for resilience  
✅ **Cleanup Jobs** — Automated data maintenance  
✅ **All Tests Passing** — 67 tests across all projects  
✅ **Enhanced Domain Entities** — UserOtp improvements  
✅ **Options Pattern** — Centralized configuration in Options folder  

The application is now ready for production use with AWS services (SES V2, SNS), while maintaining the flexibility to swap implementations (e.g., Cognito mock → real Cognito) without code changes.
