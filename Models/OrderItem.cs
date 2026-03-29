using System.Text.Json.Serialization;

namespace RegisterApi.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        // 🔥 FOREIGN KEY
        public int? OrderId { get; set; }
        [JsonIgnore]
        public Order? Order { get; set; } = null!;
        public Product Product { get; set; }  // 🔥 MUST

        public int ProductId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Thumbnail { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}
