namespace Notifications.API.Middleware;

using System.Text.Json;
using NotificationsAPI.Domain.Shared;
using Serilog;

/// <summary>
/// Middleware para capturar e tratar exceções não tratadas globalmente.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Uma exceção não tratada foi capturada");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        if (exception is DomainException domainException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            response = new ErrorResponse(domainException.Message, domainException.GetType().Name);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            response = new ErrorResponse(
                "Ocorreu um erro interno no servidor.",
                exception.GetType().Name);
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    private class ErrorResponse
    {
        public ErrorResponse()
        {
        }

        public ErrorResponse(string message, string exceptionType)
        {
            Message = message;
            ExceptionType = exceptionType;
        }

        public string Message { get; set; } = string.Empty;

        public string ExceptionType { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

/// <summary>
/// Extensões para adicionar e usar o middleware de exceção.
/// </summary>
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}
