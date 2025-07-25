using NAudio.Wave;
using RubberBand;

namespace LeaMusic.src.AudioEngine_.Streams
{
    public class RubberBandWaveStream : IWaveProvider
    {
        private IWaveProvider m_source;
        private byte[] m_sourceBuffer;
        private float[][] m_sourceSamples;
        private RubberBandStretcher m_stretcher;
        private float[][] m_stretchedSamples;
        private double m_tempo;
        private bool m_tempoChanged;

        public WaveFormat WaveFormatIEEE { get; set; }

        public void SetPitch(double pitch) => m_stretcher.SetPitchScale(pitch);

        public RubberBandWaveStream(IWaveProvider source)
        {
            if (source.WaveFormat.BitsPerSample != 16)
            {
                throw new FormatException(
                    "Can't process bit depth of " + source.WaveFormat.BitsPerSample
                );
            }

            m_source = source;
            WaveFormatIEEE = WaveFormat.CreateIeeeFloatWaveFormat(
                m_source.WaveFormat.SampleRate,
                m_source.WaveFormat.Channels
            );
            m_sourceSamples = Enumerable
                .Range(1, source.WaveFormat.Channels)
                .Select(channel => new float[16384])
                .ToArray();
            m_sourceBuffer = new byte[m_sourceSamples.Sum(channel => channel.Length) * 2];
            m_stretchedSamples = Enumerable
                .Range(1, source.WaveFormat.Channels)
                .Select(channel => new float[16384])
                .ToArray();
            m_stretcher = new RubberBandStretcher(
                m_source.WaveFormat.SampleRate,
                m_source.WaveFormat.Channels,
                RubberBandStretcher.Options.ProcessRealTime
            );
            m_tempo = 1.0;
        }

        public double Tempo
        {
            get { return m_tempo; }
            set
            {
                m_tempo = value;
                m_tempoChanged = true;
            }
        }

        public WaveFormat WaveFormat => m_source.WaveFormat;

        private List<byte> m_sourceExtraBytes = new List<byte>();

        private List<byte> m_outputExtraBytes = new List<byte>();

        private event EventHandler SourceRead;

        private event EventHandler EndOfStream;

        public void Reset() => m_stretcher.Reset();

        public void SeekTo(TimeSpan time)
        {
            m_stretcher.Reset();

            if (m_source is WaveStream sourceStream)
            {
                sourceStream.CurrentTime = time;
            }

            m_sourceExtraBytes.Clear();
            m_outputExtraBytes.Clear();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int numRead = 0;

            // Mismatched formats/interpretations:
            //
            // - Source returns raw bytes, lets us interpret.
            // - SoundTouch takes samples (Int16), counts one frame across all channels as a single sample (one left + one right == one sample).
            // - When converting to/from bytes, we need to count each channel in a frame as a separate sample (one left + one right == two samples).
            // - We implement IWaveProvider, the same as source, and are thus expected to return raw bytes.
            // - We may be asked for a number of bytes that isn't a multiple of the stretcher's output block size.
            // - We may be provided with source data that isn't a multiple of the stretcher's input block size.
            //
            // Hooray!
            if (m_outputExtraBytes.Count > 0)
            {
                if (m_outputExtraBytes.Count > count)
                {
                    m_outputExtraBytes.CopyTo(0, buffer, offset, count);
                    m_outputExtraBytes.RemoveRange(0, count);

                    return count;
                }
                else
                {
                    m_outputExtraBytes.CopyTo(buffer);

                    count -= m_outputExtraBytes.Count;
                    numRead += m_outputExtraBytes.Count;

                    m_outputExtraBytes.Clear();
                }
            }

            int bytesPerFrame = 2 * m_source.WaveFormat.Channels;

            while (true)
            {
                int stretchedFramesToRead = (count + bytesPerFrame - 1) / bytesPerFrame;

                if (stretchedFramesToRead > m_stretchedSamples[0].Length)
                {
                    stretchedFramesToRead = m_stretchedSamples[0].Length;
                }

                if (m_tempoChanged)
                {
                    m_stretcher.SetTimeRatio(1.0 / m_tempo);
                    m_tempoChanged = false;
                }

                int numberOfFramesRead = (int)
                    m_stretcher.Retrieve(m_stretchedSamples, stretchedFramesToRead);

                if (numberOfFramesRead == 0)
                {
                    int sourceBytesRead = m_sourceExtraBytes.Count;

                    if (sourceBytesRead > 0)
                    {
                        m_sourceExtraBytes.CopyTo(m_sourceBuffer);
                        m_sourceExtraBytes.Clear();
                    }

                    sourceBytesRead += m_source.Read(
                        m_sourceBuffer,
                        sourceBytesRead,
                        m_sourceBuffer.Length - sourceBytesRead
                    );

                    SourceRead?.Invoke(this, EventArgs.Empty);

                    if (sourceBytesRead == 0)
                    {
                        // End of stream, zero pad
                        Array.Clear(buffer, offset, count);

                        numRead += count;

                        EndOfStream?.Invoke(this, EventArgs.Empty);

                        return numRead;
                    }

                    int numberOfSourceSamplesPerChannel =
                        sourceBytesRead / 2 / m_source.WaveFormat.Channels;

                    int sourceBytesInSamples =
                        numberOfSourceSamplesPerChannel * m_source.WaveFormat.Channels * 2;

                    if (sourceBytesInSamples < sourceBytesRead)
                    {
                        // We got a misaligned read, stash the bytes we aren't going to process for the next pass.
                        for (int i = sourceBytesInSamples; i < sourceBytesRead; i++)
                        {
                            m_sourceExtraBytes.Add(m_sourceBuffer[i]);
                        }
                    }

                    for (int channel = 0; channel < m_source.WaveFormat.Channels; channel++)
                    {
                        int channelOffset = channel * 2;

                        for (int i = 0; i < numberOfSourceSamplesPerChannel; i++)
                        {
                            int lo = m_sourceBuffer[(i * bytesPerFrame) + channelOffset];
                            int hi = m_sourceBuffer[(i * bytesPerFrame) + channelOffset + 1];

                            short sampleValue = unchecked((short)(hi << 8 | lo));

                            m_sourceSamples[channel][i] = sampleValue * (1.0f / 32768.0f);
                        }
                    }

                    m_stretcher.Process(
                        m_sourceSamples,
                        numberOfSourceSamplesPerChannel,
                        final: false
                    );
                }
                else
                {
                    int i = 0;
                    int channel = 0;

                    while (i < numberOfFramesRead)
                    {
                        if (count == 0)
                        {
                            break;
                        }

                        float rawSample = m_stretchedSamples[channel][i];

                        channel++;

                        if (channel == m_source.WaveFormat.Channels)
                        {
                            channel = 0;
                            i++;
                        }
                        unchecked
                        {
                            short sample;

                            if (rawSample <= -1.0)
                            {
                                sample = -32768;
                            }
                            else if (rawSample >= 1.0)
                            {
                                sample = +32767;
                            }
                            else
                            {
                                int wideSample = (int)(rawSample * 32768.0f);

                                if (wideSample < -32768)
                                {
                                    sample = -32768;
                                }
                                else if (wideSample > 32767)
                                {
                                    sample = 32767;
                                }
                                else
                                {
                                    sample = (short)wideSample;
                                }
                            }

                            byte hi = (byte)(sample >> 8);
                            byte lo = (byte)(sample & 0xFF);

                            buffer[offset++] = lo;
                            numRead++;
                            count--;

                            if (count == 0)
                            {
                                m_outputExtraBytes.Add(hi);
                                break;
                            }

                            buffer[offset++] = hi;
                            numRead++;
                            count--;
                        }
                    }

                    while (i < numberOfFramesRead)
                    {
                        float rawSample = m_stretchedSamples[channel][i];

                        channel++;

                        if (channel == m_source.WaveFormat.Channels)
                        {
                            channel = 0;
                            i++;
                        }
                        unchecked
                        {
                            short sample;

                            if (rawSample <= -1.0)
                            {
                                sample = -32768;
                            }
                            else if (rawSample >= 1.0)
                            {
                                sample = +32767;
                            }
                            else
                            {
                                int wideSample = (int)(rawSample * 32768.0f);

                                if (wideSample < -32768)
                                {
                                    sample = -32768;
                                }
                                else if (wideSample > 32767)
                                {
                                    sample = 32767;
                                }
                                else
                                {
                                    sample = (short)wideSample;
                                }
                            }

                            byte hi = (byte)(sample >> 8);
                            byte lo = (byte)(sample & 0xFF);

                            m_outputExtraBytes.Add(lo);
                            m_outputExtraBytes.Add(hi);
                        }
                    }

                    if (count == 0)
                    {
                        return numRead;
                    }
                }
            }
        }
    }
}
