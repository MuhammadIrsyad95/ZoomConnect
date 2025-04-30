using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Zoom.Services.Interfaces;
using Zoom.Helper;
using Zoom.Models;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<MessageResult> SendMeetingConfirmationEmail(string email, string subject, string body)
    {
        try
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("ZoomApps@kalventis.com", "Zoom Apps");
            var message = new SendGridMessage
            {
                From = from,
                Subject = subject,
                HtmlContent = body // Menggunakan body yang diterima dari controller
            };

            var to = new EmailAddress(email);
            message.AddTo(to);

            await client.SendEmailAsync(message);

            return new MessageResult() { ErrorMessage = "Email sent successfully!", ErrorType = 0 };
        }
        catch (Exception e)
        {
            return new MessageResult() { ErrorMessage = "Failed to send email: " + e.Message, ErrorType = 1 };
        }
    }

    public string RenderMeetingConfirmationEmail(MeetingModel meeting, string startUrl)
    {
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
            </style>
        </head>
        <body>
            <p>Dear Participant,</p>
            <p>Your Zoom meeting <span class='highlight'>{meeting.Topic}</span> has been successfully booked.</p>
            <p><strong>Agenda:</strong> {meeting.Agenda}</p>
            <p><strong>Start Time:</strong> {meeting.StartDate} at {meeting.StartTime}</p>
            <p>Please join the meeting by clicking the link below:</p>
            <p><a href='{startUrl}' class='button'>Join Zoom Meeting</a></p>
            <p>If you have any questions or issues, feel free to reach out to us.</p>
            <p>Best regards,</p>
            <p>Your Zoom Scheduling Team</p>
        </body>
        </html>";
    }
}
