using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegisterApi.Data;
using RegisterApi.Models;

namespace RegisterApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ================= PLACE ORDER =================
        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder([FromBody] Order order)
        {
            if (order == null)
                return BadRequest("Order payload is null");

            if (string.IsNullOrEmpty(order.UserEmail))
                return BadRequest("UserEmail is required");

            if (order.Items == null || order.Items.Count == 0)
                return BadRequest("Order must contain items");

            // 🔥 Get seller from first product
            var firstProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == order.Items.First().ProductId);

            if (firstProduct == null)
                return BadRequest("Invalid product");

            int sellerId = firstProduct.SellerId;

            // 🔴 Ensure all products same seller
            foreach (var item in order.Items)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null || product.SellerId != sellerId)
                    return BadRequest("All products must belong to same seller");
            }

            order.SellerId = sellerId;
            order.OrderDate = DateTime.Now;
            order.Status = "Placed";

            // 🔥 Payment logic
            if (order.PaymentMethod == "COD")
            {
                order.PaymentStatus = "Pending";
            }
            else if (order.PaymentMethod == "UPI")
            {
                order.PaymentStatus = "VerificationPending";
            }
            else
            {
                return BadRequest("Invalid payment method");
            }

            foreach (var item in order.Items)
            {
                item.Order = order;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 🔥 If UPI, return seller UPI ID
            if (order.PaymentMethod == "UPI")
            {
                var seller = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Id == sellerId);

                return Ok(new
                {
                    message = "Order created. Complete UPI payment.",
                    orderId = order.Id,
                    upiId = seller?.UpiId,
                    sellerName = seller?.Name
                });
            }

            return Ok(new
            {
                message = "Order placed successfully",
                orderId = order.Id
            });
        }

        // ================= GET USER ORDERS =================
        [HttpGet("user/{email}")]
        public async Task<IActionResult> GetOrders(string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserEmail == email)
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // ================= GET ORDER DETAIL =================
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Order not found");

            return Ok(order);
        }

        // ================= UPDATE STATUS =================
        [HttpPut("status/{id}")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            [FromQuery] string status
        )
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated" });
        }
        // ================= RETURN ORDER =================
[HttpPut("return/{id}")]
public async Task<IActionResult> ReturnOrder(int id)
{
    var order = await _context.Orders.FindAsync(id);
    if (order == null)
        return NotFound("Order not found");

    // Already returned?
    if (order.Status == "Returned")
        return BadRequest("Order already returned");

    // Optional: 7 day validation server side
    if ((DateTime.Now - order.OrderDate).TotalDays > 7)
        return BadRequest("Return window expired");

    order.Status = "Returned";

    await _context.SaveChangesAsync();

    return Ok(new { message = "Order returned successfully" });
}
        // ================= DELETE ORDER =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id, [FromQuery] string email)

        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Order not found");

             if (order.UserEmail != email) return Unauthorized();

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully" });
        }
        // VERIFY UPI PAYMENT
        [HttpPut("verify-payment/{id}")]
        public async Task<IActionResult> VerifyPayment(int id, [FromQuery] string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return BadRequest("Transaction ID required");

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound("Order not found");

            order.TransactionId = transactionId;
            order.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment verified successfully" });
        }


    }
}
