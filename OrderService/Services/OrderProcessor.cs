using MassTransit;
using OrderService.Data;
using OrderService.Events;
using OrderService.Interfaces;
using OrderService.Models;
using Contracts.Events;

namespace OrderService.Services
{
    public class OrderProcessor : IOrderProcessor
    {
        private readonly OrderDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderProcessor(OrderDbContext dbContext, IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
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

            var executedEvent = new OrderExecutedEvent
            {
                UserId = order.UserId,
                Ticker = order.Ticker,
                Quantity = order.Quantity,
                Side = order.Side,
                Price = currentPrice,
                ExecutedAt = order.CreatedAt
            };

            // Publish using the shared contract interface
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
                });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to publish event to RabbitMQ.", ex);
            }

            return executedEvent;
        }
    }
}
