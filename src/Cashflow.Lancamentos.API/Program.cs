using System.Diagnostics;
using System.Text.Json;
using Cashflow.Lancamentos.API.Application.Validators;
using Cashflow.Lancamentos.API.Infrastructure.Persistence;
using Cashflow.Shared.Infrastructure.Correlation;
using Cashflow.Shared.Messaging.Interfaces;
using Cashflow.Shared.Middleware;
using Cashflow.Shared.Observability;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
var rabbitConnectionString = $"amqp://{rabbitConfig["Username"]}:{rabbitConfig["Password"]}@{rabbitConfig["Host"]}:{rabbitConfig["Port"]}";
var seqUrl = builder.Configuration["Seq:Url"];
var jeagerUrl = builder.Configuration["Jeager:Url"];


// Preparar diretório de logs
var logPath = @"C:\Cashflow\logs";
Directory.CreateDirectory(logPath);

// Logger
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(logPath, "lancamentos.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Host.UseSerilog();

// OpenTelemetry + Jaeger
var activitySource = new ActivitySource("Cashflow.Lancamentos");
builder.Services.AddSingleton(activitySource);
builder.Services.AddOpenTelemetry()
    .WithTracing(t =>
    {
        t.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Cashflow.Lancamentos"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(activitySource.Name)
            .AddJaegerExporter(o =>
            {
                o.AgentHost = "localhost";
                o.AgentPort = 6831;
            });
    });

// MediatR + pipeline
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));

// Core Services
builder.Services.AddScoped<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);
builder.Services.AddHostedService<OutboxMessageDispatcher>();

// FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateLancamentoCommandValidator>());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
});

// DBContext
builder.Services.AddDbContext<LancamentosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sql_server",
        healthQuery: "SELECT 1",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "infra" }
    )
    .AddRabbitMQ(
        rabbitConnectionString: rabbitConnectionString,
        name: "rabbitmq",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "queue", "infra" }
    )
    .AddCheck("eventos_pendentes_na_outbox", () =>
    {
        var pendencias = new Random().Next(0, 100);
        return pendencias < 50
            ? HealthCheckResult.Healthy($"Apenas {pendencias} eventos pendentes.")
            : HealthCheckResult.Unhealthy($"Eventos pendentes em excesso: {pendencias}");
    }, tags: new[] { "custom", "internal" })
    .AddUrlGroup(
        new Uri(seqUrl!),
        name: "seq_log_server",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "infra", "logging" })
    .AddUrlGroup(
        new Uri(jeagerUrl!),
        name: "jeager",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "infra", "observability" });

var app = builder.Build();

// Middlewares padrão
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthorization();
app.MapControllers();

// JSON formatado para retorno do health check
HealthCheckOptions JsonHealthResponse() => new()
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
};

// Endpoints específicos
app.MapHealthChecks("/health", JsonHealthResponse());
app.MapHealthChecks("/readiness", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("infra"),
    ResponseWriter = JsonHealthResponse().ResponseWriter
});
app.MapHealthChecks("/liveness", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();