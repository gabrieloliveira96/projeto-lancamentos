using System.Diagnostics;
using System.Text.Json;
using Cashflow.Consolidado.API.Application.Handlers;
using Cashflow.Consolidado.API.Infrastructure.Messaging;
using Cashflow.Consolidado.API.Infrastructure.Persistence;
using Cashflow.Shared.Domain.Interface;
using Cashflow.Shared.Infrastructure.Correlation;
using Cashflow.Shared.Messaging.Events;
using Cashflow.Shared.Messaging.Interfaces;
using Cashflow.Shared.Middleware;
using Cashflow.Shared.Observability;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
var rabbitConnectionString = $"amqp://{rabbitConfig["Username"]}:{rabbitConfig["Password"]}@{rabbitConfig["Host"]}:{rabbitConfig["Port"]}";
var seqUrl = builder.Configuration["Seq:Url"];
var jeagerUrl = builder.Configuration["Jeager:Url"];

// 🔧 Logger + Seq
var logPath = @"C:\Cashflow\logs";

Directory.CreateDirectory(logPath);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(logPath, "consolidados.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(seqUrl)
    .CreateLogger();

builder.Host.UseSerilog();

// 🔧 Observability: Jaeger + ActivitySource
var activitySource = new ActivitySource("Cashflow.Consolidado");
builder.Services.AddSingleton(activitySource);

builder.Services.AddOpenTelemetry()
    .WithTracing(t =>
    {
        t.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Cashflow.Consolidado"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(activitySource.Name)
            .AddJaegerExporter(o =>
            {
                o.AgentHost = "localhost";
                o.AgentPort = 6831;
            });
    });

// 🔧 Services
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
builder.Services.AddScoped<MessageHandlerExecutor>();
builder.Services.AddScoped<IMessageEventHandler<LancamentoCriadoEvent>, ProcessLancamentoEventHandler>();
builder.Services.AddHostedService<RabbitMqConsumerService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);


builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 🔧 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
});

// 🔧 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 🔧 Health Checks
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

// 🔧 Pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();
app.MapControllers();

// 🔧 JSON custom para Health Check
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

// 🔧 Health Endpoints
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
