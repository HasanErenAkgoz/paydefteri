using System.Text.Json;
using FluentValidation;
using PayDefteri.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PayDefteri.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                ve.Errors.Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Doğrulama hatası.",
                ve.Errors
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "_form" : e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) as object),
            NotFoundException nf => (StatusCodes.Status404NotFound, "Not found", nf.Message, null),
            ForbiddenException fb => (StatusCodes.Status403Forbidden, "Forbidden", fb.Message, null),
            ConflictException cf => (StatusCodes.Status409Conflict, "Conflict", cf.Message, null),
            ExternalServiceUnavailableException es => (
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                es.Message,
                null),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "Kayıt başka bir işlemle değişmiş veya silinmiş olabilir. Sayfayı yenileyip tekrar deneyin.",
                null),
            UnauthorizedAccessException ua => (StatusCodes.Status401Unauthorized, "Unauthorized", ua.Message, null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                null)
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Title}", title);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
