namespace UserManagement.Application.Common.Abstractions;

public interface IOtpSettingsProvider
{
    int Length { get; }
    int ValidForMinutes { get; }
}