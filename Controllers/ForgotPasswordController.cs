using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using RegisterApi.Services;
using RegisterApi.Helpers;
using System.Security.Cryptography;
using RegisterApi.Models;

namespace RegisterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private const string FtpOtpSalt = "ftp-otp-salt";

        public ForgotPasswordController(IConfiguration config, EmailService emailService)
        {
            _config = config;
            _emailService = emailService;
        }

        // ================= SEND OTP =================
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] ForgotPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "Email required" });

            string? conn = _config.GetConnectionString("DefaultConnection");
            string email = req.Email.Trim();

            using SqlConnection con = new SqlConnection(conn);
            using SqlCommand check = new SqlCommand("sp_ForgotPassword_CheckUser", con);
            check.CommandType = System.Data.CommandType.StoredProcedure;
            check.Parameters.AddWithValue("@Email", email);
            con.Open();
            int exists = (int)check.ExecuteScalar();
            if (exists == 0)
                return NotFound(new { message = "Email not registered" });

            string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var hasher = new PasswordHasher<string>();
            string hashedOtp = hasher.HashPassword(FtpOtpSalt, otp);

            string encExpiry = EncryptionHelper.Encrypt(DateTime.UtcNow.AddMinutes(5).Ticks.ToString());
            string encVerified = EncryptionHelper.Encrypt("0");

            using SqlCommand save = new SqlCommand("sp_ForgotPassword_SaveOtp", con);
            save.CommandType = System.Data.CommandType.StoredProcedure;
            save.Parameters.AddWithValue("@Email", email);
            save.Parameters.AddWithValue("@Otp", hashedOtp);
            save.Parameters.AddWithValue("@Expiry", encExpiry);
            save.Parameters.AddWithValue("@IsVerified", encVerified);
            save.ExecuteNonQuery();

            await _emailService.SendEmailAsync(email, "Reset OTP", $"Your OTP is {otp}");
            return Ok(new { message = "OTP sent" });
        }

        // ================= VERIFY OTP =================
        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            string? conn = _config.GetConnectionString("DefaultConnection");
            using SqlConnection con = new SqlConnection(conn);
            using SqlCommand get = new SqlCommand("sp_ForgotPassword_GetOtpData", con);
            get.CommandType = System.Data.CommandType.StoredProcedure;
            get.Parameters.AddWithValue("@Email", req.Email);
            con.Open();
            var r = get.ExecuteReader();
            if (!r.Read()) return BadRequest();

            string hashedOtp = r["OTPftp"] as String ?? "";
            string encExpiry = r["OTPftpExpiry"] as String ?? "";
            r.Close();

            var hasher = new PasswordHasher<string>();
            if (hasher.VerifyHashedPassword(
                FtpOtpSalt,
                hashedOtp ?? "" ,
                req.OTPftp ?? ""
                )
                == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Invalid OTP" });

            long ticks = long.Parse(EncryptionHelper.Decrypt(encExpiry));
            if (DateTime.UtcNow > new DateTime(ticks))
                return BadRequest(new { message = "OTP expired" });

            using SqlCommand upd = new SqlCommand("sp_ForgotPassword_MarkVerified", con);
            upd.CommandType = System.Data.CommandType.StoredProcedure;
            upd.Parameters.AddWithValue("@Email", req.Email);
            upd.Parameters.AddWithValue("@IsVerified", EncryptionHelper.Encrypt("1"));
            upd.ExecuteNonQuery();

            return Ok(new { message = "OTP verified" });
        }

        // ================= RESET PASSWORD =================
        [HttpPost("reset")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var hasher = new PasswordHasher<string>();
            string hashedPwd = hasher.HashPassword("pwd", req.Password);

            string? conn = _config.GetConnectionString("DefaultConnection");
            using SqlConnection con = new SqlConnection(conn);
            using SqlCommand cmd = new SqlCommand("sp_ForgotPassword_ResetPassword", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Email", req.Email);
            cmd.Parameters.AddWithValue("@Password", hashedPwd);
            cmd.Parameters.AddWithValue("@IsVerified", EncryptionHelper.Encrypt("0"));
            con.Open();
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Password reset successful" });
        }
    }
}