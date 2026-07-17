namespace FourInRow.Server.Services;

/// <summary>Periodically sweeps rooms that outlived their activity TTL.</summary>
public sealed class GameCleanupService(GameStore store, ILogger<GameCleanupService> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var removed = store.RemoveExpired();
            if (removed > 0)
            {
                logger.LogInformation("Removed {Count} expired game room(s).", removed);
            }
        }
    }
}
