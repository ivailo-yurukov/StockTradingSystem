namespace PortfolioService.Models
{
    public class Holding
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Ticker { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
    }
}