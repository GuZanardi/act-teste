using System.Net;
using System.Text.Json;
using FinancialAccount.API.DTOs;
using FinancialAccount.API.Exceptions;

namespace FinancialAccount.API.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponseDto
        {
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case InsufficientBalanceException insufficientEx:
                response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                errorResponse.Message = insufficientEx.Message;
                _logger.LogWarning(insufficientEx, "Tentativa de movimentação com saldo insuficiente.");
                break;

            case BusinessException businessEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = businessEx.Message;
                _logger.LogWarning(businessEx, "Violação de regra de negócio.");
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = notFoundEx.Message;
                _logger.LogWarning(notFoundEx, "Recurso não encontrado.");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "Ocorreu um erro interno inesperado no servidor.";
                errorResponse.Details = exception.Message;
                _logger.LogError(exception, "Erro não tratado capturado pelo middleware global.");
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);

        await response.WriteAsync(json);
    }
}
