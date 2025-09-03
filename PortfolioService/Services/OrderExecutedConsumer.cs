using MassTransit;
using PortfolioService.Data;
using PortfolioService.Events;
using PortfolioService.Models;
using Microsoft.EntityFrameworkCore;

namespace PortfolioService.Services
{
    public class OrderExecutedConsumer : IConsumer<OrderExecutedEvent>
    {
        private readonly PortfolioDbContext _dbContext;

        public OrderExecutedConsumer(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<OrderExecutedEvent> context)
        {
            var message = context.Message;

            // Find or create a holding for this user + ticker
            var holding = await _dbContext.Holdings
                .FirstOrDefaultAsync(h => h.UserId == message.UserId && h.Ticker == message.Ticker);

            if (holding == null)
            {
                holding = new Holding
                {
                    UserId = message.UserId,
                    Ticker = message.Ticker,
                    Quantity = 0,
                    AveragePrice = 0
                };
                _dbContext.Holdings.Add(holding);
            }

            // Apply order
            if (message.Side == "buy")
            {
                // average price calculation
                var totalCost = (holding.AveragePrice * holding.Quantity) + (message.Price * message.Quantity);
                holding.Quantity += message.Quantity;
                holding.AveragePrice = holding.Quantity > 0 ? totalCost / holding.Quantity : 0;
            }
            else if (message.Side == "sell")
            {
                holding.Quantity -= message.Quantity;
                if (holding.Quantity < 0) holding.Quantity = 0;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
