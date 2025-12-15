using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Biblio_Web.Services
{
    // Development-only email sender that logs messages to the configured logger and to a file under AppData
    // Does not send real emails. Safe for local testing when RequireConfirmedAccount is enabled.
    public class DevEmailSender : IEmailSender
    {
        private readonly ILogger<DevEmailSender> _logger;
        private readonly string _logPath;

        public DevEmailSender(ILogger<DevEmailSender> logger)
        {
            _logger = logger;
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Biblio_Web");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                _logPath = Path.Combine(folder, "dev_emails.log");
            }
            catch
            {
                _logPath = Path.GetTempFileName();
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var text = $"---\nTo: {email}\nSubject: {subject}\nAt: {DateTime.UtcNow:o}\nMessage:\n{htmlMessage}\n---\n";
            try
            {
                _logger.LogInformation("[DevEmailSender] To={Email} Subject={Subject}", email, subject);
                await File.AppendAllTextAsync(_logPath, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed writing dev email to file");
            }
        }
    }
}
