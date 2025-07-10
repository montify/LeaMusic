namespace LeaMusic.src.Services.ResourceServices_
{
    public interface IBinaryWriter
    {
        void WriteWaveformBinary(float[] audioSamples, string path);
    }
}
