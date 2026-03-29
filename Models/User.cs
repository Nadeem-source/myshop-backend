namespace RegisterApi.Models
{
    public class User
    {
        public string Email { get; set; } = string.Empty;        // plain email
        public string Password { get; set; } = string.Empty;     // hashed password

        public string? OTP { get; set; }                         // hashed registration OTP
        public long? OTPExpiry { get; set; }                     // OTP expiry ticks
        public string? IsVerified { get; set; }                 // "0" or "1" for registration verification

        public string? OTPftp { get; set; }                      // hashed forgot-password OTP
        public long? OTPftpExpiry { get; set; }                  // forgot-password OTP expiry ticks
        public string? IsVerifiedOTPftp { get; set; }            // "0" or "1" for forgot-password OTP verification

        public string Role { get; set; } = "User";               // User | Admin
                                                   
    }
}
