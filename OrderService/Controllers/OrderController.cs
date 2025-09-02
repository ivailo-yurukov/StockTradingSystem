using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;

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
        public IActionResult AddOrder(string userId, [FromBody] Order request)
        {
            // TODO: Get latest price from PriceService (cache/event-based)
            decimal currentPrice = 100m; // placeholder for now

            request.UserId = userId;
            var executedEvent = _orderProcessor.ProcessOrderAsync(request, currentPrice);

            return Ok(executedEvent);
        }
    }
}
