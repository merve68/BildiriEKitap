using System.Threading.Channels;

namespace EKitap.Api.Services;

public interface IKitapGenerationQueue
{
    void Enqueue(Guid kitapId);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}

public class KitapGenerationQueue : IKitapGenerationQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid kitapId) => _channel.Writer.TryWrite(kitapId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

public class KitapGenerationWorker(IServiceScopeFactory scopeFactory, IKitapGenerationQueue queue, ILogger<KitapGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var kitapId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IKitapService>();
                await service.GenerateAsync(kitapId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kitap üretimi başarısız: {KitapId}", kitapId);
            }
        }
    }
}
