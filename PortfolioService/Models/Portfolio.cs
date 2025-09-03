namespace PortfolioService.Models
{
    public class Portfolio
    {
        public string? UserId { get; set; }
        public List<Holding> Holdings { get; set; } = new();
    }
}
