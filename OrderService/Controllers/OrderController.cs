using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderProcessor _orderProcessor;

        public OrderController(OrderProcessor orderProcessor)
        {
            _orderProcessor = orderProcessor;
        }

        [HttpPost("add/{userId}")]
        public async Task<IActionResult> AddOrder(string userId, [FromBody] OrderRequest request)
        {
            var order = new Order
            {
                UserId = userId,
                Ticker = request.Ticker,
                Quantity = request.Quantity,
                Side = request.Side
            };
            // TODO: Get latest price from PriceService (cache/event-based)
            decimal currentPrice = 100m; // placeholder for now

            var executedEvent = await _orderProcessor.ProcessOrderAsync(order, currentPrice);

            return Ok(executedEvent);
        }
    }
}
