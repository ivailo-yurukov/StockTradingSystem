using System.Collections.Concurrent;

namespace OrderService.Services
{
    public class PriceCache
    {
        private readonly ConcurrentDictionary<string, decimal> _prices = new();
        private readonly ILogger<PriceCache> _logger;

        public PriceCache(ILogger<PriceCache> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void UpdatePrice(string ticker, decimal price)
        {
            var key = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            _prices[key] = price;
            _logger.LogInformation("[PriceCache] Updated {Ticker} => {Price}", key, price);
        }

        public decimal GetPrice(string ticker)
        {
            var key = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            if (_prices.TryGetValue(key, out var price))
            {
                _logger.LogInformation("[PriceCache] Retrieved {Ticker} => {Price}", key, price);
                return price;
            }

            _logger.LogInformation("[PriceCache] No price available for {Ticker}", key);
            return 0m;
        }
    }
}
