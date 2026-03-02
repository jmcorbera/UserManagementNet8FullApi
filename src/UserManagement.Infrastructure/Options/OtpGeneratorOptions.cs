namespace UserManagement.Infrastructure.Options;

public sealed class OtpGeneratorOptions
{
    public const string SectionName = "OtpGenerator";

    public int Length { get; set; } = 6;

    public int ValidForMinutes { get; set; } = 10;
}
