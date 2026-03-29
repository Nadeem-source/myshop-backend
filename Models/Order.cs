namespace RegisterApi.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserEmail { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public string Status { get; set; } = "Placed";
        public string FullName { get; set; }="NA";
        public string Address { get; set; }= "NA";
        public string City { get; set; }= "NA";
        public string State { get; set; }= "NA";
        public string Pincode { get; set; }= "NA";
        public string Country { get; set; }= "NA";
        public string Continent { get; set; }= "NA";
        public string Galaxy { get; set; }= "NA";
        public string Mobile { get; set; }
        public int SellerId { get; set; }=0;

        public string? PaymentMethod { get; set; }   // COD / UPI
        public string? PaymentStatus { get; set; }   // Pending / VerificationPending / Paid
        public string? TransactionId { get; set; }


    }
}
