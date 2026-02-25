using System.Reflection;
using FluentValidation;
using MediatR;
using UserManagement.Application.Common.Results;

namespace UserManagement.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that runs FluentValidation and returns Result.Failure(Validation) when invalid.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorsList = _validators.ToList();
        if (validatorsList.Count == 0)
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validatorsList.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var validationErrors = failures
            .GroupBy(x => x.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

        var error = Error.Validation("One or more validation errors occurred.", validationErrors);
        return CreateFailureResult(error);
    }

    private static TResponse CreateFailureResult(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod("Failure", BindingFlags.Public | BindingFlags.Static);
            if (failureMethod != null)
            {
                var result = failureMethod.Invoke(null, new object[] { error });
                if (result != null)
                    return (TResponse)result;
            }
        }

        throw new InvalidOperationException($"ValidationBehavior does not support response type {responseType.Name}. Use Result or Result<T>.");
    }
}
