using SendGrid;
using SendGrid.Helpers.Mail;

namespace WorkBridge.Services
{
    // This service handles ALL emails sent by WorkBridge.
    // We inject it into controllers using dependency injection.
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── CORE SEND METHOD ──────────────────────────────────────────────
        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"];
            var fromName = _config["SendGrid:FromName"] ?? "WorkBridge";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(toEmail, toName);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, body);
            }
            else
            {
                _logger.LogInformation("Email sent to {Email} - Subject: {Subject}", toEmail, subject);
            }
        }

        // ── EMAIL TEMPLATES ───────────────────────────────────────────────

        // 1. Sent to ADMIN when a user submits a new request
        public async Task SendNewRequestNotificationAsync(string adminEmail, string userName, string requestTitle, string category)
        {
            var subject = $"New Request: {requestTitle}";
            var html = EmailTemplate("New Service Request Received", $@"
                <p>Hi Admin,</p>
                <p>A new service request has been submitted and is waiting for your review.</p>
                {InfoBox(new[] {
                    ("Client", userName),
                    ("Title", requestTitle),
                    ("Category", category)
                })}
                <p>Please log in to your admin dashboard to review and accept or decline this request.</p>
                {Button("View Dashboard", "http://localhost:5080")}
            ");
            await SendEmailAsync(adminEmail, "Admin", subject, html);
        }

        // 2. Sent to USER when admin accepts their request
        public async Task SendRequestAcceptedAsync(string userEmail, string userName, string requestTitle, decimal price)
        {
            var subject = $"Your request has been accepted — {requestTitle}";
            var html = EmailTemplate("Great news! Your request was accepted 🎉", $@"
                <p>Hi {userName},</p>
                <p>Your service request has been reviewed and <strong>accepted</strong>. Work will begin shortly!</p>
                {InfoBox(new[] {
                    ("Request", requestTitle),
                    ("Price", $"${price}"),
                    ("Status", "Accepted — In Progress")
                })}
                <p>You'll receive another email when your work is completed and ready for download.</p>
                {Button("View My Requests", "http://localhost:5080")}
            ");
            await SendEmailAsync(userEmail, userName, subject, html);
        }

        // 3. Sent to USER when admin declines their request
        public async Task SendRequestDeclinedAsync(string userEmail, string userName, string requestTitle, string reason)
        {
            var subject = $"Update on your request — {requestTitle}";
            var html = EmailTemplate("Update on your request", $@"
                <p>Hi {userName},</p>
                <p>Unfortunately, we are unable to take on your request at this time.</p>
                {InfoBox(new[] {
                    ("Request", requestTitle),
                    ("Status", "Declined"),
                    ("Reason", reason ?? "No reason provided")
                })}
                <p>You're welcome to submit a new request at any time. We apologize for any inconvenience.</p>
                {Button("Submit New Request", "http://localhost:5080")}
            ");
            await SendEmailAsync(userEmail, userName, subject, html);
        }

        // 4. Sent to USER when admin marks work as completed
        public async Task SendRequestCompletedAsync(string userEmail, string userName, string requestTitle, decimal price)
        {
            var subject = $"Your work is ready! — {requestTitle}";
            var html = EmailTemplate("Your completed work is ready for download! ✅", $@"
                <p>Hi {userName},</p>
                <p>Great news! Your request has been completed and your files are ready.</p>
                {InfoBox(new[] {
                    ("Request", requestTitle),
                    ("Amount Due", $"${price}"),
                    ("Status", "Completed — Payment Required")
                })}
                <p>Please log in to your dashboard, confirm payment of <strong>${price}</strong>, and your files will be unlocked for download immediately.</p>
                {Button("Pay & Download Now", "http://localhost:5080")}
            ");
            await SendEmailAsync(userEmail, userName, subject, html);
        }

        // 5. Sent to USER when payment is confirmed
        public async Task SendPaymentConfirmedAsync(string userEmail, string userName, string requestTitle, decimal amount)
        {
            var subject = $"Payment confirmed — Download your files now";
            var html = EmailTemplate("Payment confirmed — your files are unlocked! 🔓", $@"
                <p>Hi {userName},</p>
                <p>Your payment has been confirmed! Your completed files are now available for download.</p>
                {InfoBox(new[] {
                    ("Request", requestTitle),
                    ("Amount Paid", $"${amount}"),
                    ("Status", "Delivered")
                })}
                <p>Log in to your dashboard and click the Download button to get your files.</p>
                {Button("Download My Files", "http://localhost:5080")}
                <p style='color:#888;font-size:13px;margin-top:24px'>Thank you for using WorkBridge!</p>
            ");
            await SendEmailAsync(userEmail, userName, subject, html);
        }


        // ── HTML TEMPLATE HELPERS ─────────────────────────────────────────

        // Wraps content in a clean, professional email layout
        private static string EmailTemplate(string heading, string content) => $@"
        <!DOCTYPE html>
        <html>
        <head><meta charset='utf-8'></head>
        <body style='margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif'>
          <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:30px 0'>
            <tr><td align='center'>
              <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08)'>
                <!-- Header -->
                <tr>
                  <td style='background:#1A3A6B;padding:28px 40px;text-align:center'>
                    <h1 style='color:#ffffff;margin:0;font-size:24px;letter-spacing:-0.5px'>WorkBridge</h1>
                    <p style='color:#93C5FD;margin:6px 0 0;font-size:13px'>Assignment & Web Development Services</p>
                  </td>
                </tr>
                <!-- Body -->
                <tr>
                  <td style='padding:36px 40px'>
                    <h2 style='color:#1a1a18;margin:0 0 20px;font-size:20px'>{heading}</h2>
                    {content}
                  </td>
                </tr>
                <!-- Footer -->
                <tr>
                  <td style='background:#f8f8f8;padding:20px 40px;text-align:center;border-top:1px solid #eee'>
                    <p style='color:#999;font-size:12px;margin:0'>© 2026 WorkBridge · All rights reserved</p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>";

        // Creates a styled info box with key-value pairs
        private static string InfoBox((string Key, string Value)[] items)
        {
            var rows = string.Join("", items.Select(i => $@"
                <tr>
                  <td style='padding:8px 12px;font-weight:600;color:#555;font-size:14px;width:130px'>{i.Key}</td>
                  <td style='padding:8px 12px;color:#1a1a18;font-size:14px'>{i.Value}</td>
                </tr>"));
            return $@"
                <table style='background:#f0f4ff;border-radius:8px;width:100%;margin:20px 0;border-collapse:collapse'>
                  {rows}
                </table>";
        }

        // Creates a styled call-to-action button
        private static string Button(string text, string url) => $@"
            <div style='text-align:center;margin:28px 0'>
              <a href='{url}' style='background:#1A3A6B;color:#ffffff;padding:13px 32px;border-radius:8px;text-decoration:none;font-size:15px;font-weight:600;display:inline-block'>{text}</a>
            </div>";
    }
}