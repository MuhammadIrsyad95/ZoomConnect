using System.Threading.Tasks;
using Zoom.Helper;
using Zoom.Models;

namespace Zoom.Services.Interfaces
{
    public interface IEmailService
    {
        Task<MessageResult> SendMeetingConfirmationEmail(string email, string subject, string body);
        string RenderMeetingConfirmationEmail(MeetingModel meeting, string startUrl); // Tambahkan ini
    }
}
