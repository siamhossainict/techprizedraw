using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TechPrizeDraw.Data;
using TechPrizeDraw.Models.Entities;
using TechPrizeDraw.Models.Enums;
using TechPrizeDraw.Services.Interfaces;

namespace TechPrizeDraw.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> SendOrderAcknowledgmentAsync(Order order)
    {
        if (order.Customer == null)
        {
            await _context.Entry(order).Reference(o => o.Customer).LoadAsync();
        }

        var customer = order.Customer;
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
        {
            _logger.LogWarning("Cannot send acknowledgment: customer email missing for Order #{OrderNumber}", order.OrderNumber);
            return false;
        }

        string subject = $"Payment Information Received — Order #{order.OrderNumber}";
        string body = $@"
            <div style='font-family: Arial, sans-serif; background-color: #070d1e; color: #ffffff; padding: 30px; border-radius: 8px; max-width: 600px; margin: 0 auto;'>
                <div style='border-bottom: 2px solid #e5a93c; padding-bottom: 15px; margin-bottom: 20px;'>
                    <h2 style='color: #e5a93c; margin: 0;'>TECH Prize Draw</h2>
                    <p style='color: #94a3b8; margin: 5px 0 0 0;'>Payment Submission Acknowledgment</p>
                </div>
                <p>Hello <strong>{WebUtility.HtmlEncode(customer.FullName)}</strong>,</p>
                <p>We have successfully received your payment submission for the TECH Prize Draw.</p>
                <div style='background-color: #0c1631; border: 1px solid #1e3166; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p style='margin: 5px 0;'><strong>Order Number:</strong> <span style='color: #38bdf8;'>{order.OrderNumber}</span></p>
                    <p style='margin: 5px 0;'><strong>Amount:</strong> ৳{order.Amount:N0} BDT</p>
                    <p style='margin: 5px 0;'><strong>Transaction ID:</strong> {WebUtility.HtmlEncode(order.TransactionId)}</p>
                    <p style='margin: 5px 0;'><strong>Status:</strong> <span style='color: #fbbf24; font-weight: bold;'>PENDING VERIFICATION</span></p>
                </div>
                <div style='background-color: #1e293b; border-left: 4px solid #38bdf8; padding: 12px; margin: 20px 0;'>
                    <p style='margin: 0; color: #cbd5e1; font-size: 14px;'>
                        <strong>Important:</strong> Your payment is currently under manual verification by our operations team. 
                        <strong>Your official ticket will be issued and emailed to you only after administrator approval.</strong>
                    </p>
                </div>
                <p style='color: #94a3b8; font-size: 12px; margin-top: 30px; border-top: 1px solid #1e3166; padding-top: 15px;'>
                    If you did not initiate this order, please contact support at techprizedraw@inbox.eu immediately.
                </p>
            </div>";

        return await SendAndLogEmailAsync(customer.Email, subject, body, "OrderAcknowledgment", order.Id, null);
    }

    public async Task<bool> SendApprovedTicketEmailAsync(Ticket ticket, Order order, Customer customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email))
        {
            _logger.LogWarning("Cannot send ticket email: customer email missing for Ticket #{TicketNumber}", ticket.TicketNumber);
            return false;
        }

        var campaign = await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == ticket.CampaignId);
        string drawDateFormatted = campaign?.DrawDate.ToString("MMMM dd, yyyy") ?? "To Be Announced";
        string verifyUrl = ticket.VerificationUrl ?? $"/ticket/verify/{ticket.TicketNumber}";

        string subject = $"Your TECH Prize Draw Ticket — {ticket.TicketNumber}";
        string body = $@"
            <div style='font-family: Arial, sans-serif; background-color: #070d1e; color: #ffffff; padding: 30px; border-radius: 8px; max-width: 650px; margin: 0 auto;'>
                <div style='border-bottom: 2px solid #e5a93c; padding-bottom: 15px; margin-bottom: 20px; text-align: center;'>
                    <span style='background: #e5a93c; color: #070d1e; font-size: 11px; font-weight: bold; padding: 3px 8px; border-radius: 4px; text-transform: uppercase;'>Official Entry Voucher</span>
                    <h1 style='color: #ffffff; margin: 10px 0 5px 0; font-size: 26px; letter-spacing: 1px;'>TECH PRIZE DRAW</h1>
                    <p style='color: #94a3b8; margin: 0; font-size: 14px;'>Bangladesh Premium Technology Sweepstakes</p>
                </div>

                <p>Congratulations <strong>{WebUtility.HtmlEncode(customer.FullName)}</strong>!</p>
                <p>Your payment of <strong>৳{order.Amount:N0} BDT</strong> has been <strong>VERIFIED AND APPROVED</strong> by our administrator. Your official draw entry has been finalized.</p>

                <!-- Digital Ticket Card -->
                <div style='background: linear-gradient(135deg, #0c1631 0%, #162447 100%); border: 2px solid #e5a93c; border-radius: 12px; padding: 25px; margin: 25px 0; box-shadow: 0 8px 24px rgba(0,0,0,0.5);'>
                    <div style='display: flex; justify-content: space-between; border-bottom: 1px dashed #334155; padding-bottom: 15px; margin-bottom: 15px;'>
                        <div>
                            <span style='color: #94a3b8; font-size: 11px; text-transform: uppercase;'>Official Ticket Number</span>
                            <div style='color: #e5a93c; font-size: 30px; font-weight: bold; letter-spacing: 2px;'>{ticket.TicketNumber}</div>
                        </div>
                        <div style='text-align: right;'>
                            <span style='background-color: #10b981; color: #ffffff; font-size: 12px; font-weight: bold; padding: 4px 10px; border-radius: 20px;'>STATUS: VALID</span>
                        </div>
                    </div>
                    <div style='margin-bottom: 15px;'>
                        <p style='margin: 4px 0; font-size: 14px;'><strong style='color: #94a3b8;'>Participant:</strong> {WebUtility.HtmlEncode(customer.FullName)}</p>
                        <p style='margin: 4px 0; font-size: 14px;'><strong style='color: #94a3b8;'>Mobile:</strong> {customer.MobileNumber}</p>
                        <p style='margin: 4px 0; font-size: 14px;'><strong style='color: #94a3b8;'>Order Reference:</strong> {order.OrderNumber}</p>
                        <p style='margin: 4px 0; font-size: 14px;'><strong style='color: #94a3b8;'>Scheduled Draw Date:</strong> <span style='color: #38bdf8;'>{drawDateFormatted}</span></p>
                    </div>
                    <div style='border-top: 1px dashed #334155; padding-top: 15px; text-align: center;'>
                        <p style='color: #e2e8f0; font-size: 13px; margin: 0 0 10px 0;'><strong>Featured Prizes:</strong> 1st: Laptop | 2nd: Audio Interface | 3rd: Acoustic Guitar</p>
                        <a href='{verifyUrl}' style='display: inline-block; background-color: #e5a93c; color: #070d1e; font-weight: bold; text-decoration: none; padding: 10px 24px; border-radius: 6px; font-size: 14px;'>Verify Ticket Online</a>
                    </div>
                </div>

                <p style='font-size: 13px; color: #94a3b8;'>
                    Keep this email safe. Winning tickets are selected via our cryptographically audited random draw. Draw results will be publicly announced on the official winners portal.
                </p>
                <p style='color: #64748b; font-size: 11px; margin-top: 25px; border-top: 1px solid #1e3166; padding-top: 15px;'>
                    TECH Prize Draw Support: techprizedraw@inbox.eu | Dhaka, Bangladesh
                </p>
            </div>";

        return await SendAndLogEmailAsync(customer.Email, subject, body, "ApprovedTicketDelivery", order.Id, ticket.Id);
    }

    public async Task<bool> SendRejectionNoticeAsync(Order order, string reason)
    {
        if (order.Customer == null)
        {
            await _context.Entry(order).Reference(o => o.Customer).LoadAsync();
        }

        var customer = order.Customer;
        if (customer == null || string.IsNullOrWhiteSpace(customer.Email)) return false;

        string subject = $"Payment Verification Update — Order #{order.OrderNumber}";
        string body = $@"
            <div style='font-family: Arial, sans-serif; background-color: #070d1e; color: #ffffff; padding: 30px; border-radius: 8px; max-width: 600px; margin: 0 auto;'>
                <div style='border-bottom: 2px solid #ef4444; padding-bottom: 15px; margin-bottom: 20px;'>
                    <h2 style='color: #ef4444; margin: 0;'>TECH Prize Draw</h2>
                    <p style='color: #94a3b8; margin: 5px 0 0 0;'>Payment Verification Notice</p>
                </div>
                <p>Hello <strong>{WebUtility.HtmlEncode(customer.FullName)}</strong>,</p>
                <p>We are writing to inform you that your submitted transaction for Order <strong>#{order.OrderNumber}</strong> could not be verified.</p>
                <div style='background-color: #1f121d; border: 1px solid #7f1d1d; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <p style='margin: 0; color: #fca5a5;'><strong>Verification Outcome:</strong> {WebUtility.HtmlEncode(reason)}</p>
                </div>
                <p style='font-size: 14px; color: #cbd5e1;'>
                    Because payment could not be confirmed, no valid ticket has been issued and reserved inventory has been returned to the pool.
                </p>
                <p style='font-size: 14px; color: #cbd5e1;'>
                    If you believe this is an error or already completed the payment, please contact our support desk with your payment screenshot and MFS statement at <strong>techprizedraw@inbox.eu</strong>.
                </p>
            </div>";

        return await SendAndLogEmailAsync(customer.Email, subject, body, "PaymentRejectionNotice", order.Id, null);
    }

    public async Task<bool> ResendTicketEmailAsync(int ticketId, string adminUsername)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Order)
            .Include(t => t.Customer)
            .Include(t => t.Campaign)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null || ticket.Customer == null || ticket.Order == null)
        {
            _logger.LogWarning("ResendTicketEmailAsync: Ticket #{TicketId} or related entities not found.", ticketId);
            return false;
        }

        _logger.LogInformation("Admin {AdminUser} requested resending ticket email for {TicketNumber}", adminUsername, ticket.TicketNumber);
        return await SendApprovedTicketEmailAsync(ticket, ticket.Order, ticket.Customer);
    }

    private async Task<bool> SendAndLogEmailAsync(string recipient, string subject, string body, string templateName, int? orderId, int? ticketId)
    {
        bool isEmailEnabled = _configuration.GetValue<bool>("Email:Enabled", false);
        string host = _configuration.GetValue<string>("Email:Host") ?? string.Empty;
        int port = _configuration.GetValue<int>("Email:Port", 587);
        string username = _configuration.GetValue<string>("Email:Username") ?? string.Empty;
        string password = _configuration.GetValue<string>("Email:Password") ?? string.Empty;
        string fromEmail = _configuration.GetValue<string>("Email:FromEmail") ?? "techprizedraw@inbox.eu";
        string fromName = _configuration.GetValue<string>("Email:FromName") ?? "TECH Prize Draw";

        var emailLog = new EmailLog
        {
            RecipientEmail = recipient,
            Subject = subject,
            TemplateName = templateName,
            OrderId = orderId,
            TicketId = ticketId,
            CreatedAt = DateTime.UtcNow
        };

        if (!isEmailEnabled || string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP not enabled or host not configured. Simulating delivery to {Recipient} (Subject: {Subject})", recipient, subject);
            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;
            emailLog.ErrorMessage = "Simulated delivery (SMTP disabled by config).";

            await _context.EmailLogs.AddAsync(emailLog);
            await _context.SaveChangesAsync();
            return true;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(username, password),
                Timeout = 15000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(recipient));

            await client.SendMailAsync(message);

            emailLog.Status = EmailStatus.Sent;
            emailLog.SentAt = DateTime.UtcNow;
            await _context.EmailLogs.AddAsync(emailLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully sent email [{Template}] to {Recipient}", templateName, recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch email [{Template}] to {Recipient}", templateName, recipient);

            emailLog.Status = EmailStatus.Failed;
            emailLog.ErrorMessage = ex.Message;
            await _context.EmailLogs.AddAsync(emailLog);
            await _context.SaveChangesAsync();

            return false;
        }
    }
}
