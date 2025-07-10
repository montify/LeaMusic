namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class WaveformService : IWaveformService
    {
        public WaveformProvider GenerateFromAudio(Track track)
        {
            var resampler = new WdlResamplingSampleProvider(track.Audio.ToSampleProvider(), 3000);
            return new WaveformProvider(resampler, (int)track.Audio.TotalTime.TotalSeconds);
        }

        public byte[] GetWaveformData(WaveformProvider provider)
        {
            // Your existing binary writing logic, but to a memory stream
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                foreach (float sample in provider.WaveformBuffer)
                {
                    writer.Write(sample);
                }

                return ms.ToArray();
            }
        }

        public WaveformProvider LoadFromData(byte[] data)
        {
            // Your existing binary reading logic, but from a byte array
            float[] result = new float[data.Length / 4];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);

            // Assuming a fixed wave format for loaded waveforms
            return new WaveformProvider(result, new WaveFormat(3000, 32, 2));
        }
    }
}
