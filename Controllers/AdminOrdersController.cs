using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegisterApi.Data;

namespace RegisterApi.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET ALL ORDERS (ADMIN ONLY)
        [HttpGet]
        public async Task<IActionResult> GetAllOrders(
            [FromHeader(Name = "role")] string role,
            [FromQuery] int sellerId
        )
        {
            if (string.IsNullOrEmpty(role) || role != "Admin")
                return Unauthorized(new { message = "Admin access required" });

            var orders = await _context.Orders
    .Include(o => o.Items)
            .ThenInclude(i => i.Product) // 🔥 IMPORTANT

    .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId)) // 🔥 FILTER
    .OrderByDescending(o => o.OrderDate)
    .ToListAsync();

            return Ok(orders);
        }

        // ✅ UPDATE ORDER STATUS
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromBody] string status,
            [FromHeader(Name = "role")] string role
        )
        {
            if (string.IsNullOrEmpty(role) || role != "Admin")
                return Unauthorized(new { message = "Admin access required" });

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            order.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }
        // ================= VERIFY UPI PAYMENT =================
        [HttpPut("{id}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(
            int id,
            [FromHeader(Name = "role")] string role
        )
        {
            if (string.IsNullOrEmpty(role) || role != "Admin")
                return Unauthorized(new { message = "Admin access required" });

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            if (order.PaymentMethod != "UPI")
                return BadRequest(new { message = "Not a UPI order" });

            order.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment marked as Paid" });
        }
        [HttpPut("{id}/cod-paid")]
        public async Task<IActionResult> MarkCodPaid(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            order.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            return Ok(order);
        }
    }
}
