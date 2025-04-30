using Newtonsoft.Json;

namespace Zoom.Models
{
    public class ParticipantApi
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("user_email")]
        public string UserEmail { get; set; }

        [JsonProperty("join_time")]
        public DateTime JoinTime { get; set; }

        [JsonProperty("leave_time")]
        public DateTime LeaveTime { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("attentiveness_score")]
        public string AttentivenessScore { get; set; }

        [JsonProperty("failover")]
        public bool Failover { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("customer_key")]
        public string CustomerKey { get; set; }

        [JsonProperty("participant_user_id")]
        public string ParticipantUserId { get; set; }

        public long MeetingID { get; set; }

    }
    public class ParticipantResponseApi
    {
        [JsonProperty("page_count")]
        public int PageCount { get; set; }

        [JsonProperty("page_size")]
        public int PageSize { get; set; }

        [JsonProperty("total_records")]
        public int TotalRecords { get; set; }

        [JsonProperty("next_page_token")]
        public string NextPageToken { get; set; }

        [JsonProperty("participants")]
        public List<ParticipantApi> Participants { get; set; }
    }
}
