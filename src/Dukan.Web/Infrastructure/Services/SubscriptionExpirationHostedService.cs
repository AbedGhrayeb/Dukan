using Dukan.Web.Application.Interfaces;

namespace Dukan.Web.Infrastructure.Services;

public sealed class SubscriptionExpirationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionExpirationHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ExpireAndSyncFirebaseAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to expire overdue subscriptions.");
            }
        }
    }

    private async Task ExpireAndSyncFirebaseAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var count = await subscriptionService.ExpireOverdueAsync(ct);
        if (count > 0)
            logger.LogInformation("Hosted expiry completed: {Count} overdue subscription(s) marked as expired (Firebase synced).", count);
    }
}
