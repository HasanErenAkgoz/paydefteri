using System.Reflection;
using FluentValidation;
using FuzulTaksitTakip.Application.Common.Behaviors;
using FuzulTaksitTakip.Application.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FuzulTaksitTakip.Application;

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
