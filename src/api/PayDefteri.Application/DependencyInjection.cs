using System.Reflection;
using FluentValidation;
using PayDefteri.Application.Common.Behaviors;
using PayDefteri.Application.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PayDefteri.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<MobileSessionIssuer>();
        // Outer → inner: logging wraps validation wraps handler
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
