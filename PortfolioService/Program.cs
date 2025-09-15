using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;
using PortfolioService.Events;
using PortfolioService.Services;
using PortfolioService.Middleware;
using Contracts.Events;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} CorrelationId={CorrelationId} TraceId={TraceId} {Properties}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// OpenTelemetry tracing - configure exporters and instrumentation
var serviceName = "PortfolioService";
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Register ActivitySources used by consumers to create spans
            .AddSource("PortfolioService.OrderExecutedConsumer", "PortfolioService.PriceUpdatedConsumer");
            //.AddJaegerExporter(options =>
            //{
            //    options.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
            //    options.AgentPort = int.TryParse(builder.Configuration["Jaeger:Port"], out var p) ? p : 6831;
            //})
            //.AddZipkinExporter(options =>
            //{
            //    options.Endpoint = new Uri(builder.Configuration["Zipkin:Endpoint"] ?? "http://localhost:9411/api/v2/spans");
            //});
    });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderExecutedConsumer>();
    x.AddConsumer<PriceUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // map to shared contract interfaces
        cfg.Message<IOrderExecutedEvent>(m => m.SetEntityName("order-executed"));
        cfg.Message<IPriceUpdatedEvent>(m => m.SetEntityName("price-updated"));

        cfg.ReceiveEndpoint("portfolio-service", e =>
        {
            e.ConfigureConsumer<OrderExecutedConsumer>(context);
            e.ConfigureConsumer<PriceUpdatedConsumer>(context);
        });
    });
});

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

// Auto apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
