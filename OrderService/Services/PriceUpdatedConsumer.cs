using MassTransit;
using OrderService.Events;

namespace OrderService.Services
{
    public class PriceUpdatedConsumer : IConsumer<PriceUpdatedEvent>
    {
        private readonly PriceCache _priceCache;

        public PriceUpdatedConsumer(PriceCache priceCache)
        {
            _priceCache = priceCache;
        }

        public Task Consume(ConsumeContext<PriceUpdatedEvent> context)
        {
            var msg = context.Message;
            var ticker = (msg.Ticker ?? string.Empty).Trim().ToUpperInvariant();
            _priceCache.UpdatePrice(ticker, msg.Price);
            Console.WriteLine($"[OrderService] Consumer received {ticker} => {msg.Price}");
            return Task.CompletedTask;
        }
    }
}
