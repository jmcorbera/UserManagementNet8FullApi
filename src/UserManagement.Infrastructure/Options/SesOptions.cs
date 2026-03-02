namespace UserManagement.Infrastructure.Options;

public sealed class SesOptions
{
    public const string SectionName = "Ses";

    public string Region { get; set; } = "us-east-1";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "User Management";
    public string? ReplyTo { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}
