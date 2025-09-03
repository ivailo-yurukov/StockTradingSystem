namespace PortfolioService.Events
{
    public class OrderExecutedEvent
    {
        public string? UserId { get; set; } 
        public string? Ticker { get; set; }
        public int Quantity { get; set; }
        public string? Side { get; set; } 
        public decimal Price { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}
