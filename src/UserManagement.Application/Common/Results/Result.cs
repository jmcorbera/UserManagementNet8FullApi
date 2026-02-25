namespace UserManagement.Application.Common.Results;

/// <summary>
/// Non-generic result for operations that do not return a value.
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}
