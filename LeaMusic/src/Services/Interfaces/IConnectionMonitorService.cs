namespace LeaMusic.src.Services.Interfaces
{
    public interface IConnectionMonitorService
    {
        public Task<bool> CheckInternetConnection();
    }
}
