using System.Threading.Channels;

namespace SpotifyPlaylistQueryMod.Background.Services.Queue;

internal sealed class ChannelBasedTaskQueue<T> : ITaskQueue<T> where T : notnull
{
    private readonly Channel<T> queue;

    public ChannelBasedTaskQueue(int capacity) : this(GetDefaultOptions(capacity)) { }
    public ChannelBasedTaskQueue(BoundedChannelOptions options)
    {
        queue = Channel.CreateBounded<T>(options);
    }

    public ValueTask QueueAsync(T item, CancellationToken cancel)
    {
        ArgumentNullException.ThrowIfNull(item);
        return queue.Writer.WriteAsync(item, cancel);
    }

    public ValueTask<T> DequeueAsync(CancellationToken cancel) =>
        queue.Reader.ReadAsync(cancel);

    private static BoundedChannelOptions GetDefaultOptions(int capacity) =>
        new(capacity) { FullMode = BoundedChannelFullMode.Wait };
}
