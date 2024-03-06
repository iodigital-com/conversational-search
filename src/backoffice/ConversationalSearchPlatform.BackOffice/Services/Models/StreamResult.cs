namespace ConversationalSearchPlatform.BackOffice.Services.Models;

public record StreamResult<T>
{
    private StreamResult(T? Value, Exception? Error, string? SkipReason)
    {
        this.Value = Value;
        this.Error = Error;
        this.SkipReason = SkipReason;
    }

    public bool IsOk => Error == null;
    public T? Value { get; init; }
    public Exception? Error { get; init; }
    public string? SkipReason { get; init; }

    public static StreamResult<T> Ok(T result) => new(result, default, default);

    public static StreamResult<T> FunctionCall(T result) => new(result, default, default);

    public static StreamResult<T> Fail(Exception exception) =>
        new(default, exception, default);

    public static StreamResult<T> Skip(string reason) =>
        new(default, default, reason);


    public static implicit operator StreamResult<T>(T value)
        => Ok(value);

    public static implicit operator StreamResult<T>(Exception err)
        => Fail(err);

    public static implicit operator StreamResult<T>(string skipReason)
        => Skip(skipReason);
}