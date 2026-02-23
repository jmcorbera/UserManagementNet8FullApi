namespace UserManagement.Domain.Specifications;

/// <summary>
/// Interfaz base para Specifications (patrón Specification).
/// </summary>
public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T entity);
}
