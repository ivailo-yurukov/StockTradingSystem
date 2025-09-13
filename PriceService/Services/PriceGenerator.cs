using MassTransit;
using Contracts.Events;

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

                    var roundedPrice = Math.Round(price, 2);
                    var timestamp = DateTime.UtcNow;

                    // publish using the shared contract interface (anonymous object)
                    await _publishEndpoint.Publish<IPriceUpdatedEvent>(new
                    {
                        Ticker = ticker,
                        Price = roundedPrice,
                        Timestamp = timestamp
                    }, stoppingToken);

                    Console.WriteLine($"[PriceService] Published {ticker} at {roundedPrice}");
                }

                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
