namespace Zoom.Models
{
    public class MeetingModel
    {
        public int ID { get; set; }
        public string Email { get; set; }
        public string Topic { get; set; }
        public DateOnly? StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public TimeOnly? StartTime { get; set; }
        public DateOnly? EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public TimeOnly? EndTime { get; set; }

        public int? Duration { get; set; }

        public string Agenda { get; set; }
        public string StartUrl { get; set; }
        public string JoinUrl { get; set; }


    }
}
