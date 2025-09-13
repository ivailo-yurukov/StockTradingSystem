using MassTransit;
using Contracts.Events;

namespace OrderService.Services
{
    public class PriceUpdatedConsumer : IConsumer<IPriceUpdatedEvent>
    {
        private readonly PriceCache _priceCache;
        private readonly ILogger<PriceUpdatedConsumer> _logger;

        public PriceUpdatedConsumer(PriceCache priceCache, ILogger<PriceUpdatedConsumer> logger)
        {
            _priceCache = priceCache;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<IPriceUpdatedEvent> context)
        {
            var msg = context.Message;
            var ticker = (msg.Ticker ?? string.Empty).Trim().ToUpperInvariant();
            _priceCache.UpdatePrice(ticker, msg.Price);
            _logger.LogInformation("[OrderService] Consumer received {Ticker} => {Price}", ticker, msg.Price);
            return Task.CompletedTask;
        }
    }
}
