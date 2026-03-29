using System.ComponentModel.DataAnnotations;

namespace RegisterApi.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public double DiscountPercentage { get; set; }
        public double Rating { get; set; }

        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;

        public string Thumbnail { get; set; } = string.Empty;

        public int Stock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // 🔥 ADD THIS (DO NOT REMOVE ANYTHING)
        public List<string> Images { get; set; } = new();
        public int SellerId { get; set; }
    }
}
