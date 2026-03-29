namespace RegisterApi.Models
{
    public class VerifyOtpRequest
    {
        public string Email { get; set; } = string.Empty;  // plain email

        // Registration OTP / Forgot-password OTP
        public string? OTP { get; set; }     // registration OTP
        public string? OTPftp { get; set; }  // forgot-password OTP

        // Determine which OTP is being verified
        //public bool IsForForgotPassword => !string.IsNullOrEmpty(OTPftp);
    }
}
