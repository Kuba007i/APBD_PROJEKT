using Microsoft.AspNetCore.Mvc;
using RevenueRecognition.Api.Common.Exceptions;

namespace RevenueRecognition.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (AppException exception)
        {
            await WriteProblemDetailsAsync(
                context,
                exception.StatusCode,
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Wystąpił nieobsłużony błąd aplikacji.");

            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Wystąpił wewnętrzny błąd serwera.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Nieprawidłowe żądanie",
            StatusCodes.Status401Unauthorized => "Nieprawidłowe dane logowania",
            StatusCodes.Status404NotFound => "Nie znaleziono zasobu",
            StatusCodes.Status409Conflict => "Konflikt danych",
            StatusCodes.Status502BadGateway => "Błąd zewnętrznej usługi",
            _ => "Błąd serwera"
        };
    }
}