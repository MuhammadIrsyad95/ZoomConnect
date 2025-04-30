using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.Net;
using System.Security.Claims;
using System.Text;
using Zoom.Models;
using Zoom.Services;
using Zoom.Services.Interfaces;

namespace Zoom.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ZoomContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailServiceSmtp _emailServiceSmtp; // Inject IEmailService

        public static ZoomApi CurrentZoom { get; set; } = new ZoomApi();

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ZoomContext context, IMemoryCache memoryCache, IEmailServiceSmtp emailServiceSmtp)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _memoryCache = memoryCache;
            _emailServiceSmtp = emailServiceSmtp;
        }

        public IActionResult Index()
        {
            // Ambil email dari claim setelah login
            string userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Set email di ViewBag
            ViewBag.UserEmail = userEmail;

            // Membuat model MeetingModel untuk dikirim ke view
            MeetingModel meeting = new MeetingModel();

            return View(meeting);
        }

        public async Task<bool> IsAvailable(DateTime startDate, TimeOnly startTime, DateTime endDate, TimeOnly endTime, string email)
        {
            // Convert time components to TimeSpan for comparison
            TimeSpan startTs = startTime.ToTimeSpan();
            TimeSpan endTs = endTime.ToTimeSpan();

            // Check if there is any meeting that overlaps with the given range for the same host
            var meeting = await _context.Meetings.FirstOrDefaultAsync(x =>
                x.ZoomAccount.Equals(email) &&
                (
                    (x.StartDate < endDate.Date || (x.StartDate == endDate.Date && x.StartTime < endTs)) &&
                    (x.EndDate > startDate.Date || (x.EndDate == startDate.Date && x.EndTime > startTs))
                )
            );

            return meeting == null;
        }

        public async Task<IActionResult> LaunchMeeting(long zoomID)
        {
            try
            {
                await LoginZoom();

                var client = new RestClient("https://api.zoom.us");
                var request = new RestRequest($"/v2/meetings/{zoomID}", Method.Get);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");
                RestResponse response = await client.ExecuteAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    CurrentZoom.ZoomMeeting = JsonConvert.DeserializeObject<ZoomMeetingApi>(response.Content);
                }
                else
                {
                    CurrentZoom.ResponeApi = JsonConvert.DeserializeObject<ResponeApi>(response.Content);
                    TempData["error"] = CurrentZoom.ResponeApi.Message;
                    return RedirectToAction("List");
                }
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return Redirect(CurrentZoom.ZoomMeeting.StartUrl);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeeting(MeetingModel meeting)
        {
            try
            {
                // Validasi: Periksa apakah start time dan end time sama
                DateTime startDateTime = meeting.StartDate.Value.ToDateTime(meeting.StartTime.Value);
                DateTime endDateTime = meeting.EndDate.Value.ToDateTime(meeting.EndTime.Value);

                // Pastikan start time dan end time tidak sama baik pada tanggal yang sama atau tidak
                if (startDateTime.Date == endDateTime.Date && startDateTime == endDateTime)
                {
                    TempData["error"] = "Start time and end time cannot be the same.";
                    return RedirectToAction("Index");
                }

                // Melakukan login ke Zoom
                await LoginZoom();

                var userList = await _context.ZoomAccounts.ToListAsync();
                bool isAvailable = false;
                string email = string.Empty;
                var startDate = TimeZoneInfo.ConvertTimeToUtc(startDateTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta"));
                var endDate = TimeZoneInfo.ConvertTimeToUtc(endDateTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta"));
                int duration = (int)(endDateTime - startDateTime).TotalMinutes;

                // Check if the meeting time is available for the Zoom accounts
                foreach (var user in userList)
                {
                    isAvailable = await IsAvailable(startDateTime, meeting.StartTime.Value, endDateTime, meeting.EndTime.Value, user.Email);
                    if (isAvailable)
                    {
                        email = user.Email;
                        var warningMessage = await CheckPotentialConflict(startDateTime, email);
                        if (warningMessage != null)
                        {
                            TempData["warning"] = warningMessage;
                        }
                        break;
                    }
                }

                if (isAvailable)
                {
                    var client = new RestClient("https://api.zoom.us");
                    var request = new RestRequest($"/v2/users/{email}/meetings", Method.Post);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");

                    var settings = new ExpandoObject() as IDictionary<string, Object>;
                    settings.Add("approval_type", 0);
                    settings.Add("registrants_email_notification", false);
                    settings.Add("registrants_confirmation_email", false);
                    settings.Add("waiting_room", false);
                    settings.Add("join_before_host", true);

                    var meetingRequest = new
                    {
                        topic = meeting.Topic,
                        agenda = meeting.Agenda,
                        type = 3,
                        start_time = startDate,
                        duration = (int)(endDateTime - startDateTime).TotalMinutes,
                        timezone = "Asia/Jakarta",
                        default_password = true,
                        registration_type = 0,
                        settings = settings
                    };

                    request.AddJsonBody(meetingRequest);

                    var response = await client.ExecuteAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        CurrentZoom.ZoomMeeting = JsonConvert.DeserializeObject<ZoomMeetingApi>(response.Content);
                        await _context.Meetings.AddAsync(new Meeting
                        {
                            Topic = meeting.Topic,
                            Agenda = meeting.Agenda,
                            StartDate = startDateTime,
                            StartTime = meeting.StartTime.Value.ToTimeSpan(),
                            Duration = (int)CurrentZoom.ZoomMeeting.Duration,
                            EndDate = endDateTime,
                            EndTime = meeting.EndTime.Value.ToTimeSpan(),
                            StartUrl = CurrentZoom.ZoomMeeting.StartUrl,
                            JoinUrl = CurrentZoom.ZoomMeeting.JoinUrl,
                            ZoomId = CurrentZoom.ZoomMeeting.Id,
                            Email = meeting.Email,
                            Passcode = CurrentZoom.ZoomMeeting.Passcode,
                            ZoomAccount = CurrentZoom.ZoomMeeting.HostEmail,
                        });

                        await _context.SaveChangesAsync();

                        // Send confirmation email to the host and participants
                        try
                        {
                            string emailSubject = "Zoom Meeting Confirmation";
                            string emailBody = _emailServiceSmtp.RenderMeetingConfirmationEmail(
                                meeting,
                                CurrentZoom.ZoomMeeting.StartUrl,
                                CurrentZoom.ZoomMeeting.JoinUrl,
                                CurrentZoom.ZoomMeeting.Id.ToString(),  // Add the meetingId
                                CurrentZoom.ZoomMeeting.Passcode      // Add the passcode
                            );

                            await _emailServiceSmtp.SendMeetingConfirmationEmail(meeting.Email, emailSubject, emailBody);
                            _logger.LogInformation($"Email sent successfully to {meeting.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError($"Failed to send email: {emailEx.Message}");
                            TempData["error"] = "There was an issue sending the confirmation email.";
                        }

                        TempData["success"] = CurrentZoom.ZoomMeeting.Duration != duration
                            ? "Zoom meeting booked but duration has been changed due to Zoom user policy"
                            : "Zoom meeting booked!";
                    }
                    else
                    {
                        CurrentZoom.ResponeApi = JsonConvert.DeserializeObject<ResponeApi>(response.Content);
                        TempData["error"] = CurrentZoom.ResponeApi.Message;
                    }
                }
                else
                {
                    TempData["error"] = "The requested Zoom meeting time is already booked. Please choose a different time slot. If you need further assistance, kindly contact ITS or create an ITSwift ticket.";
                }
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            return RedirectToAction("Index");
        }

        [Route("ZoomRedirect/Start/{id}")]
        public async Task<IActionResult> StartRedirect(long id)
        {
            try
            {
                await LoginZoom();

                var client = new RestClient("https://api.zoom.us");
                var request = new RestRequest($"/v2/meetings/{id}", Method.Get);
                request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");

                var response = await client.ExecuteAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    TempData["error"] = "Gagal mengambil data Zoom.";
                    return RedirectToAction("Index", "Home");
                }

                var meeting = JsonConvert.DeserializeObject<ZoomMeetingApi>(response.Content);

                var dbMeeting = await _context.Meetings.FirstOrDefaultAsync(m => m.ZoomId == id);
                if (dbMeeting != null)
                {
                    dbMeeting.StartUrl = meeting.StartUrl;
                    _context.Meetings.Update(dbMeeting);
                    await _context.SaveChangesAsync();
                }

                return Redirect(meeting.StartUrl);
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Terjadi kesalahan: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }



        public async Task<IActionResult> List()
        {
            return View(await _context.Meetings.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> GetReportList(long meetingID, string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["error"] = "Email is required.";
                return RedirectToAction("Report");
            }

            // Validasi apakah email tersebut adalah host dari meeting
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.ZoomId == meetingID);
            if (meeting == null)
            {
                TempData["error"] = $"No meeting found with ID {meetingID}.";
                return RedirectToAction("Report");
            }

            if (!meeting.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = $"The provided email does not match the creator of meeting ID {meetingID}.";
                return RedirectToAction("Report");
            }

            List<ParticipantApi> participants = new List<ParticipantApi>();
            try
            {
                await LoginZoom();
                var client = new RestClient("https://zoom.us");
                var request = new RestRequest($"/v2/report/meetings/{meetingID}/participants", Method.Get);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");

                string nextPageToken = string.Empty;
                do
                {
                    if (!string.IsNullOrEmpty(nextPageToken))
                        request.AddParameter("next_page_token", nextPageToken);

                    RestResponse response = await client.ExecuteAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseApi = JsonConvert.DeserializeObject<ParticipantResponseApi>(response.Content);
                        participants.AddRange(responseApi.Participants);
                        nextPageToken = responseApi.NextPageToken;
                    }
                } while (!string.IsNullOrEmpty(nextPageToken));
            }
            catch (Exception e)
            {
                TempData["error"] = e.Message;
            }
            string cacheKey = $"{meetingID}_{email.ToLower()}";
            _memoryCache.Set(cacheKey, participants, TimeSpan.FromMinutes(30));

            return RedirectToAction("Report", new { cacheKey, email });

        }

        public IActionResult Report(string cacheKey, string email)
        {
            if (!string.IsNullOrEmpty(email))
            {
                ViewBag.Email = email;
            }
            else
            {
                ViewBag.Email = User.FindFirstValue(ClaimTypes.Email); // fallback ke email login
            }

            if (!string.IsNullOrEmpty(cacheKey) && _memoryCache.TryGetValue(cacheKey, out List<ParticipantApi> participants))
            {
                var parts = cacheKey.Split('_');
                if (parts.Length > 0)
                    ViewBag.MeetingID = parts[0];
                return View(participants);
            }

            return View(new List<ParticipantApi>());
        }


        public async Task LoginZoom()
        {
            try
            {
                var zoomAuthorized = _configuration["Zoom:Authorized"];
                var zoomAccountId = _configuration["Zoom:accountID"];

                var client = new RestClient("https://zoom.us");
                var request = new RestRequest("/oauth/token", Method.Post);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Authorization", $"Basic {zoomAuthorized}");
                request.AddParameter("grant_type", "account_credentials");
                request.AddParameter("account_id", zoomAccountId);
                RestResponse response = await client.ExecuteAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    CurrentZoom = JsonConvert.DeserializeObject<ZoomApi>(response.Content);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public async Task<IActionResult> GetZoomAccounts()
        {
            var accounts = await _context.Meetings
                .Select(m => m.ZoomAccount)
                .Distinct()
                .ToListAsync();

            return Json(accounts);
        }

        public async Task<IActionResult> Calendar()
        {
            var meetings = await _context.Meetings.ToListAsync();
            return View(meetings);
        }

        [HttpGet]
        public async Task<IActionResult> GetCalendarEvents(string zoomAccount = "all")
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var meetingsQuery = _context.Meetings.AsQueryable();

            if (zoomAccount != "all")
            {
                meetingsQuery = meetingsQuery.Where(m => m.ZoomAccount == zoomAccount);
            }

            var accounts = await _context.Meetings
                .Select(m => m.ZoomAccount)
                .Distinct()
                .ToListAsync();

            var colorMapping = accounts
                .Select((account, index) => new
                {
                    account,
                    color = $"hsl({(index * 60 + 200) % 360}, 80%, 60%)"
                })
                .ToDictionary(a => a.account, a => a.color);

            var meetings = await meetingsQuery.Select(m => new
            {
                id = m.ZoomId,
                title = m.Topic,
                start = m.StartDate.HasValue && m.StartTime.HasValue ? m.StartDate.Value.Add(m.StartTime.Value) : (DateTime?)null,
                end = m.EndDate.HasValue && m.EndTime.HasValue ? m.EndDate.Value.Add(m.EndTime.Value) : (DateTime?)null,
                backgroundColor = colorMapping.ContainsKey(m.ZoomAccount) ? colorMapping[m.ZoomAccount] : "#28a745",
                extendedProps = new
                {
                    // Menggunakan Url.Action untuk membuat URL yang dinamis
                    startUrl = Url.Action("StartRedirect", "Home", new { id = m.ZoomId }),
                    joinUrl = m.JoinUrl,
                    passcode = m.Passcode,
                    creatorEmail = m.Email,
                    meetingId = m.ZoomId
                }
            }).ToListAsync();

            return Json(meetings);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(long meetingID, string email, string format = "xlsx", string cacheKey = "")
        {
            if (string.IsNullOrWhiteSpace(email))
                return Content("Email is required to download the report.");

            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.ZoomId == meetingID);
            if (meeting == null)
                return Content($"No meeting found with ID {meetingID}.");

            if (!meeting.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
                return Content("The provided email does not match the meeting creator.");

            List<ParticipantApi> participants;
            cacheKey = string.IsNullOrWhiteSpace(cacheKey) ? $"{meetingID}_{email.ToLower()}" : cacheKey;

            if (!_memoryCache.TryGetValue(cacheKey, out participants))
            {
                // Kalau cache kosong, ambil ulang dari Zoom
                participants = new List<ParticipantApi>();
                try
                {
                    await LoginZoom();
                    var client = new RestClient("https://zoom.us");
                    var request = new RestRequest($"/v2/report/meetings/{meetingID}/participants", Method.Get);
                    request.AddHeader("Accept", "application/json");
                    request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");

                    string nextPageToken = string.Empty;
                    do
                    {
                        if (!string.IsNullOrEmpty(nextPageToken))
                            request.AddParameter("next_page_token", nextPageToken);

                        RestResponse response = await client.ExecuteAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseApi = JsonConvert.DeserializeObject<ParticipantResponseApi>(response.Content);
                            participants.AddRange(responseApi.Participants);
                            nextPageToken = responseApi.NextPageToken;
                        }
                    } while (!string.IsNullOrEmpty(nextPageToken));
                }
                catch (Exception e)
                {
                    return Content($"Error: {e.Message}");
                }
            }

            if (participants.Count == 0)
                return Content("No participant data found for this meeting.");

            return format.ToLower() == "csv" ? GenerateCsv(participants) : GenerateExcel(participants);
        }

        private IActionResult GenerateCsv(List<ParticipantApi> participants)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Name,Email,Join Time,Leave Time,Duration (Minutes)");

            foreach (var p in participants)
            {
                builder.AppendLine($"{p.Name},{p.UserEmail},{p.JoinTime},{p.LeaveTime},{p.Duration}");
            }

            byte[] csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
            return File(csvBytes, "text/csv", "Zoom_Report.csv");
        }

        private IActionResult GenerateExcel(List<ParticipantApi> participants)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Participants");
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "Email";
                worksheet.Cell(1, 3).Value = "Join Time";
                worksheet.Cell(1, 4).Value = "Leave Time";
                worksheet.Cell(1, 5).Value = "Duration (Minutes)";

                int row = 2;
                foreach (var p in participants)
                {
                    worksheet.Cell(row, 1).Value = p.Name;
                    worksheet.Cell(row, 2).Value = p.UserEmail;
                    worksheet.Cell(row, 3).Value = p.JoinTime.ToString();
                    worksheet.Cell(row, 4).Value = p.LeaveTime.ToString();
                    worksheet.Cell(row, 5).Value = p.Duration;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Zoom_Report.xlsx");
                }
            }
        }


        [HttpPost]
        public async Task<IActionResult> RescheduleMeeting([FromBody] RescheduleDto dto)
        {
            var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.ZoomId == dto.MeetingId);
            if (meeting == null) return NotFound();

            await LoginZoom();

            var client = new RestClient("https://api.zoom.us");
            var request = new RestRequest($"/v2/meetings/{meeting.ZoomId}", Method.Patch);
            request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");
            request.AddJsonBody(new
            {
                start_time = DateTime.Parse(dto.NewStart).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = (int)(DateTime.Parse(dto.NewEnd) - DateTime.Parse(dto.NewStart)).TotalMinutes
            });

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode, response.Content);

            // Update local DB
            var newStart = DateTime.Parse(dto.NewStart);
            var newEnd = DateTime.Parse(dto.NewEnd);

            meeting.StartDate = newStart.Date;
            meeting.EndDate = newEnd.Date;
            meeting.StartTime = newStart.TimeOfDay;
            meeting.EndTime = newEnd.TimeOfDay;


            await _context.SaveChangesAsync();

            return Ok();
        }

       

        [HttpPost]
        public async Task<IActionResult> CancelMeeting(long meetingId)
        {
            try
            {
                await LoginZoom();
                var meeting = await _context.Meetings.FirstOrDefaultAsync(m => m.ZoomId == meetingId);
                if (meeting == null) return NotFound("Meeting not found.");

                var client = new RestClient("https://api.zoom.us");
                var request = new RestRequest($"/v2/meetings/{meetingId}", Method.Delete);
                request.AddHeader("Authorization", $"Bearer {CurrentZoom.access_token}");

                var response = await client.ExecuteAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _context.Meetings.Remove(meeting);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Meeting canceled successfully.";
                }
                else
                {
                    TempData["error"] = "Failed to cancel the meeting.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("List");
        }

        private async Task<string?> CheckPotentialConflict(DateTime newStartDateTime, string selectedZoomEmail)
        {
            var newStartDate = newStartDateTime.Date;
            var bufferStart = newStartDateTime.AddMinutes(-30).TimeOfDay;
            var newStartTime = newStartDateTime.TimeOfDay;

            var conflictMeeting = await _context.Meetings
                .Where(x => x.ZoomAccount == selectedZoomEmail &&
                            x.EndDate == newStartDate &&
                            x.EndTime >= bufferStart &&
                            x.EndTime <= newStartTime)
                .OrderByDescending(x => x.EndTime)
                .FirstOrDefaultAsync();

            if (conflictMeeting != null)
            {
                return $"⚠️ Notice: The Zoom account '{selectedZoomEmail}' was just used for a meeting that ended at {conflictMeeting.EndTime:hh\\:mm}. " +
                       $"Your meeting is scheduled to start at {newStartDateTime:HH:mm}. There is a potential overlap if the previous meeting overruns. " +
                       $"We recommend adding a 30-minute buffer to avoid conflicts.";
            }

            return null;
        }

        [HttpPost]
        public async Task<IActionResult> CheckConflict([FromBody] MeetingModel meeting)
        {
            DateTime startDateTime = meeting.StartDate.Value.ToDateTime(meeting.StartTime.Value);

            var userList = await _context.ZoomAccounts.ToListAsync();

            foreach (var user in userList)
            {
                var isAvailable = await IsAvailable(startDateTime, meeting.StartTime.Value,
                                                    meeting.EndDate.Value.ToDateTime(meeting.EndTime.Value),
                                                    meeting.EndTime.Value, user.Email);
                if (isAvailable)
                {
                    var warningMessage = await CheckPotentialConflict(startDateTime, user.Email);
                    if (warningMessage != null)
                    {
                        return Json(new { hasWarning = true, message = warningMessage });
                    }
                    return Json(new { hasWarning = false });
                }
            }

            return Json(new { hasWarning = false }); // Default
        }


    }
}
