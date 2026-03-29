namespace RegisterApi.Models
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;  // Email jo password reset ke liye use hoga
    }
}
