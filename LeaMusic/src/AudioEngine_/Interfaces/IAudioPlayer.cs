using LeaMusic.src.Services.Interfaces;

namespace LeaMusic.src.AudioEngine_.Interfaces
{
    public interface IAudioPlayer
    {
        void Init(IMixer mixer);

        void Play();

        void Pause();

        void Stop();

        public PlaybackState GetAudioPlaybackState();
    }
}
