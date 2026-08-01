using FuzulTaksitTakip.Application.Reminders;
using FuzulTaksitTakip.Infrastructure.Email;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuzulTaksitTakip.Infrastructure.Background;

public sealed class DailyReminderHostedService : BackgroundService
{
    private static readonly TimeZoneInfo TurkeyTz = ResolveTurkeyTz();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReminderOptions _options;
    private readonly ILogger<DailyReminderHostedService> _logger;

    public DailyReminderHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReminderOptions> options,
        ILogger<DailyReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Payment reminder job started (RunHourLocal={Hour}, RunOnStartup={Startup})",
            _options.RunHourLocal,
            _options.RunOnStartup);

        if (_options.RunOnStartup)
        {
            await RunOnceAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun();
            _logger.LogInformation("Next payment reminder run in {Delay}", delay);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new ProcessPaymentRemindersCommand(), cancellationToken);
            _logger.LogInformation(
                "Payment reminders processed. Partner={Partner} Owner={Owner} Skipped={Skipped}",
                result.PartnerEmails,
                result.OwnerEmails,
                result.SkippedLogged);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Payment reminder job failed");
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var hour = Math.Clamp(_options.RunHourLocal, 0, 23);
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TurkeyTz);
        var nextLocal = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, hour, 0, 0, DateTimeKind.Unspecified);
        if (nextLocal <= nowLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, TurkeyTz);
        var delay = nextUtc - DateTime.UtcNow;
        return delay < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : delay;
    }

    private static TimeZoneInfo ResolveTurkeyTz()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
    }
}
