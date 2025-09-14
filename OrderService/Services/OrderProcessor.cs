using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Data;
using OrderService.Events;
using OrderService.Interfaces;
using OrderService.Models;
using Contracts.Events;
using Serilog.Context;
using LogContext = Serilog.Context.LogContext;

namespace OrderService.Services
{
    public class OrderProcessor : IOrderProcessor
    {
        public const string ActivitySourceName = "OrderService.OrderProcessor";
        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        private readonly OrderDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderProcessor> _logger;

        public OrderProcessor(OrderDbContext dbContext, IPublishEndpoint publishEndpoint, ILogger<OrderProcessor> logger)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<OrderExecutedEvent> ProcessOrderAsync(Order order, decimal currentPrice)
        {
            // derive correlation and trace id for scope
            var correlationId = Activity.Current?.Tags.FirstOrDefault(t => t.Key == "correlation_id").Value
                                ?? Activity.Current?.TraceId.ToString()
                                ?? Guid.NewGuid().ToString();

            var traceId = Activity.Current?.TraceId.ToString() ?? string.Empty;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", traceId))
            using (_logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["TraceId"] = traceId,
                ["UserId"] = order.UserId ?? string.Empty,
                ["Ticker"] = order.Ticker ?? string.Empty
            }))
            {
                _logger.LogInformation("Processing order: UserId={UserId}, Ticker={Ticker}, Quantity={Quantity}, Side={Side}",
                    order.UserId, order.Ticker, order.Quantity, order.Side);

                // Create a span for DB persist
                using (var dbActivity = ActivitySource.StartActivity("SaveOrderToDb", ActivityKind.Internal))
                {
                    // Ensure user exists
                    var user = await _dbContext.Users.FindAsync(order.UserId);
                    if (user == null)
                    {
                        user = new User { UserId = order.UserId };
                        _dbContext.Users.Add(user);
                    }

                    // Save order
                    order.ExecutedPrice = currentPrice;
                    order.CreatedAt = DateTime.UtcNow;
                    _dbContext.Orders.Add(order);

                    await _dbContext.SaveChangesAsync();
                    _logger.LogDebug("Order saved: OrderId={OrderId}, UserId={UserId}, Ticker={Ticker}", order.OrderId, order.UserId, order.Ticker);
                }

                var executedEvent = new OrderExecutedEvent
                {
                    UserId = order.UserId,
                    Ticker = order.Ticker,
                    Quantity = order.Quantity,
                    Side = order.Side,
                    Price = currentPrice,
                    ExecutedAt = order.CreatedAt
                };

                // Publish with a publish span
                using (var publishActivity = ActivitySource.StartActivity("PublishOrderExecuted", ActivityKind.Producer))
                {
                    publishActivity?.AddTag("message_type", nameof(IOrderExecutedEvent));
                    publishActivity?.AddTag("user_id", executedEvent.UserId ?? string.Empty);
                    publishActivity?.AddTag("ticker", executedEvent.Ticker ?? string.Empty);

                    try
                    {
                        await _publishEndpoint.Publish<IOrderExecutedEvent>(new
                        {
                            UserId = executedEvent.UserId,
                            Ticker = executedEvent.Ticker,
                            Quantity = executedEvent.Quantity,
                            Side = executedEvent.Side,
                            Price = executedEvent.Price,
                            ExecutedAt = executedEvent.ExecutedAt
                        }, context =>
                        {
                            // forward correlation and W3C trace headers
                            context.Headers.Set("X-Correlation-ID", correlationId);
                            if (Activity.Current != null)
                            {
                                context.Headers.Set("traceparent", Activity.Current.Id);
                                context.Headers.Set("tracestate", Activity.Current.TraceStateString ?? string.Empty);
                            }
                        });

                        _logger.LogInformation("Published OrderExecutedEvent: UserId={UserId}, Ticker={Ticker}", executedEvent.UserId, executedEvent.Ticker);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish OrderExecutedEvent for UserId={UserId}, Ticker={Ticker}", executedEvent.UserId, executedEvent.Ticker);
                        throw;
                    }
                }

                return executedEvent;
            }
        }
    }
}
