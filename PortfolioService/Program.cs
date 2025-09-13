using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;
using PortfolioService.Events;
using PortfolioService.Services;
using Contracts.Events;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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
