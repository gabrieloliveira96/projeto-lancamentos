using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cashflow.Shared.Infrastructure.Correlation;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

public async Task InvokeAsync(HttpContext context, ICorrelationContext correlation)
    {
        // Tenta pegar o ID do header, senão gera um novo
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        correlation.CorrelationId = correlationId;

        // Coloca no contexto de log do Serilog
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            await _next(context);
        }
    }
}
