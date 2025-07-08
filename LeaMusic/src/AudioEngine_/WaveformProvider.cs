using NAudio.Wave;

namespace LeaMusic.src.AudioEngine_
{
    public class WaveformProvider
    {
        public float[] WaveformBuffer { get; set; }

        private readonly WaveFormat m_waveFormat;

        public WaveformProvider(ISampleProvider sampleProvider, int totalTimeInSec)
        {
            int totalSamples = sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels * totalTimeInSec;
            WaveformBuffer = new float[totalSamples];

            sampleProvider.Read(WaveformBuffer, 0, WaveformBuffer.Length);

            m_waveFormat = sampleProvider.WaveFormat;
        }

        public WaveformProvider(float[] waveform, WaveFormat waveFormat)
        {
            WaveformBuffer = waveform;
            m_waveFormat = waveFormat;
        }

        public Memory<float> RequestSamples(double startInSec, double endInSec, int widthInPixel)
        {
            double startSampleIndex = startInSec * m_waveFormat.SampleRate * m_waveFormat.Channels;
            double endSampleIndex = endInSec * m_waveFormat.SampleRate * m_waveFormat.Channels;

            double totalSamplesInRange = endSampleIndex - startSampleIndex;
            double samplesPerPixel = totalSamplesInRange / widthInPixel;

            var resultSamples = new float[widthInPixel];

            for (int i = 0; i < resultSamples.Length; i++)
            {
                double sliceStart = Math.Max(startSampleIndex + (i * samplesPerPixel), 0);
                double sliceEnd = Math.Min(sliceStart + samplesPerPixel, endSampleIndex);

                int start = (int)sliceStart;
                int end = (int)sliceEnd;

                start = Math.Max(0, start);
                end = Math.Min(WaveformBuffer.Length, end);

                // Better Version no GC pressure
                if (start < end)
                {
                    float max = float.MinValue;
                    for (int j = start; j < end; j++)
                    {
                        if (WaveformBuffer[j] > max)
                        {
                            max = WaveformBuffer[j];
                        }
                    }

                    resultSamples[i] = max;
                }
                else
                {
                    resultSamples[i] = 0f;
                }
            }

            return resultSamples.AsMemory();
        }
    }
}