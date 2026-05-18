using EdgePulse.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EdgePulse.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                (object)ex.Errors),

            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                ex.Message,
                (object)new {}),

            ForbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                "You do not have permission to perform this action.",
                (object)new {}),

            ConflictException ex => (
                StatusCodes.Status409Conflict,
                ex.Message,
                (object)new {}),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                (object)new {})
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.5.{statusCode - 399}"
        };

        if (errors is IDictionary<string, string[]> validationErrors)
            problemDetails.Extensions["errors"] = validationErrors;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problemDetails,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        await context.Response.WriteAsync(json);
    }
}
