using OrderService.Events;
using OrderService.Models;

namespace OrderService.Interfaces
{
    public interface IOrderProcessor
    {
        Task<OrderExecutedEvent> ProcessOrderAsync(Order order, decimal currentPrice);
    }
}