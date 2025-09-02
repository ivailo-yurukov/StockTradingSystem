namespace OrderService.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string? UserId { get; set; }
        public string? Ticker { get; set; }
        public int Quantity { get; set; }
        public string? Side { get; set; }
        public decimal ExecutedPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }
}
