namespace RegisterApi.Models
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;    // null-safe
        public string Password { get; set; } = string.Empty; // null-safe
    }
}
