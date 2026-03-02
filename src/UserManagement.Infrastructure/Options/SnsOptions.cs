namespace UserManagement.Infrastructure.Options;

public sealed class SnsOptions
{
    public const string SectionName = "Sns";

    public string Region { get; set; } = "us-east-1";
    public string TopicArn { get; set; } = string.Empty;
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}
