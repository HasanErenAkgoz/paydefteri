using System.Diagnostics;
using System.Security.Claims;

namespace FuzulTaksitTakip.Api.Middleware;

/// <summary>
/// Logs every HTTP request/response with method, path, status, user, and duration.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;

        try
        {
            await _next(context);
            sw.Stop();
            var user = ResolveUser(context);
            _logger.LogInformation(
                "HTTP {Method} {Path} → {StatusCode} for {User} in {ElapsedMs}ms",
                method,
                path,
                context.Response.StatusCode,
                user,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var user = ResolveUser(context);
            _logger.LogError(
                ex,
                "HTTP {Method} {Path} crashed for {User} in {ElapsedMs}ms",
                method,
                path,
                user,
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    private static bool ShouldSkip(string path)
        => path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
           || path.Equals("/health", StringComparison.OrdinalIgnoreCase);

    private static string ResolveUser(HttpContext context)
    {
        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return "anonymous";
        }

        return principal.FindFirstValue(ClaimTypes.Email)
               ?? principal.FindFirstValue("email")
               ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue("sub")
               ?? "authenticated";
    }
}
