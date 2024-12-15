using System.Runtime.ExceptionServices;
using Polly;

namespace SpotifyPlaylistQueryMod.Utils;

public static class TaskExtensions
{
    internal static Task<Result<T>> ToResultTask<T>(this Task<T> task)
    {
        return task.ContinueWith(t =>
        {
            if (t.IsFaulted) return Result<T>.Failure(t.Exception.InnerException ?? t.Exception);
            if (t.IsCanceled && t.Exception is not null) ExceptionDispatchInfo.Throw(t.Exception);
            return Result<T>.Success(t.Result);
        });
    }

    internal static Task<T> WithRetryPolicy<T>(this Task<T> task, IAsyncPolicy retryPolicy) =>
        retryPolicy.ExecuteAsync(async () => await task);

    internal static Task WithRetryPolicy(this Task task, IAsyncPolicy retryPolicy) =>
        retryPolicy.ExecuteAsync(async () => await task);
}
