using System.Net;
using System.Text.Json;
using FluentAssertions;
using FinancialAccount.API.DTOs;
using FinancialAccount.API.Exceptions;
using FinancialAccount.API.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinancialAccount.Tests;

/// <summary>
/// Testa o GlobalExceptionHandlerMiddleware verificando que cada tipo de exceção
/// gera o status HTTP e o corpo JSON correto — sem precisar subir a aplicação completa.
/// </summary>
public class GlobalExceptionMiddlewareTests
{
    // Cria um DefaultHttpContext com MemoryStream de resposta para inspecionar o output
    private static (HttpContext context, MemoryStream responseBody) CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        return (context, responseBody);
    }

    private static ILogger<GlobalExceptionHandlerMiddleware> CreateMockLogger()
        => new Mock<ILogger<GlobalExceptionHandlerMiddleware>>().Object;

    private static async Task<(int statusCode, ErrorResponseDto? body)> InvokeMiddleware(
        Exception exceptionToThrow)
    {
        var (context, responseBody) = CreateHttpContext();

        // Simula o próximo middleware lançando a exceção
        RequestDelegate next = _ => throw exceptionToThrow;

        var middleware = new GlobalExceptionHandlerMiddleware(next, CreateMockLogger());
        await middleware.InvokeAsync(context);

        // Lê e desserializa a resposta JSON
        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody).ReadToEndAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var errorBody = JsonSerializer.Deserialize<ErrorResponseDto>(json, options);

        return (context.Response.StatusCode, errorBody);
    }

    [Fact]
    public async Task InsufficientBalanceException_ShouldReturn422WithCorrectMessage()
    {
        // Arrange
        var exception = new InsufficientBalanceException(requestedAmount: 500m, currentBalance: 100m);

        // Act
        var (statusCode, body) = await InvokeMiddleware(exception);

        // Assert
        statusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity); // 422
        body.Should().NotBeNull();
        body!.Message.Should().Contain("500");
        body.Message.Should().Contain("100");
    }

    [Fact]
    public async Task BusinessException_ShouldReturn400WithCorrectMessage()
    {
        // Arrange
        var exception = new BusinessException("O valor da transação deve ser maior que zero.");

        // Act
        var (statusCode, body) = await InvokeMiddleware(exception);

        // Assert
        statusCode.Should().Be((int)HttpStatusCode.BadRequest); // 400
        body.Should().NotBeNull();
        body!.Message.Should().Be("O valor da transação deve ser maior que zero.");
    }

    [Fact]
    public async Task NotFoundException_ShouldReturn404WithCorrectMessage()
    {
        // Arrange
        var exception = new NotFoundException("Conta com ID '00000000-0000-0000-0000-000000000001' não foi encontrada.");

        // Act
        var (statusCode, body) = await InvokeMiddleware(exception);

        // Assert
        statusCode.Should().Be((int)HttpStatusCode.NotFound); // 404
        body.Should().NotBeNull();
        body!.Message.Should().Contain("não foi encontrada");
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500WithGenericMessage()
    {
        // Arrange — qualquer exceção não mapeada deve resultar em 500
        var exception = new InvalidOperationException("Erro inesperado interno.");

        // Act
        var (statusCode, body) = await InvokeMiddleware(exception);

        // Assert
        statusCode.Should().Be((int)HttpStatusCode.InternalServerError); // 500
        body.Should().NotBeNull();
        body!.Message.Should().Be("Ocorreu um erro interno inesperado no servidor.");
        body.Details.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AnyException_ShouldReturnJsonContentType()
    {
        // Arrange
        var exception = new BusinessException("Qualquer erro.");

        // Act
        var (context, responseBody) = CreateHttpContext();
        RequestDelegate next = _ => throw exception;
        var middleware = new GlobalExceptionHandlerMiddleware(next, CreateMockLogger());
        await middleware.InvokeAsync(context);

        // Assert — Content-Type deve ser application/json independentemente do erro
        context.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task AnyException_ResponseBody_ShouldContainTimestamp()
    {
        // Arrange
        var exception = new BusinessException("Erro com timestamp.");

        // Act
        var (statusCode, body) = await InvokeMiddleware(exception);

        // Assert
        body.Should().NotBeNull();
        body!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }
}
