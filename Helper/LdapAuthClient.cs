using Microsoft.Extensions.Options;
using System.Text;

namespace Zoom.Helper
{
    public class LdapAuthClient
    {
        private readonly AdditionalSetting _setting;
        public LdapAuthClient(IOptions<AdditionalSetting> setting)
        {
            _setting = setting.Value;
        }
        public async Task<bool> Authenticate(string username, string password)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_setting.BaseAddress);
                var authRequest = new
                {
                    Username = username,
                    Password = password
                };
                var json = System.Text.Json.JsonSerializer.Serialize(authRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/ldapauth/authenticate", content);

                return response.IsSuccessStatusCode; // Return true or false based on response
            }
        }
    }

}
