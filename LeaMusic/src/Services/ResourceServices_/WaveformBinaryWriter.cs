namespace LeaMusic.src.Services.ResourceServices_
{
    public class WaveformBinaryWriter : IBinaryWriter
    {
        public void WriteWaveformBinary(float[] audioSamples, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                foreach (float sample in audioSamples)
                {
                    writer.Write(sample);
                }
            }
        }
    }
}
