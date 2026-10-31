namespace AdaraBox.Core.Common;

public sealed record OperationError(string Code, string Message);

public sealed record OperationResult(bool Success, OperationError? Error = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string code, string message) => new(false, new OperationError(code, message));
}

public sealed record OperationResult<T>(bool Success, T? Value = default, OperationError? Error = null)
{
    public static OperationResult<T> Ok(T value) => new(true, value);
    public static OperationResult<T> Fail(string code, string message) => new(false, default, new OperationError(code, message));
}
