namespace LeaMusic.src.Services.Interfaces
{
    public interface IBinaryWriter
    {
        void WriteWaveformBinary(float[] audioSamples, string path);
    }
}
