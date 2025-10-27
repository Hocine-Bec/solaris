using Application.Interfaces.Services;
using Domain.ValueObjects;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly GmailSetting _setting = new();    
    private readonly IGmailAuthService _authService;
    
    public EmailService(IGmailAuthService authService)
    {
        _authService = authService;
        _setting.SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? throw new InvalidOperationException("SMTP_HOST environment variable is not set");
        _setting.SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? throw new InvalidOperationException("SMTP_PORT environment variable is not set"));
        _setting.FromName = Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? throw new InvalidOperationException("SMTP_FROM_NAME environment variable is not set");
        _setting.FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? throw new InvalidOperationException("SMTP_FROM_EMAIL environment variable is not set");
    }

    public async Task SendLeadEmailAsync(string toEmail, string toName, string propertyAddress)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_setting.FromName, _setting.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "Your Solar Quote Request - We're on it! ☀️";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = $@"
            <!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <link href=""https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700&display=swap"" rel=""stylesheet"">
  <style>
    body {{
      font-family: 'Poppins';
    }}
  </style>
</head>
<body style=""margin:0;padding:0;background-color:#F2F5F9;"">
  <table role=""presentation"" style=""width:100%;background-color:#F2F5F9"">
    <tr>
      <td style=""padding:40px 20px"">
        <table role=""presentation""
          style=""max-width:640px;margin:0 auto;background-color:#FCFCFC;border-radius:16px;overflow:hidden;box-shadow:0 12px 40px rgba(11,31,59,.12)"">
          <!-- HERO -->
          <tr>
            <td
              style=""background:linear-gradient(135deg, #0B1F3B 0%, #1d3a5f 100%);padding:60px 40px;text-align:center"">
              <h1 style=""margin:0;color:#FDC11B;font-size:36px;font-weight:700;line-height:1.1;"">
                ☀️ Solaris
              </h1>
              <p style=""margin:12px 0 0;color:#B8C7E0;font-size:18px;font-weight:100;"">
                Your journey to premium solar starts here.
              </p>
            </td>
          </tr>
          <!-- BODY -->
          <tr>
            <td style=""padding:50px 40px"">
              <h2 style=""margin:0 0 20px;color:#0B1F3B;font-size:26px;font-weight:600;"">
                Hi {toName}! 👋
              </h2>
              <p style=""margin:0 0 22px;color:#5E6E85;font-size:16px;line-height:1.65;font-weight:400;"">
                Thank you for trusting Solaris. We're already designing a system that will cut your energy bill and your
                carbon footprint.
              </p>
              <!-- CARD -->
              <table role=""presentation""
                style=""width:100%;background-color:#FFF8F2;border-left:4px solid #FFB86C;border-radius:8px;margin:25px 0"">
                <tr>
                  <td style=""padding:24px"">
                    <p
                      style=""margin:0 0 6px;color:#5E6E85;font-size:12px;font-weight:600;text-transform:uppercase;letter-spacing:.6px"">
                      Quote request for
                    </p>
                    <p style=""margin:0;color:#0B1F3B;font-size:18px;font-weight:600"">
                      📍 {propertyAddress}
                    </p>
                  </td>
                </tr>
              </table>
              <h3 style=""margin:34px 0 16px;color:#0B1F3B;font-size:20px;font-weight:600;"">
                What happens next?
              </h3>
              <table role=""presentation"" style=""width:100%"">
                <tr>
                  <td style=""padding:14px 0;border-bottom:1px solid #E3EAF3"">
                    <span style=""font-size:20px;color:#FFB86C;padding-right:12px"">✅</span>
                    <span style=""color:#5E6E85;font-size:15px;font-weight:400;"">
                      Our solar architects will review your property.
                    </span>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:14px 0;border-bottom:1px solid #E3EAF3"">
                    <span style=""font-size:20px;color:#FFB86C;padding-right:12px"">📞</span>
                    <span style=""color:#5E6E85;font-size:15px;font-weight:400;"">
                      We'll call you within one business day.
                    </span>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:14px 0;border-bottom:1px solid #E3EAF3"">
                    <span style=""font-size:20px;color:#FFB86C;padding-right:12px"">🏠</span>
                    <span style=""color:#5E6E85;font-size:15px;font-weight:400;"">
                      Schedule a free site assessment.
                    </span>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:14px 0"">
                    <span style=""font-size:20px;color:#FFB86C;padding-right:12px"">💰</span>
                    <span style=""color:#5E6E85;font-size:15px;font-weight:400;"">
                      Receive your premium quote with financing options.
                    </span>
                  </td>
                </tr>
              </table>
              <!-- SUPPORT -->
              <table role=""presentation"" style=""width:100%;background-color:#F2F5F9;border-radius:8px;margin:30px 0 0"">
                <tr>
                  <td style=""padding:24px"">
                    <p style=""margin:0 0 6px;color:#0B1F3B;font-size:15px;font-weight:600;"">
                      Questions?
                    </p>
                    <p style=""margin:0;color:#5E6E85;font-size:14px;font-weight:400;"">
                      Reply to this email or call us at <strong style=""color:#0B1F3B;font-weight:600;"">(800)
                        SOLAR-NOW</strong>
                    </p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <!-- FOOTER -->
          <tr>
            <td style=""background-color:#0B1F3B;padding:36px 40px;text-align:center"">
              <p style=""margin:0 0 8px;color:#FDC11B;font-size:14px;font-weight:600;"">
                Solaris Team
              </p>
              <p style=""margin:0;color:#B8C7E0;font-size:13px;font-weight:400;"">
                Making premium solar energy accessible to everyone.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
            "
        };

        await SendEmailAsync(message);
    }

    public async Task SendSalesNotificationEmailAsync(int toCustomerId, string toName, string toEmail,
        string toPhone, string toAddress)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_setting.FromName, _setting.FromEmail));
        message.To.Add(new MailboxAddress(_setting.FromName, _setting.FromEmail));
        message.Subject = $"🔔 New Lead: {toName}";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        </head>
        <body style='margin: 0; padding: 0; background-color: #f3f4f6; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif;'>
            <table role='presentation' style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 40px 20px;'>
                        <table role='presentation' style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
                            <!-- Header -->
                            <tr>
                                <td style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 30px; text-align: center;'>
                                    <div style='font-size: 40px; margin-bottom: 10px;'>🔔</div>
                                    <h1 style='margin: 0; color: #ffffff; font-size: 26px; font-weight: 600;'>New Lead Alert</h1>
                                    <p style='margin: 8px 0 0 0; color: #d1fae5; font-size: 14px;'>From Website Form</p>
                                </td>
                            </tr>
                            
                            <!-- Lead Info -->
                            <tr>
                                <td style='padding: 40px 30px;'>
                                    <h2 style='margin: 0 0 24px 0; color: #1f2937; font-size: 20px; font-weight: 600;'>Lead Information</h2>
                                    
                                    <table role='presentation' style='width: 100%; border-collapse: collapse; background-color: #f0fdf4; border-radius: 8px; overflow: hidden;'>
                                        <tr>
                                            <td style='padding: 16px 20px; border-bottom: 1px solid #d1fae5;'>
                                                <div style='color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;'>Full Name</div>
                                                <div style='color: #1f2937; font-size: 16px; font-weight: 600;'>{toName}</div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 16px 20px; border-bottom: 1px solid #d1fae5;'>
                                                <div style='color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;'>Email</div>
                                                <div><a href='mailto:{toEmail}' style='color: #10b981; font-size: 15px; text-decoration: none; font-weight: 500;'>{toEmail}</a></div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 16px 20px; border-bottom: 1px solid #d1fae5;'>
                                                <div style='color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;'>Phone</div>
                                                <div><a href='tel:{toPhone}' style='color: #10b981; font-size: 15px; text-decoration: none; font-weight: 500;'>{toPhone}</a></div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 16px 20px; border-bottom: 1px solid #d1fae5;'>
                                                <div style='color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;'>Property Address</div>
                                                <div style='color: #1f2937; font-size: 15px;'>{toAddress}</div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 16px 20px;'>
                                                <div style='color: #6b7280; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px;'>Lead ID</div>
                                                <div style='color: #1f2937; font-size: 15px; font-weight: 600;'>#{toCustomerId}</div>
                                            </td>
                                        </tr>
                                    </table>
                                    
                                    <!-- CTA Button -->
                                    <table role='presentation' style='width: 100%; margin: 30px 0;'>
                                        <tr>
                                            <td style='text-align: center;'>
                                                <a href='http://localhost:5000/admin/customers/{toCustomerId}' 
                                                   style='display: inline-block; background-color: #10b981; color: #ffffff; 
                                                          padding: 14px 32px; text-decoration: none; border-radius: 8px; 
                                                          font-weight: 600; font-size: 15px; box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);'>
                                                    View Lead in CRM →
                                                </a>
                                            </td>
                                        </tr>
                                    </table>
                                    
                                    <!-- Alert Box -->
                                    <div style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px 20px; border-radius: 6px; margin-top: 30px;'>
                                        <p style='margin: 0; color: #92400e; font-size: 14px; line-height: 1.6;'>
                                            <strong style='font-weight: 600;'>⏰ Action Required:</strong> Contact this lead within 24 hours for best conversion results.
                                        </p>
                                    </div>
                                </td>
                            </tr>
                            
                            <!-- Footer -->
                            <tr>
                                <td style='background-color: #f9fafb; padding: 20px 30px; text-align: center; border-top: 1px solid #e5e7eb;'>
                                    <p style='margin: 0; color: #9ca3af; font-size: 12px;'>Solar CRM Lead Management System</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
    "
        };

        await SendEmailAsync(message);
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        var accessToken = await _authService.GetAccessTokenAsync();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_setting.SmtpHost, _setting.SmtpPort, SecureSocketOptions.StartTls);
        var oauth2 = new SaslMechanismOAuth2(_setting.FromEmail, accessToken);
        await smtp.AuthenticateAsync(oauth2);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}