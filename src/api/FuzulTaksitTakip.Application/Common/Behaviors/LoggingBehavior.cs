using System.Diagnostics;
using FuzulTaksitTakip.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FuzulTaksitTakip.Application.Common.Behaviors;

/// <summary>
/// Logs every MediatR request: who, what, duration, success/failure.
/// Does not log request bodies (may contain passwords / PII payloads).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var user = ResolveUser();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("→ {RequestName} started by {User}", requestName, user);

        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation(
                "← {RequestName} succeeded for {User} in {ElapsedMs}ms",
                requestName,
                user,
                sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(
                ex,
                "✗ {RequestName} failed for {User} in {ElapsedMs}ms ({ErrorType}: {ErrorMessage})",
                requestName,
                user,
                sw.ElapsedMilliseconds,
                ex.GetType().Name,
                ex.Message);
            throw;
        }
    }

    private string ResolveUser()
    {
        if (!string.IsNullOrWhiteSpace(_currentUser.Email))
        {
            return _currentUser.Email!;
        }

        if (!string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return _currentUser.UserId!;
        }

        return "anonymous";
    }
}
