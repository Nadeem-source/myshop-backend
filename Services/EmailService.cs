using SendGrid;
using SendGrid.Helpers.Mail;

namespace RegisterApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("SendGrid API Key missing in configuration");

            var client = new SendGridClient(apiKey);

            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"];

            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);

            var response = await client.SendEmailAsync(msg);

            // 🔥 Console Logs (Debug में दिखेंगे)
            Console.WriteLine("====================================");
            Console.WriteLine("SendGrid Response Status: " + response.StatusCode);

            var body = await response.Body.ReadAsStringAsync();
            Console.WriteLine("SendGrid Response Body: " + body);
            Console.WriteLine("====================================");

            // ❌ If SendGrid returned an error (non-2xx)
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("SendGrid Error: " + body);
            }
        }
    }
}
