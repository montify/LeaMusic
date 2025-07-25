using LeaMusic.src.Services.ResourceServices_;

namespace LeaMusic.src.Services.Interfaces
{
    public interface ISyncService
    {
        public Task<bool> DetermineSyncLocationAsync(
            string projectName,
            Location location,
            Action<string> statusCallback
        );
    }
}
