namespace OrderService.Events
{
    public class PriceUpdatedEvent
    {
        public string? Ticker { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
