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
            _priceCache.UpdatePrice(msg.Ticker, msg.Price);
            Console.WriteLine($"[OrderService] Updated price for {msg.Ticker}: {msg.Price}");
            return Task.CompletedTask;
        }
    }
}
