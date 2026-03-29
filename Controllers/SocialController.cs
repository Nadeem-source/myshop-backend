using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RegisterApi.Helpers;

namespace RegisterApi.Controllers
{
    [Route("api/social")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        private readonly IConfiguration _config;

        public SocialController(IConfiguration config)
        {
            _config = config;
        }

        // ================= GOOGLE SIGNUP =================
        [HttpPost("google-signup")]
        public async Task<IActionResult> GoogleSignup([FromBody] GoogleAuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.IdToken))
                return BadRequest(new { message = "Token missing" });

            GoogleJsonWebSignature.Payload payload =
                await GoogleJsonWebSignature.ValidateAsync(req.IdToken);

            string? email = payload.Email?.Trim();

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Invalid Google token" });

            string? conn = _config.GetConnectionString("DefaultConnection");

            // 🔹 CHECK USER EXISTS
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_CheckUserExists", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);

                con.Open();
                int exists = (int)cmd.ExecuteScalar();

                if (exists > 0)
                    return BadRequest(new { message = "Email already registered." });
            }

            // 🔹 INSERT GOOGLE USER
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_InsertUser", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", "GOOGLE_AUTH");
                cmd.Parameters.AddWithValue("@IsVerified", "1");
                cmd.Parameters.AddWithValue("@Role", req.Role?? "User"); // 🔥 FIX

                con.Open();
                cmd.ExecuteNonQuery();
                // 🔥 STEP 1: Declare userId
                int userId = 0;

                // 🔥 STEP 2: Fetch userId from Users table
                using (SqlConnection con3 = new SqlConnection(conn))
                using (SqlCommand cmd3 = new SqlCommand(
                    "SELECT UserId FROM Users WHERE Email=@Email", con3))
                {
                    cmd3.Parameters.AddWithValue("@Email", email);

                    con3.Open();
                    userId = (int)cmd3.ExecuteScalar();
                }
                using (SqlConnection con2 = new SqlConnection(conn))
                using (SqlCommand cmd2 = new SqlCommand(
                    @"INSERT INTO Admins (Name, Email, UpiId, UserId)
      VALUES (@Name, @Email, @UpiId, @UserId)", con2))
                {
                    cmd2.Parameters.AddWithValue("@Name", email);
                    cmd2.Parameters.AddWithValue("@Email", email);
                    cmd2.Parameters.AddWithValue("@UpiId", "");
                    cmd2.Parameters.AddWithValue("@UserId", userId);

                    con2.Open();
                    cmd2.ExecuteNonQuery();

                }
            }

            return Ok(new { message = "Google Signup Successful!" });
        }

        // ================= GOOGLE LOGIN =================
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.IdToken))
                return BadRequest(new { message = "Token missing" });

            GoogleJsonWebSignature.Payload payload =
                await GoogleJsonWebSignature.ValidateAsync(req.IdToken);

            string? email = payload.Email?.Trim();

            if (string.IsNullOrEmpty(email))
                return BadRequest(new { message = "Invalid Google token" });

            string? conn = _config.GetConnectionString("DefaultConnection");

            int exists = 0;
            string roleDb = "User"; // ✅ DEFAULT SAFE ROLE

            // 🔹 Check user exists
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_CheckUserExists", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);

                con.Open();
                exists = (int)cmd.ExecuteScalar();
            }

            if (exists == 0)
                return BadRequest(new { message = "User not found. Please Signup using Google." });

            // 🔹 FETCH ROLE (✅ ADDED)
            int sellerId = 0; // 🔥 ADD

            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT UserId, Role FROM Users WHERE Email = @Email", con))
            {
                cmd.Parameters.AddWithValue("@Email", email);

                con.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        roleDb = reader["Role"].ToString()!;
                        sellerId = Convert.ToInt32(reader["UserId"]); // 🔥 IMPORTANT
                    }
                }
            }

            // ✅ RETURN ROLE TO FRONTEND
            return Ok(new
            {
                message = "Google Login Successful!",
                email = email,
                role = roleDb,
                sellerId=sellerId
            });
        }
    }

    // ================= REQUEST MODEL =================
    public class GoogleAuthRequest
    {
        public string? IdToken { get; set; }
        public string? Role {get; set; }
    }
}
