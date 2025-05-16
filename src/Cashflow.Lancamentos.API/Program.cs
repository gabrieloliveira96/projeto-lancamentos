using System.Diagnostics;
using Cashflow.Lancamentos.API.Application.Validators;
using Cashflow.Lancamentos.API.Infrastructure.Messaging;
using Cashflow.Lancamentos.API.Infrastructure.Persistence;
using Cashflow.Lancamentos.API.Observability;
using Cashflow.Shared.Infrastructure.Correlation;
using Cashflow.Shared.Messaging.Interfaces;
using Cashflow.Shared.Middleware;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var logPath = @"C:\Cashflow\logs";
Directory.CreateDirectory(logPath);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(logPath, "lancamentos.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341") // ou endereço do seu servidor Seq
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(t =>
    {
        t
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("Cashflow.Lancamentos"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Cashflow.Lancamentos")
            .AddJaegerExporter(o =>
            {
                o.AgentHost = "localhost";
                o.AgentPort = 6831;
            });
    });



builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddMediatR(typeof(Program));
builder.Services.AddScoped<IMessageBus, RabbitMqMessageBus>();
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();
builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);
builder.Services.AddHostedService<OutboxMessageDispatcher>();

builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<CreateLancamentoCommandValidator>());

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<LancamentosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
