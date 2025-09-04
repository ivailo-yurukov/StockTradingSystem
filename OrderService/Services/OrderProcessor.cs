using MassTransit;
using OrderService.Data;
using OrderService.Events;
using OrderService.Interfaces;
using OrderService.Models;

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

            //Publish event to RabbitMQ
            await _publishEndpoint.Publish(executedEvent);

            return executedEvent;
        }
    }
}
