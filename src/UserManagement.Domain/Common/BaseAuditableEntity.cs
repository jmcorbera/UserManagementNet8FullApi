namespace UserManagement.Domain.Common;

public abstract class BaseAuditableEntity<T> : BaseEntity<T>
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }

    protected virtual void SetCreated(string? createdBy)
    {
        Created = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
    }

    protected void SetLastModified(string? modifiedBy)
    {
        LastModified = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}

