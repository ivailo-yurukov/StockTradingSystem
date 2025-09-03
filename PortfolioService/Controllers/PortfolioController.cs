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

            return Ok(holdings);
        }
    }
}
