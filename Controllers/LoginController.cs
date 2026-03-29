using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using RegisterApi.Models;
using System.Text.RegularExpressions;

namespace RegisterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] User user)
        {
            try
            {
                if (user == null)
                    return BadRequest(new { message = "Invalid request body" });

                // 1️⃣ Trim
                user.Email = user.Email?.Trim();
                user.Password = user.Password?.Trim();

                // 2️⃣ Validation
                if (string.IsNullOrEmpty(user.Email))
                    return BadRequest(new { message = "Email is required." });

                if (string.IsNullOrEmpty(user.Password))
                    return BadRequest(new { message = "Password is required." });

                if (!Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return BadRequest(new { message = "Invalid email format." });

                if (user.Password.Length < 6)
                    return BadRequest(new { message = "Password must be at least 6 characters." });

                string? conn = _configuration.GetConnectionString("DefaultConnection");

                string hashedPasswordDb = "";
                string isVerifiedDb = "";
                string roleDb = "";   // ✅ ADDED (as you asked, nothing removed)
                string userIdDb = "";

                // 3️⃣ CALL STORED PROCEDURE
                using (SqlConnection con = new SqlConnection(conn))
                using (SqlCommand cmd = new SqlCommand("sp_LoginUser", con))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Email", user.Email);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return NotFound(new { message = "Email not registered." });

                        hashedPasswordDb = reader["Password"]?.ToString() ?? "";
                        isVerifiedDb = reader["IsVerified"]?.ToString() ?? "";
                        roleDb = reader["Role"]?.ToString() ?? "User"; // fallback safety
                        userIdDb = reader["SellerId"]?.ToString() ?? "";
                    }
                }

                if (string.IsNullOrEmpty(hashedPasswordDb))
                    return StatusCode(500, new { message = "Password data corrupted" });

                if (string.IsNullOrEmpty(isVerifiedDb))
                    return StatusCode(500, new { message = "Verification data missing" });

                // 4️⃣ CHECK IsVerified
                if (isVerifiedDb != "1")
                {
                    return BadRequest(new
                    {
                        message = "Email not verified. Please verify OTP."
                    });
                }

                // 5️⃣ Verify Password
                var hasher = new PasswordHasher<string>();
                var result = hasher.VerifyHashedPassword(
                    "pwd",
                    hashedPasswordDb,
                    user.Password
                );

                if (result == PasswordVerificationResult.Failed)
                    return BadRequest(new { message = "Incorrect password." });

                // 6️⃣ SUCCESS (ROLE INCLUDED ✅)
                return Ok(new
                {
                    message = "Login successful",
                    email = user.Email,
                    role = roleDb,
                    sellerId = userIdDb   // 🔥 ADD THIS
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }
}
