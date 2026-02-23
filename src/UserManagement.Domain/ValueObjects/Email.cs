using System.Text.RegularExpressions;
using UserManagement.Domain.Common;

namespace UserManagement.Domain.ValueObjects;

/// <summary>
/// Value Object que representa un email válido.
/// </summary>
public sealed record Email
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email cannot be null or empty.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new DomainException($"Invalid email format: {value}");

        return new Email(normalized);
    }

    private static bool IsValidEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;

        var dotIndex = email.LastIndexOf('.');
        if (dotIndex <= atIndex + 1) return false;

        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

}

