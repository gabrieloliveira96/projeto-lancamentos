using Cashflow.Consolidado.API.Infrastructure.Persistence;
using Cashflow.Consolidado.API.Application.Handlers;
using Cashflow.Consolidado.API.Infrastructure.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Cashflow.Shared.Infrastructure.Correlation;
using Cashflow.Shared.Middleware;

var logPath = @"C:\Cashflow\logs";
Directory.CreateDirectory(logPath);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(logPath, "consolidados.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq("http://localhost:5341") // ou endereço do seu servidor Seq
    .CreateLogger();
    
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
    c.UseInlineDefinitionsForEnums();
});

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMediatR(typeof(Program));
builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

builder.Services.AddScoped<ProcessLancamentoEventHandler>();
builder.Services.AddHostedService<RabbitMqConsumerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
