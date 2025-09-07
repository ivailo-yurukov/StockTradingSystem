using System.Collections.Concurrent;

namespace OrderService.Services
{
    public class PriceCache
    {
        private readonly ConcurrentDictionary<string, decimal> _prices = new();

        public void UpdatePrice(string ticker, decimal price)
        { 
            var key = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            _prices[key] = price;
            Console.WriteLine($"[PriceCache] Updated {key} => {price}");
        }

        public decimal GetPrice(string ticker)
        {
            var key = (ticker ?? string.Empty).Trim().ToUpperInvariant();
            return _prices.TryGetValue(key, out var price) ? price : 0m;
        }
    }
}
