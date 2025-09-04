using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Interfaces;
using OrderService.Models;
using OrderService.Services;
using System;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderProcessor _orderProcessor;
        private readonly PriceCache _priceCache;

        public OrderController(OrderProcessor orderProcessor, PriceCache priceCache)
        {
            _orderProcessor = orderProcessor;
            _priceCache = priceCache;
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

            decimal currentPrice = _priceCache.GetPrice(order.Ticker);
            if (currentPrice == 0m)
            {
                return BadRequest($"No price available for {order.Ticker} yet.");
            }

            var executedEvent = await _orderProcessor.ProcessOrderAsync(order, currentPrice);

            return Ok(executedEvent);
        }
    }
}
