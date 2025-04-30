namespace Zoom.Models
{
    public class RescheduleDto
    {
        public long MeetingId { get; set; }
        public string NewStart { get; set; }
        public string NewEnd { get; set; }
    }
}
  