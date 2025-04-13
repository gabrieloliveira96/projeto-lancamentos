using System.Net;
using System.Text.Json;
using Cashflow.Shared.Infrastructure.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Cashflow.Shared.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger logger)
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
            // resolve o scoped service manualmente
            var correlationContext = context.RequestServices.GetService<ICorrelationContext>();

            _logger.Error(ex, "Erro não tratado. CorrelationId: {CorrelationId}", correlationContext?.CorrelationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                status = context.Response.StatusCode,
                error = "Erro interno do servidor",
                correlationId = correlationContext?.CorrelationId ?? string.Empty
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
