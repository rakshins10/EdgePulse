using EdgePulse.Application.Common.Behaviours;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Application.Common.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EdgePulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // MediatR -- scans this assembly for handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                Assembly.GetExecutingAssembly()));

        // Localization resolver
        services.AddScoped<ILookupTranslator, LookupTranslator>();

        // FluentValidation -- scans for validators
        services.AddValidatorsFromAssembly(
            Assembly.GetExecutingAssembly());

        // MediatR Pipeline Behaviours
        // Order matters -- logging first, then validation
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(LoggingBehaviour<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehaviour<,>));

        return services;
    }
}
