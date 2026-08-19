using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Infrastructure.Persistence;
using EdgePulse.Infrastructure.Services;
using EdgePulse.Infrastructure.Services.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EdgePulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ILocaleContext, LocaleContext>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddHttpClient<IIdentityAdminService, KeycloakAdminService>();
        services.AddHttpClient<Application.Features.Webhooks.IWebhookSender, WebhookSender>();
        // ── AI assistant — provider selected by Ai:Provider (ollama | azureopenai | none)
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        var aiProvider = (configuration["Ai:Provider"] ?? "none").Trim().ToLowerInvariant();
        switch (aiProvider)
        {
            case "ollama":
                services.AddHttpClient<IAiAssistant, OllamaAiAssistant>();
                break;
            case "azureopenai":
                services.AddHttpClient<IAiAssistant, AzureOpenAiAssistant>();
                break;
            default:
                services.AddSingleton<IAiAssistant, NullAiAssistant>();
                break;
        }

        services.AddDbContext<EdgePulseDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.MigrationsAssembly(
                        typeof(EdgePulseDbContext).Assembly.FullName);
                    // Transient network faults (e.g. Docker port relay hiccups)
                    // are retried instead of surfacing as 500s.
                    b.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<EdgePulseDbContext>());

        return services;
    }
}
