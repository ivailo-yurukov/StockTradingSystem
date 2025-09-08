using MassTransit;
using OrderService.Data;
using OrderService.Events;
using OrderService.Interfaces;
using OrderService.Models;
using System.Diagnostics;

namespace OrderService.Services
{
    public class OrderProcessor : IOrderProcessor
    {
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

            // Create event
            var executedEvent = new OrderExecutedEvent
            {
                UserId = order.UserId,
                Ticker = order.Ticker,
                Quantity = order.Quantity,
                Side = order.Side,
                Price = currentPrice,
                ExecutedAt = order.CreatedAt
            };

            // Publish event with trace headers and correlation id
            var traceId = Activity.Current?.TraceId.ToHexString();
            var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

            try
            {
                await _publishEndpoint.Publish(executedEvent, ctx =>
                {
                    ctx.CorrelationId = Guid.NewGuid(); 
                    if (!string.IsNullOrEmpty(traceId)) ctx.Headers.Set("trace-id", traceId);
                    ctx.Headers.Set("request-id", requestId);
                    ctx.Headers.Set("user-id", order.UserId ?? string.Empty);
                });
                _logger.LogInformation("Published OrderExecutedEvent for User={UserId} Ticker={Ticker} Price={Price} TraceId={TraceId} RequestId={RequestId}",
                    order.UserId, order.Ticker, currentPrice, traceId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderExecutedEvent for User={UserId}", order.UserId);
                throw new ApplicationException("Failed to publish event to RabbitMQ.", ex);
            }

            return executedEvent;
        }
    }
}
