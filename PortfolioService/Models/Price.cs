namespace PortfolioService.Models
{
    public class Price
    {
        public int Id { get; set; }
        public string? Ticker { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
