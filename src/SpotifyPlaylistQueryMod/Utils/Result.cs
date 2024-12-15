using System.Diagnostics.CodeAnalysis;

namespace SpotifyPlaylistQueryMod.Utils;

public class Result<T>
{
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsSuccess { get; }
    public Exception? Exception { get; }
    public T? Value { get; }

    private Result(bool success, T? value, Exception? ex)
    {
        IsSuccess = success;
        Value = value;
        Exception = ex;
    }

    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Failure(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(Exception));
        return new Result<T>(false, default, ex);
    }
}