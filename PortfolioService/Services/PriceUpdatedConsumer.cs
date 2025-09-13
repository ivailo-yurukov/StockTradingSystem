using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;
using PortfolioService.Models;
using Contracts.Events;
using Microsoft.Extensions.Logging;

namespace PortfolioService.Services
{
    public class PriceUpdatedConsumer : IConsumer<IPriceUpdatedEvent>
    {
        private readonly PortfolioDbContext _dbContext;
        private readonly ILogger<PriceUpdatedConsumer> _logger;

        public PriceUpdatedConsumer(PortfolioDbContext dbContext, ILogger<PriceUpdatedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IPriceUpdatedEvent> context)
        {
            var message = context.Message;
            var ticker = (message.Ticker ?? string.Empty).Trim().ToUpperInvariant();

            _logger.LogInformation("Received PriceUpdatedEvent: Ticker={Ticker}, Price={Price}, Timestamp={Timestamp}", ticker, message.Price, message.Timestamp);

            var price = await _dbContext.Prices
                .FirstOrDefaultAsync(p => p.Ticker == ticker);

            if (price == null)
            {
                price = new Price
                {
                    Ticker = ticker,
                    CurrentPrice = message.Price,
                    UpdatedAt = message.Timestamp
                };
                _dbContext.Prices.Add(price);
                _logger.LogDebug("Inserted new price record: Ticker={Ticker}, Price={Price}", ticker, message.Price);
            }
            else
            {
                price.CurrentPrice = message.Price;
                price.UpdatedAt = message.Timestamp;
                _logger.LogDebug("Updated price record: Ticker={Ticker}, Price={Price}", ticker, message.Price);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Price persisted for Ticker={Ticker}", ticker);
        }
    }
}
