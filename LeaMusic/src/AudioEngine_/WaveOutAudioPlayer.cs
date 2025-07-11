namespace LeaMusic.src.AudioEngine_
{
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;

    public class WaveOutAudioPlayer : IAudioPlayer
    {
        private WaveOutEvent m_waveOut;
        private PlaybackState m_playbackState;

        public PlaybackState GetAudioPlaybackState() => m_playbackState;

        public WaveOutAudioPlayer()
        {
            m_waveOut = new WaveOutEvent();

            m_waveOut.DesiredLatency = 450;
        }

        public void Init(IMixer mixer)
        {
            if (m_waveOut != null)
            {
                m_waveOut.Dispose();
                m_waveOut = null;
            }

            m_waveOut = new WaveOutEvent();
            m_waveOut.DesiredLatency = 450;

            m_waveOut.Init(mixer);
        }

        public void Pause()
        {
            m_playbackState = PlaybackState.Pause;

            m_waveOut.Pause();
        }

        public void Play()
        {
            m_playbackState = PlaybackState.Playing;

            m_waveOut.Play();
        }

        public void Stop()
        {
            m_playbackState = PlaybackState.Stop;
            m_waveOut.Stop();
        }
    }
}
