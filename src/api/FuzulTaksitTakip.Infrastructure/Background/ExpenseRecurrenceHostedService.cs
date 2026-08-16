using FuzulTaksitTakip.Application.Common.Interfaces;
using FuzulTaksitTakip.Application.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FuzulTaksitTakip.Infrastructure.Background;

/// <summary>Runs due recurring-expense generation once per UTC day outside HTTP read paths.</summary>
public sealed class ExpenseRecurrenceHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpenseRecurrenceHostedService> _logger;

    public ExpenseRecurrenceHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpenseRecurrenceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A second instance can generate the same period concurrently. The database unique
        // index is authoritative; retrying in a fresh DbContext turns that conflict into a no-op.
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                await ExpenseRecurrenceGenerator.GenerateAllDueAsync(db, today, cancellationToken);
                return;
            }
            catch (DbUpdateException) when (attempt == 1)
            {
                _logger.LogInformation("Recurring expense generation conflicted; retrying safely.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Recurring expense generation failed");
                return;
            }
        }
    }
}
