using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Interfaces;
using OrderService.Middleware;
using OrderService.Services;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Contracts.Events;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration (enrichments added via middleware/log scopes)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} CorrelationId={CorrelationId} TraceId={TraceId} {Properties}{NewLine}{Exception}")

    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// OpenTelemetry tracing
var serviceName = "OrderService";
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            //.AddEntityFrameworkCoreInstrumentation() // requires package OpenTelemetry.Instrumentation.EntityFrameworkCore
            .AddSource(OrderProcessor.ActivitySourceName, "OrderService.PriceUpdatedConsumer"); // include custom ActivitySources
            //.AddSource("OrderService.Correlation") // optional other sources
                                                   // Exporters - configure endpoints via configuration if needed
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
//builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder =>
//{
//    tracerProviderBuilder
//        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
//        .AddAspNetCoreInstrumentation()
//        .AddHttpClientInstrumentation()
//        //.AddEntityFrameworkCoreInstrumentation() // requires package OpenTelemetry.Instrumentation.EntityFrameworkCore
//        .AddSource(OrderProcessor.ActivitySourceName) // include custom ActivitySources
//        //.AddSource("OrderService.Correlation") // optional other sources
//                                               // Exporters - configure endpoints via configuration if needed
//        .AddJaegerExporter(options =>
//        {
//            options.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
//            options.AgentPort = int.TryParse(builder.Configuration["Jaeger:Port"], out var p) ? p : 6831;
//        })
//        .AddZipkinExporter(options =>
//        {
//            options.Endpoint = new Uri(builder.Configuration["Zipkin:Endpoint"] ?? "http://localhost:9411/api/v2/spans");
//        });
//});

builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();
builder.Services.AddSingleton<PriceCache>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PriceUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // use shared contract interface names for entity routing
        cfg.Message<IPriceUpdatedEvent>(m =>
        {
            m.SetEntityName("price-updated");
        });

        cfg.Message<IOrderExecutedEvent>(m =>
        {
            m.SetEntityName("order-executed");
        });

        cfg.ReceiveEndpoint("order-service", e =>
        {
            e.ConfigureConsumer<PriceUpdatedConsumer>(context);
        });
    });
});

builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
//app.UseGlobalExceptionHandling();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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
