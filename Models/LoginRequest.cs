namespace RegisterApi.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;    // null-safe
        public string Password { get; set; } = string.Empty; // null-safe
    }
}
