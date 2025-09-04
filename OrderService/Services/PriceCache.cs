using System.Collections.Concurrent;

namespace OrderService.Services
{
    public class PriceCache
    {
        private readonly ConcurrentDictionary<string, decimal> _prices = new();

        public void UpdatePrice(string ticker, decimal price)
        {
            _prices[ticker] = price;
        }

        public decimal GetPrice(string ticker)
        {
            return _prices.TryGetValue(ticker, out var price) ? price : 0m;
        }
    }
}
