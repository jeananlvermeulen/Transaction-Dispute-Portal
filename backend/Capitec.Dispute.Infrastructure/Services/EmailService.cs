using Capitec.Dispute.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Capitec.Dispute.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendDisputeConfirmationAsync(string toEmail, string firstName, string incidentReference, string reason, string summary)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping dispute confirmation email to {Email}", toEmail);
            return;
        }

        var subject = $"Dispute Submitted – Reference {incidentReference}";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Dispute Confirmation</h2>
                    <p>Dear {firstName},</p>
                    <p>Your dispute has been successfully submitted. Here are the details:</p>
                    <table style=""width: 100%; border-collapse: collapse; margin: 16px 0;"">
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold; width: 40%;"">Reference Number</td>
                            <td style=""padding: 8px;"">{incidentReference}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">Reason</td>
                            <td style=""padding: 8px;"">{reason}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">Summary</td>
                            <td style=""padding: 8px;"">{summary}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">Status</td>
                            <td style=""padding: 8px;"">Submitted</td>
                        </tr>
                    </table>
                    <p>Our team will review your dispute and get back to you as soon as possible. You can track the progress of your dispute by logging into the Capitec Dispute Portal.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Dispute confirmation email sent to {Email} for reference {Reference}", toEmail, incidentReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send dispute confirmation email to {Email}", toEmail);
        }
    }

    public async Task SendStatusUpdateAsync(string toEmail, string firstName, string incidentReference, string oldStatus, string newStatus, string? notes, bool bookCall = false)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping status update email to {Email}", toEmail);
            return;
        }

        var statusColors = new Dictionary<string, string>
        {
            { "Pending", "#f59e0b" },
            { "UnderReview", "#3b82f6" },
            { "Resolved", "#22c55e" },
            { "Rejected", "#ef4444" },
            { "Submitted", "#6b7280" }
        };
        var statusLabels = new Dictionary<string, string>
        {
            { "Pending", "Pending" },
            { "UnderReview", "Under Review" },
            { "Resolved", "Resolved" },
            { "Rejected", "Rejected" },
            { "Submitted", "Submitted" }
        };

        var newStatusColor = statusColors.GetValueOrDefault(newStatus, "#6b7280");
        var newStatusLabel = statusLabels.GetValueOrDefault(newStatus, newStatus);
        var notesRow = string.IsNullOrEmpty(notes) ? "" : $@"
            <tr>
                <td style=""padding: 8px; background: #f5f5f5; font-weight: bold; width: 40%;"">Notes</td>
                <td style=""padding: 8px;"">{notes}</td>
            </tr>";

        var callNotice = bookCall ? @"
            <div style=""margin: 16px 0; padding: 12px 16px; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 4px;"">
                <p style=""margin: 0; color: #1d4ed8; font-weight: bold;"">📞 A Capitec employee will call you within the next 15 minutes.</p>
                <p style=""margin: 4px 0 0; color: #3b82f6; font-size: 13px;"">Please ensure your phone is available. If you miss the call, our team will try again shortly.</p>
            </div>" : "";

        var subject = $"Dispute Status Update – Reference {incidentReference}";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Dispute Status Update</h2>
                    <p>Dear {firstName},</p>
                    <p>The status of your dispute has been updated. Here are the details:</p>
                    <table style=""width: 100%; border-collapse: collapse; margin: 16px 0;"">
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold; width: 40%;"">Reference Number</td>
                            <td style=""padding: 8px;"">{incidentReference}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">New Status</td>
                            <td style=""padding: 8px;"">
                                <span style=""background: {newStatusColor}20; color: {newStatusColor}; padding: 4px 10px; border-radius: 12px; font-weight: bold;"">{newStatusLabel}</span>
                            </td>
                        </tr>
                        {notesRow}
                    </table>
                    {callNotice}
                    <p>You can log into the Capitec Dispute Portal to view the full details and history of your dispute.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Status update email sent to {Email} for reference {Reference} — new status: {Status}", toEmail, incidentReference, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status update email to {Email}", toEmail);
        }
    }

    public async Task SendDisputeCancellationAsync(string toEmail, string firstName, string incidentReference, string cancellationReason)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping dispute cancellation email to {Email}", toEmail);
            return;
        }

        var subject = $"Dispute Cancelled – Reference {incidentReference}";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Dispute Cancelled</h2>
                    <p>Dear {firstName},</p>
                    <p>Your dispute has been successfully cancelled. Here are the details:</p>
                    <table style=""width: 100%; border-collapse: collapse; margin: 16px 0;"">
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold; width: 40%;"">Reference Number</td>
                            <td style=""padding: 8px;"">{incidentReference}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">Status</td>
                            <td style=""padding: 8px;"">
                                <span style=""background: #f3f4f620; color: #6b7280; padding: 4px 10px; border-radius: 12px; font-weight: bold;"">Cancelled</span>
                            </td>
                        </tr>
                        <tr>
                            <td style=""padding: 8px; background: #f5f5f5; font-weight: bold;"">Reason for Cancellation</td>
                            <td style=""padding: 8px;"">{cancellationReason}</td>
                        </tr>
                    </table>
                    <p>If you cancelled this dispute by mistake or have further concerns, please log into the Capitec Dispute Portal and submit a new dispute.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Dispute cancellation email sent to {Email} for reference {Reference}", toEmail, incidentReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send dispute cancellation email to {Email}", toEmail);
        }
    }

    public async Task SendPasswordChangeCodeAsync(string toEmail, string firstName, string code)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping password change code email to {Email}", toEmail);
            return;
        }

        var subject = "Your Password Change Verification Code";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Password Change Request</h2>
                    <p>Dear {firstName},</p>
                    <p>We received a request to change your password. Please use the verification code below to confirm this change:</p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <div style=""display: inline-block; background: #eff6ff; border: 2px solid #3b82f6; border-radius: 12px; padding: 16px 40px;"">
                            <p style=""margin: 0; font-size: 36px; font-weight: bold; letter-spacing: 12px; color: #1d4ed8;"">{code}</p>
                        </div>
                    </div>
                    <p>This code is valid for <strong>10 minutes</strong>. If you did not request a password change, please ignore this email — your password will remain unchanged.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Password change code sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password change code email to {Email}", toEmail);
        }
    }

    public async Task SendProfileChangeCodeAsync(string toEmail, string firstName, string code)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping profile change code email to {Email}", toEmail);
            return;
        }

        var subject = "Your Profile Update Verification Code";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Profile Update Request</h2>
                    <p>Dear {firstName},</p>
                    <p>We received a request to update your profile details. Please use the verification code below to confirm this change:</p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <div style=""display: inline-block; background: #eff6ff; border: 2px solid #3b82f6; border-radius: 12px; padding: 16px 40px;"">
                            <p style=""margin: 0; font-size: 36px; font-weight: bold; letter-spacing: 12px; color: #1d4ed8;"">{code}</p>
                        </div>
                    </div>
                    <p>This code is valid for <strong>10 minutes</strong>. If you did not request a profile update, please ignore this email — your details will remain unchanged.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Profile change code sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send profile change code email to {Email}", toEmail);
        }
    }

    public async Task SendRegistrationVerificationCodeAsync(string toEmail, string firstName, string code)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping registration verification email to {Email}", toEmail);
            return;
        }

        var subject = "Your Registration Verification Code";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Verify Your Email</h2>
                    <p>Dear {firstName},</p>
                    <p>Thank you for registering. Please use the verification code below to complete your account creation:</p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <div style=""display: inline-block; background: #eff6ff; border: 2px solid #3b82f6; border-radius: 12px; padding: 16px 40px;"">
                            <p style=""margin: 0; font-size: 36px; font-weight: bold; letter-spacing: 12px; color: #1d4ed8;"">{code}</p>
                        </div>
                    </div>
                    <p>This code is valid for <strong>10 minutes</strong>. If you did not attempt to register, please ignore this email.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Registration verification code sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration verification email to {Email}", toEmail);
        }
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
    {
        var smtp = _configuration.GetSection("Smtp");
        var host = smtp["Host"];
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"];
        var password = smtp["Password"];
        var fromEmail = smtp["FromEmail"] ?? username ?? string.Empty;
        var fromName = smtp["FromName"] ?? "Capitec Dispute Portal";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping password reset code email to {Email}", toEmail);
            return;
        }

        var subject = "Your Password Reset Code";
        var body = $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color: #333;"">
                <div style=""max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px;"">
                    <h2 style=""color: #005eb8;"">Capitec Bank – Password Reset</h2>
                    <p>Dear {firstName},</p>
                    <p>We received a request to reset your password. Use the verification code below to proceed:</p>
                    <div style=""text-align: center; margin: 32px 0;"">
                        <div style=""display: inline-block; background: #eff6ff; border: 2px solid #3b82f6; border-radius: 12px; padding: 16px 40px;"">
                            <p style=""margin: 0; font-size: 36px; font-weight: bold; letter-spacing: 12px; color: #1d4ed8;"">{code}</p>
                        </div>
                    </div>
                    <p>This code is valid for <strong>10 minutes</strong>. If you did not request a password reset, please ignore this email — your password will remain unchanged.</p>
                    <p style=""color: #888; font-size: 12px;"">This is an automated message. Please do not reply to this email.</p>
                </div>
            </body>
            </html>";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Password reset code sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset code email to {Email}", toEmail);
        }
    }
}
