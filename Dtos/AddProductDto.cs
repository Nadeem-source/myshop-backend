using Microsoft.AspNetCore.Http;
namespace RegisterApi.Dtos 
{
    public class AddProductDto 
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public double DiscountPercentage { get; set; } 
        public double Rating { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string? Thumbnail { get; set; } 
        // 🔥 NEW: multiple internet URLs
        public List<string>? ImageUrls { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public int Stock { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } ="InStock";
        public int SellerId { get; set; }
    }
}