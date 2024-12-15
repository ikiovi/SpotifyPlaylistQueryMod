namespace SpotifyPlaylistQueryMod.Background.Services.Queue;

public interface ITaskQueue<T>
{
    ValueTask QueueAsync(T item, CancellationToken cancel);
    ValueTask<T> DequeueAsync(CancellationToken cancel);
}