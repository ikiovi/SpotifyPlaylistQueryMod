using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpotifyPlaylistQueryMod.Data.Exceptions;

public class EntityNotFoundException<T> : Exception
{
    public EntityNotFoundException(string message, Exception? exception) : base(message, exception) { }
    public EntityNotFoundException(string message) : base(message) { }
    public EntityNotFoundException() : base() { }

    public static void ThrowIfNull([NotNull] T? entity, string message)
    {
        if (entity is not null) return;
        throw new EntityNotFoundException<T>(message);
    }
}

public class EntityNotFoundException<T, K> : EntityNotFoundException<T>
{
    public required K Key { get; init; }
    public EntityNotFoundException(string message, Exception? exception) : base(message, exception) { }
    [SetsRequiredMembers]
    public EntityNotFoundException(string message, K key) : base(message) => Key = key;
    [SetsRequiredMembers]
    public EntityNotFoundException(K key) : this(ExceptionMessage(key), key) { }

    public static void ThrowIfNull([NotNull] T? entity, K key)
    {
        if (entity is not null) return;
        throw new EntityNotFoundException<T, K>(key);
    }

    private static string ExceptionMessage(K key) => $"Entity of type [{typeof(T)}] with key [{key}] not found.";
}

public static class EntityNotFoundException
{
    public static void ThrowIfNull<T>([NotNull] T? entity, string message)
    {
        EntityNotFoundException<T>.ThrowIfNull(entity, message);
    }

    public static void ThrowIfNullWithKey<T, K>([NotNull] T? entity, K key)
    {
        EntityNotFoundException<T, K>.ThrowIfNull(entity, key);
    }
}