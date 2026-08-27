using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.API.Middleware;
using Notifications.Domain.Notifications;
using Shouldly;
using Xunit;

namespace Notifications.API.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenDomainExceptionIsThrown_ReturnsBadRequestWithMessage()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidNotificationEmailException("invalido"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().ShouldBe("Email inválido fornecido: 'invalido'");
        body.GetProperty("exceptionType").GetString().ShouldBe(nameof(InvalidNotificationEmailException));
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericExceptionIsThrown_ReturnsInternalServerErrorWithGenericMessage()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        var body = await ReadResponseBodyAsync(context);
        body.GetProperty("message").GetString().ShouldBe("Ocorreu um erro interno no servidor.");
        body.GetProperty("exceptionType").GetString().ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionIsThrown_DoesNotChangeResponse()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private static ExceptionMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        return new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };
    }

    private static async Task<JsonElement> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        return JsonDocument.Parse(json).RootElement;
    }
}
