using MassTransit;
using PriceService.Events;

namespace PriceService.Services
{
    public class PriceGenerator : BackgroundService
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly string[] _tickers = { "AAPL", "TSLA", "NVDA" };
        private readonly Random _random = new();

        public PriceGenerator(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var ticker in _tickers)
                {
                    var price = (decimal)(_random.NextDouble() * 1000 + 50); // random price between 50–1050

                    var priceUpdate = new PriceUpdatedEvent
                    {
                        Ticker = ticker,
                        Price = Math.Round(price, 2),
                        Timestamp = DateTime.UtcNow
                    };

                    await _publishEndpoint.Publish(priceUpdate, stoppingToken);
                    Console.WriteLine($"[PriceService] Published {ticker} at {priceUpdate.Price}");
                }

                await Task.Delay(1000, stoppingToken); 
            }
        }
    }
}
