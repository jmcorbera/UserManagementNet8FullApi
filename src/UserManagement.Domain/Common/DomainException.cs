namespace UserManagement.Domain.Common;

public class DomainException : Exception
{
    public DomainException(string? message = "Exception in Domain Layer")
        : base(message)
    {
    }
}