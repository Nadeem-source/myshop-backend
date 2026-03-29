using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using RegisterApi.Models;
using RegisterApi.Helpers;

namespace RegisterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResetPasswordController : ControllerBase
    {
        private readonly IConfiguration _config;
        private const string FtpOtpSalt = "ftp-otp-salt";
        private const string PwdSalt = "pwd-salt";

        public ResetPasswordController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.OTPftp))
                return BadRequest(new { message = "Email & OTP required" });

            string? conn = _config.GetConnectionString("DefaultConnection");
            string email = req.Email.Trim();
            string hashedOtpDb = "";
            string encExpiry = "";
            string encIsVerified = "";

            using (SqlConnection con = new SqlConnection(conn))
            {
                const string q = "SELECT OTPftp, OTPftpExpiry, IsVerifiedOTPftp FROM Users WHERE Email=@Email";
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        return NotFound(new { message = "Invalid email" });

                    hashedOtpDb = reader["OTPftp"] as string ?? "";
                    encExpiry = reader["OTPftpExpiry"] as string ?? "";
                    encIsVerified = reader["IsVerifiedOTPftp"] as string ?? "";
                }
            }

            if (string.IsNullOrEmpty(hashedOtpDb) || string.IsNullOrEmpty(encExpiry))
                return BadRequest(new { message = "OTP not generated" });

            // Verify OTP
            var hasher = new PasswordHasher<string>();
            var result = hasher.VerifyHashedPassword(FtpOtpSalt, hashedOtpDb, req.OTPftp);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Invalid OTP" });

            // Check expiry
            string decExpiry = EncryptionHelper.Decrypt(encExpiry);
            if (!long.TryParse(decExpiry, out long ticks))
                return StatusCode(500, new { message = "Expiry decrypt failed" });

            var expiryTime = new DateTime(ticks, DateTimeKind.Utc);
            if (DateTime.UtcNow > expiryTime)
                return BadRequest(new { message = "OTP expired" });

            // Mark OTP as verified (encrypted)
            using (SqlConnection con = new SqlConnection(conn))
            {
                const string upd = "UPDATE Users SET IsVerifiedOTPftp=@IsVerified WHERE Email=@Email";
                using (SqlCommand cmd = new SqlCommand(upd, con))
                {
                    cmd.Parameters.AddWithValue("@IsVerified", EncryptionHelper.Encrypt("1"));
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "OTP verified" });
        }

        [HttpPost("reset")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email & Password required" });

            string? conn = _config.GetConnectionString("DefaultConnection");
            string email = req.Email.Trim();

            // Check if OTP was verified
            using (SqlConnection con = new SqlConnection(conn))
            {
                const string q = "SELECT IsVerifiedOTPftp FROM Users WHERE Email=@Email";
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    var encIsVerified = cmd.ExecuteScalar() as string;
                    if (string.IsNullOrEmpty(encIsVerified))
                        return BadRequest(new { message = "OTP not verified" });

                    string decVerified = EncryptionHelper.Decrypt(encIsVerified);
                    if (decVerified != "1")
                        return BadRequest(new { message = "OTP not verified" });
                }
            }

            // Hash new password
            var hasher = new PasswordHasher<string>();
            string hashedPassword = hasher.HashPassword(PwdSalt, req.Password);

            // Update password and reset OTP fields
            using (SqlConnection con = new SqlConnection(conn))
            {
                const string upd = @"
                    UPDATE Users
                    SET Password=@Pass,
                        OTPftp=NULL,
                        OTPftpExpiry=NULL,
                        IsVerifiedOTPftp=@IsVerified
                    WHERE Email=@Email";
                using (SqlCommand cmd = new SqlCommand(upd, con))
                {
                    cmd.Parameters.AddWithValue("@Pass", hashedPassword);
                    cmd.Parameters.AddWithValue("@IsVerified", EncryptionHelper.Encrypt("0"));
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Password reset successful" });
        }
    }
}
