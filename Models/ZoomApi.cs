using Newtonsoft.Json;

namespace Zoom.Models
{
    public class ZoomApi
    {
        [JsonProperty("access_token")]
        public string access_token { get; set; }

        [JsonProperty("token_type")]
        public string token_type { get; set; }

        [JsonProperty("refresh_token")]
        public string refresh_token { get; set; }

        [JsonProperty("expires_in")]
        public int? expires_in { get; set; }

        [JsonProperty("scope")]
        public string scope { get; set; }
        public DateTime? last_refresh { get; set; }
        public bool? isValid { get; set; }

        public ZoomMeetingApi ZoomMeeting { get; set; }
        public MeetingListApi MeetingList { get; set; }
        public ResponeApi ResponeApi { get; set; }
    }
    public class ResponeApi
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
    public class ZoomMeetingApi
    {

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("host_id")]
        public string HostId { get; set; }

        [JsonProperty("host_email")]
        public string HostEmail { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("agenda")]
        public string Agenda { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("start_url")]
        public string StartUrl { get; set; }

        [JsonProperty("join_url")]
        public string JoinUrl { get; set; }
        [JsonProperty("duration")]
        public double Duration { get; set; }
        [JsonProperty("password")]
        public string Passcode { get; set; }

    }

    public class MeetingListApi
    {

        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        [JsonProperty("total_records")]
        public int TotalRecords { get; set; }

        [JsonProperty("next_page_token")]
        public string NextPageToken { get; set; }

        [JsonProperty("meetings")]
        public List<MeetingListModelApi> Meetings { get; set; }
    }
    public class MeetingListModelApi
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("host_id")]
        public string HostId { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("agenda")]
        public string Agenda { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("join_url")]
        public string JoinUrl { get; set; }
    }
}
