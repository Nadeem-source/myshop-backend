namespace RegisterApi.Models
{
    public class SellerMessage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string? CustomerEmail { get; set; }

        public int SellerId { get; set; }
        public string? Message { get; set; }

        public string? Reply { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
