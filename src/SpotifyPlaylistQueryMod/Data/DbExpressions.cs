using System.Linq.Expressions;
using SpotifyPlaylistQueryMod.Models.Entities;
using SpotifyPlaylistQueryMod.Shared.Enums;
using PQSetPropertyCalls = Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<SpotifyPlaylistQueryMod.Models.Entities.PlaylistQueryState>;

namespace SpotifyPlaylistQueryMod.Data;

internal static class DbExpressions
{
    public static readonly Expression<Func<PlaylistQueryExecutionStatus, PlaylistQueryExecutionStatus>> UpdateExecutionStatusExpression =
    status => !status.HasFlag(PlaylistQueryExecutionStatus.NoReadAccess | PlaylistQueryExecutionStatus.NoWriteAccess) ?
        status & ~PlaylistQueryExecutionStatus.Blocked :
        status;
    public static Expression<Func<PlaylistQueryState, PlaylistQueryExecutionStatus>> UpdateReadAccessExpression =>
        Compose<PlaylistQueryState, PlaylistQueryExecutionStatus>(
            q => q.Status & ~PlaylistQueryExecutionStatus.NoReadAccess,
            UpdateExecutionStatusExpression
        );

    public static readonly Expression<Func<PlaylistQueryState, PlaylistQueryExecutionStatus>> BlockReadAccessExpression =
        Compose<PlaylistQueryState, PlaylistQueryExecutionStatus>(
            q => q.Status | PlaylistQueryExecutionStatus.Blocked | PlaylistQueryExecutionStatus.NoReadAccess,
            UpdateExecutionStatusExpression
        );

    //equivalent to e => e.SetProperty(p => p.Status, q => expr(q))
    public static Expression<Func<PQSetPropertyCalls, PQSetPropertyCalls>> StatusUpdateAsyncLambdaExpression(Expression<Func<PlaylistQueryState, PlaylistQueryExecutionStatus>> expr)
    {
        ParameterExpression param = Expression.Parameter(typeof(PQSetPropertyCalls), "e");
        InvocationExpression body = Expression.Invoke((PQSetPropertyCalls e, Func<PlaylistQueryState, PlaylistQueryExecutionStatus> v) =>
            e.SetProperty(static p => p.Status, v),
            param, expr
        );
        return Expression.Lambda<Func<PQSetPropertyCalls, PQSetPropertyCalls>>(body, param);
    }

    public static Expression<Func<T, V>> Compose<T, V>(Expression<Func<T, V>> first, Expression<Func<V, V>> second)
    {
        ParameterExpression param = Expression.Parameter(typeof(T));
        InvocationExpression body = Expression.Invoke(second, Expression.Invoke(first, param));
        return Expression.Lambda<Func<T, V>>(body, param);
    }
}