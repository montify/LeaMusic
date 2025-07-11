namespace LeaMusic.src.AudioEngine_
{
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class NaudioMixer : IMixer
    {
        private readonly MixingSampleProvider m_mixer;

        public WaveFormat WaveFormat => m_mixer.WaveFormat;

        public NaudioMixer()
        {
            m_mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        }

        public void AddMixerInput(Track track)
        {
            m_mixer.AddMixerInput(track.VolumeStream);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return m_mixer.Read(buffer, offset, count);
        }

        public void RemoveAllMixerInputs()
        {
            m_mixer.RemoveAllMixerInputs();
        }
    }
}
