using QuantAssembly.Common.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Markdig;

namespace QuantAssembly.Analyst.Service
{
    public class EmailService
    {
        private readonly string smtpServer;
        private readonly int smtpPort;
        private readonly string sourceEmail;
        private readonly string appPassword;
        private ILogger logger;
        public EmailService(string smtpServer, int smtpPort, string sourceEmail, string appPassword, ILogger logger)
        {
            this.smtpServer = smtpServer;
            this.smtpPort = smtpPort;
            this.sourceEmail = sourceEmail;
            this.appPassword = appPassword;
            this.logger = logger;
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail)) throw new ArgumentException("Recipient email is required.");
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject cannot be empty.");
            if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.");

            // Create MIME email
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("QuantAssembly.Analyst", this.sourceEmail));
            email.To.Add(new MailboxAddress(recipientEmail, recipientEmail));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"<html><body>{body}</body></html>",
                TextBody = body // Fallback for non-HTML email clients
            };

            email.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            try
            {
                await smtpClient.ConnectAsync(this.smtpServer, this.smtpPort, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(this.sourceEmail, this.appPassword);
                await smtpClient.SendAsync(email);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to send email: {ex.Message}");
            }
            finally
            {
                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}