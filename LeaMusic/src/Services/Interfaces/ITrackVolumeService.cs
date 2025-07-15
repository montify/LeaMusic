namespace LeaMusic.src.Services.Interfaces
{
    public interface ITrackVolumeService
    {
        void SoloTrack(int trackId);

        void MuteTrack(int trackId);

        void MuteAllTracks();

        void SetTrackVolume(int trackId, float volume);
    }
}
