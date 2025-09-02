namespace OrderService.Models
{
    public class User
    {
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<Order> Orders { get; set; } = new();
    }
}
