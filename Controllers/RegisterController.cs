using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using RegisterApi.Models;
using RegisterApi.Helpers;
using RegisterApi.Services;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RegisterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private const string RegOtpSalt = "reg-otp-salt";

        public RegisterController(IConfiguration config, EmailService emailService)
        {
            _config = config;
            _emailService = emailService;
        }

        // ================= SEND OTP =================
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email & Password required." });

            if (!Regex.IsMatch(req.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return BadRequest(new { message = "Invalid email format." });

            string? conn = _config.GetConnectionString("DefaultConnection");

            // 🔹 CHECK EMAIL EXISTS (SP)
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_CheckUserExists", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", req.Email);

                con.Open();
                int exists = (int)cmd.ExecuteScalar();
                if (exists > 0)
                    return BadRequest(new { message = "Email already registered." });
            }

            // 🔹 HASH PASSWORD
            var pwdHasher = new PasswordHasher<string>();
            string hashedPassword = pwdHasher.HashPassword("pwd", req.Password);

            // 🔹 INSERT USER (SP)  👉 IsVerified = 0 (PLAIN)
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_InsertUser", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.Parameters.AddWithValue("@Password", hashedPassword);
                cmd.Parameters.AddWithValue("@IsVerified", 0);
                cmd.Parameters.AddWithValue("@Role", req.Role ?? "User");

                con.Open();
                cmd.ExecuteNonQuery();
            }

            // 🔹 GENERATE OTP
            string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var otpHasher = new PasswordHasher<string>();
            string hashedOtp = otpHasher.HashPassword(RegOtpSalt, otp);

            string expiry = EncryptionHelper.Encrypt(
                DateTime.UtcNow.AddMinutes(10).Ticks.ToString()
            );

            // 🔹 UPDATE OTP (SP)
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_SaveOtp", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.Parameters.AddWithValue("@Otp", hashedOtp);
                cmd.Parameters.AddWithValue("@Expiry", expiry);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            await _emailService.SendEmailAsync(req.Email, "OTP", $"Your OTP is {otp}");

            return Ok(new { message = "OTP sent successfully!" });
        }

        // ================= VERIFY OTP =================
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp(VerifyOtpRequest req)
        {
            string? conn = _config.GetConnectionString("DefaultConnection");
            string dbOtp = "", dbExpiry = "";

            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_GetOtpData", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", req.Email);

                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                        return NotFound(new { message = "Email not found." });

                    dbOtp = r["OTP"]?.ToString() ?? "";
                    dbExpiry = r["OTPExpiry"]?.ToString() ?? "";
                }
            }

            var hasher = new PasswordHasher<string>();
            if (hasher.VerifyHashedPassword(RegOtpSalt, dbOtp, req.OTP ?? "")
                == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Invalid OTP." });

            long ticks = long.Parse(EncryptionHelper.Decrypt(dbExpiry));
            if (DateTime.UtcNow > new DateTime(ticks, DateTimeKind.Utc))
                return BadRequest(new { message = "OTP expired." });

            // 🔹 VERIFY USER (SP) 👉 IsVerified = 1 (PLAIN)
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_VerifyUser", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", req.Email);
                cmd.Parameters.AddWithValue("@IsVerified", 1);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "OTP verified successfully!" });
        }
    }
}
