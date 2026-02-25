namespace UserManagement.Application.Common.Results;

/// <summary>
/// Represents an application error with code, message and optional validation details.
/// </summary>
public sealed class Error
{
    public static class Codes
    {
        public const string Validation = "Validation";
        public const string NotFound = "NotFound";
        public const string Conflict = "Conflict";
        public const string OtpInvalid = "OtpInvalid";
        public const string OtpExpired = "OtpExpired";
        public const string FeatureDisabled = "FeatureDisabled";
        public const string Unexpected = "Unexpected";
        public const string ExternalService = "ExternalService";
    }

    public string Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    private Error(string code, string message, IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        Code = code;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public static Error Validation(string message, IReadOnlyDictionary<string, string[]>? validationErrors = null)
        => new(Codes.Validation, message, validationErrors);

    public static Error NotFound(string message)
        => new(Codes.NotFound, message);

    public static Error Conflict(string message)
        => new(Codes.Conflict, message);

    public static Error OtpInvalid(string message)
        => new(Codes.OtpInvalid, message);

    public static Error OtpExpired(string message)
        => new(Codes.OtpExpired, message);

    public static Error FeatureDisabled(string message)
        => new(Codes.FeatureDisabled, message);

    public static Error Unexpected(string message)
        => new(Codes.Unexpected, message);

    public static Error ExternalService(string message)
        => new(Codes.ExternalService, message);
}
