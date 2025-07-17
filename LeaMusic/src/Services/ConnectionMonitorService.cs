using LeaMusic.src.Services.Interfaces;

namespace LeaMusic.src.Services
{
    public class ConnectionMonitorService : IConnectionMonitorService
    {
        public async Task<bool> CheckInternetConnection()
        {
            try
            {
                using var client = new HttpClient();
                using var response = await client.GetAsync("https://www.google.com", HttpCompletionOption.ResponseHeadersRead);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
