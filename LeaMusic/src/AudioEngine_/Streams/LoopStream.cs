using NAudio.Wave;

namespace LeaMusic.src.AudioEngine_.Streams
{
    public class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;
        public readonly long loopStartBytes;
        public readonly long loopEndBytes;
        public readonly long startPosition;

        public LoopStream(WaveStream source, double startTimeSec, double loopDurationSec)
        {
            sourceStream = source;
            startPosition = (long)(startTimeSec * source.WaveFormat.AverageBytesPerSecond);
            loopStartBytes = startPosition;
            loopEndBytes = loopStartBytes + (long)(loopDurationSec * source.WaveFormat.AverageBytesPerSecond);

            // Ensure we don't exceed file length
            if (loopEndBytes > sourceStream.Length)
                loopEndBytes = sourceStream.Length;

            sourceStream.Position = startPosition;
        }

        public void JumpToSeconds(double startTimeSec)
        {
            sourceStream.CurrentTime = TimeSpan.FromSeconds(startTimeSec);
        }

        public override WaveFormat WaveFormat => sourceStream.WaveFormat;

        public override long Length => loopEndBytes - loopStartBytes;

        public override long Position
        {
            get => sourceStream.Position - loopStartBytes;
            set
            {
                long newPos = loopStartBytes + value;
                if (newPos >= loopEndBytes)
                    newPos = loopStartBytes;

                sourceStream.Position = newPos;
            }
        }


        public float CurrentPositionInSec => (float)Position / sourceStream.WaveFormat.AverageBytesPerSecond;
        public float TotalLengthInSec => (float)sourceStream.Length / sourceStream.WaveFormat.AverageBytesPerSecond;


        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                long bytesRemainingUntilLoopEnd = loopEndBytes - sourceStream.Position;
                int bytesToRead = (int)Math.Min(count - totalBytesRead, bytesRemainingUntilLoopEnd);

                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, bytesToRead);

                if (bytesRead == 0)
                {
                    // End of stream — jump to loop start
                    sourceStream.Position = loopStartBytes;
                    continue;
                }

                totalBytesRead += bytesRead;

                // If we reached loop end, wrap around
                if (sourceStream.Position >= loopEndBytes)
                {
                    sourceStream.Position = loopStartBytes;
                }
            }

            return totalBytesRead;
        }

    }
}
