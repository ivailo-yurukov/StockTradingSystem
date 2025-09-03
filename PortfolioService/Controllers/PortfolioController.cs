using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PortfolioService.Data;
using Microsoft.EntityFrameworkCore;

namespace PortfolioService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly PortfolioDbContext _dbContext;

        public PortfolioController(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPortfolio(string userId)
        {
            var holdings = await _dbContext.Holdings
         .Where(h => h.UserId == userId)
         .ToListAsync();

            var prices = await _dbContext.Prices.ToListAsync();

            var result = holdings
                .Select(holding =>
                {
                    // find the latest price for this holding (or 0 if not found)
                    var price = prices.FirstOrDefault(p => p.Ticker == holding.Ticker);
                    var latestPrice = price != null ? price.CurrentPrice : 0;

                    // calculate market value
                    var marketValue = holding.Quantity * latestPrice;

                    return new
                    {
                        Ticker = holding.Ticker,
                        Quantity = holding.Quantity,
                        AveragePrice = holding.AveragePrice,
                        CurrentPrice = latestPrice,
                        MarketValue = marketValue
                    };
                });

            return Ok(result);
        }
    }
}
