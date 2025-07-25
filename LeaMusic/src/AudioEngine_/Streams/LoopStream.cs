using NAudio.Wave;

namespace LeaMusic.src.AudioEngine_.Streams
{
    public class LoopStream : WaveStream
    {
        private readonly WaveStream m_sourceStream;
        private readonly long m_loopStartBytes;
        private readonly long m_loopEndBytes;
        private readonly long m_startPosition;

        public LoopStream(WaveStream source, double startTimeSec, double loopDurationSec)
        {
            m_sourceStream = source;
            m_startPosition = (long)(startTimeSec * source.WaveFormat.AverageBytesPerSecond);
            m_loopStartBytes = m_startPosition;
            m_loopEndBytes =
                m_loopStartBytes
                + (long)(loopDurationSec * source.WaveFormat.AverageBytesPerSecond);

            // Ensure we don't exceed file length
            if (m_loopEndBytes > m_sourceStream.Length)
            {
                m_loopEndBytes = m_sourceStream.Length;
            }

            m_sourceStream.Position = m_startPosition;
        }

        public void JumpToSeconds(double startTimeSec)
        {
            m_sourceStream.CurrentTime = TimeSpan.FromSeconds(startTimeSec);
        }

        public override WaveFormat WaveFormat => m_sourceStream.WaveFormat;

        public override long Length => m_loopEndBytes - m_loopStartBytes;

        public override long Position
        {
            get => m_sourceStream.Position - m_loopStartBytes;
            set
            {
                long newPos = m_loopStartBytes + value;
                if (newPos >= m_loopEndBytes)
                {
                    newPos = m_loopStartBytes;
                }

                m_sourceStream.Position = newPos;
            }
        }

        public float CurrentPositionInSec =>
            (float)Position / m_sourceStream.WaveFormat.AverageBytesPerSecond;

        public float TotalLengthInSec =>
            (float)m_sourceStream.Length / m_sourceStream.WaveFormat.AverageBytesPerSecond;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                long bytesRemainingUntilLoopEnd = m_loopEndBytes - m_sourceStream.Position;
                int bytesToRead = (int)Math.Min(count - totalBytesRead, bytesRemainingUntilLoopEnd);

                int bytesRead = m_sourceStream.Read(buffer, offset + totalBytesRead, bytesToRead);

                if (bytesRead == 0)
                {
                    // End of stream — jump to loop start
                    m_sourceStream.Position = m_loopStartBytes;
                    continue;
                }

                totalBytesRead += bytesRead;

                // If we reached loop end, wrap around
                if (m_sourceStream.Position >= m_loopEndBytes)
                {
                    m_sourceStream.Position = m_loopStartBytes;
                }
            }

            return totalBytesRead;
        }
    }
}
