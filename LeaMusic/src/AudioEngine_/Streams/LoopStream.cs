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
            int bytesRead = sourceStream.Read(buffer, offset, count);

            if (sourceStream.Position >= loopEndBytes) // If we reach loop end, restart
            {
                sourceStream.Position = loopStartBytes;
            }

            return bytesRead;
        }
    }
}
