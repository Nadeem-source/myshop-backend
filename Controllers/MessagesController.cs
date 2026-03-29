using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RegisterApi.Data;
using RegisterApi.Models;

namespace RegisterApi.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessagesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SellerMessage msg)
        {
            if (msg == null || msg.SellerId == 0 || string.IsNullOrEmpty(msg.Message))
                return BadRequest("Invalid data");

            msg.CreatedAt = DateTime.Now;

            _context.SellerMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Ok(msg);
        }

        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetSellerMessages(int sellerId)
        {
            var messages = await _context.SellerMessages
                .Where(x => x.SellerId == sellerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPut("reply/{id}")]
        public async Task<IActionResult> Reply(int id, [FromBody] string reply)
        {
            var msg = await _context.SellerMessages.FindAsync(id);

            if (msg == null)
                return NotFound();

            msg.Reply = reply;

            await _context.SaveChangesAsync();

            return Ok(msg);
        }
        [HttpGet("customer/{email}/{productId}")]
        public async Task<IActionResult> GetCustomerMessages(string email, int productId)
        {
            var messages = await _context.SellerMessages
                .Where(x => x.CustomerEmail == email && x.ProductId == productId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }
    }
}
