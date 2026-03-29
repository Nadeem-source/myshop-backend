namespace RegisterApi.Models
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;    // null-safe
        public string Password { get; set; } = string.Empty; // null-safe
        public string Role { get; set; } = string.Empty; // null safe
    }
}
