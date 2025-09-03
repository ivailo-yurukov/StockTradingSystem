using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;
using PortfolioService.Events;
using PortfolioService.Models;

namespace PortfolioService.Services
{
    public class PriceUpdatedConsumer: IConsumer<PriceUpdatedEvent>
    {
        private readonly PortfolioDbContext _dbContext;

        public PriceUpdatedConsumer(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<PriceUpdatedEvent> context)
        {
            var message = context.Message;

            var price = await _dbContext.Prices
                .FirstOrDefaultAsync(p => p.Ticker == message.Ticker);

            if (price == null)
            {
                price = new Price
                {
                    Ticker = message.Ticker,
                    CurrentPrice = message.Price,
                    UpdatedAt = message.Timestamp
                };
                _dbContext.Prices.Add(price);
            }
            else
            {
                price.CurrentPrice = message.Price;
                price.UpdatedAt = message.Timestamp;
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
