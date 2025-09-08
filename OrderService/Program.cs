using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Interfaces;
using OrderService.Middleware;
using OrderService.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IOrderProcessor, OrderProcessor>();
builder.Services.AddSingleton<PriceCache>();


//builder.Services.addo.AddOpenTelemetryTracing(b =>
//{
//    b.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OrderService"))
//     .AddAspNetCoreInstrumentation()
//     .AddHttpClientInstrumentation()
//     // .AddSqlClientInstrumentation() // enable if needed
//     // Configure exporter (Jaeger, OTLP, Zipkin)
//     .AddJaegerExporter(options =>
//     {
//         options.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
//         options.AgentPort = int.Parse(builder.Configuration["Jaeger:Port"] ?? "6831");
//     });
//});

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

        cfg.Message<OrderService.Events.PriceUpdatedEvent>(m =>
        {
            m.SetEntityName("price-updated");
        });

        cfg.Message<OrderService.Events.OrderExecutedEvent>(m =>
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

app.UseRequestTracing();           // add tracing middleware early
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
