using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EdgePulse.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public LoggingBehaviour(
        ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
        ICurrentUserService currentUser)
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
        var userId = _currentUser.UserId ?? "anonymous";
        var tenantId = _currentUser.IsAuthenticated
            ? _currentUser.TenantId.ToString()
            : "none";

        _logger.LogInformation(
            "EdgePulse Request: {RequestName} UserId: {UserId} TenantId: {TenantId}",
            requestName, userId, tenantId);

        var response = await next();

        _logger.LogInformation(
            "EdgePulse Response: {RequestName} completed",
            requestName);

        return response;
    }
}
