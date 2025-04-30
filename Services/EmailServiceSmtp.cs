using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Zoom.Helper;
using Zoom.Models;
using Zoom.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Routing;

public class EmailServiceSmtp : IEmailServiceSmtp
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUrlHelper _urlHelper;

    public EmailServiceSmtp(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IActionContextAccessor actionContextAccessor,
        IUrlHelperFactory urlHelperFactory)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        if (actionContextAccessor.ActionContext != null)
        {
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
        }
    }

    public async Task<MessageResult> SendMeetingConfirmationEmail(string email, string subject, string body)
    {
        try
        {
            var smtpServer = _configuration["Smtp:Server"];
            var smtpPort = int.Parse(_configuration["Smtp:Port"]);

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                // Disable authentication and SSL
                client.Credentials = null;
                client.EnableSsl = false;

                var fromEmail = "no-reply@kalventis.com";
                var fromName = "Zoom Connect";
                var from = new MailAddress(fromEmail, fromName);
                var to = new MailAddress(email);

                using (var message = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await client.SendMailAsync(message);
                }
            }

            return new MessageResult() { ErrorMessage = "Email sent successfully!", ErrorType = 0 };
        }
        catch (Exception e)
        {
            return new MessageResult() { ErrorMessage = "Failed to send email: " + e.Message, ErrorType = 1 };
        }
    }

    public string RenderMeetingConfirmationEmail(MeetingModel meeting, string startUrl, string joinUrl, string meetingId, string passcode)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        var scheme = request?.Scheme ?? "https";  
        var host = request?.Host.Value ?? "apps.kalventis.com";  

        // Gunakan UrlHelper untuk menghasilkan URL dinamis
        string redirectStartUrl = _urlHelper != null
            ? _urlHelper.Action("Start", "ZoomRedirect", new { id = meetingId }, scheme, host)
            : $"{scheme}://{host}{_httpContextAccessor.HttpContext?.Request.PathBase}/ZoomRedirect/Start/{meetingId}";

        return $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; }}
        .highlight {{ font-weight: bold; color: #4CAF50; }}
        .button {{
            background-color: #4CAF50;
            color: white;
            padding: 10px 20px;
            text-align: center;
            display: inline-block;
            border-radius: 5px;
            text-decoration: none;
        }}
        .start-button {{
            color: #1a73e8;
            font-weight: bold;
            text-decoration: underline;
            font-size: 22px;
            padding: 12px 25px;
        }}
        .start-button:hover {{
            text-decoration: none;
        }}
    </style>
</head>
<body>
    <p>Dear {meeting.Email},</p>
    <p>Your Zoom meeting <span class='highlight'>{meeting.Topic}</span> has been successfully booked.</p>
    <p><strong>Agenda:</strong> {meeting.Agenda}</p>
    <p><strong>Start Time:</strong> {meeting.StartDate} at {meeting.StartTime}</p>

    <p><strong>Meeting ID:</strong> {meetingId}</p>
    <p><strong>Passcode:</strong> {passcode}</p>

    <p>Please ensure that the meeting takes place at the scheduled time.</p>

    <p>Please copy or share the following link with the participants to join the meeting:</p>
    <a href='{joinUrl}'>{joinUrl}</a></p>

    <p>As the host, please use the link below and ensure that you are connected to the Kalventis network or your VPN is active to start the meeting:</p>
    <p><a href='{redirectStartUrl}' class='start-button'>Start Zoom Meeting</a></p>

    <p>If you have any questions or issues, feel free to reach out to us.</p>
    <p>Best regards,<br>Zone System</p>
</body>
</html>";
    }
}
