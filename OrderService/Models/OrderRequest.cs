namespace OrderService.Models
{
    public class OrderRequest
    {
        public string? Ticker { get; set; }
        public int Quantity { get; set; }
        public string? Side { get; set; } 
    }
}
