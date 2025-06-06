using NAudio.Wave;
using PortAudioSharp;
using System.Runtime.InteropServices;

namespace LeaMusic.src.AudioEngine_
{
     public class PortAudioWaveOut : IDisposable
    {
        private StreamParameters parameters;
        private ISampleProvider sampleProvider;
        private PortAudioSharp.Stream outputStream;

        public TimeSpan CurrentTime { get; private set; }

        public event Action<TimeSpan> OnPlay;

        public PortAudioWaveOut()
        {
            PortAudio.Initialize();
        }

        public void Init(ISampleProvider sampleProvider)
        {
            if (sampleProvider.WaveFormat.BitsPerSample != sizeof(float) * 8)
                throw new Exception("Must be 32bit float format");

            this.sampleProvider = sampleProvider;
            int deviceIndex = PortAudio.DefaultOutputDevice;
            var info = PortAudio.GetDeviceInfo(deviceIndex);
            Console.WriteLine($"Using device: {info.name}, Sample Rate: {sampleProvider.WaveFormat.SampleRate}, Channels: {sampleProvider.WaveFormat.Channels}");

            parameters = new StreamParameters
            {
                device = deviceIndex,
                channelCount = sampleProvider.WaveFormat.Channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = info.defaultHighOutputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };
            Console.WriteLine($"LATENCY: {parameters.suggestedLatency}");
        }
        double startTime = -1;
        public void Play()
        {
            PortAudioSharp.Stream.Callback callback = (IntPtr input, IntPtr output,
               uint frameCount,
               ref StreamCallbackTimeInfo timeInfo,
               StreamCallbackFlags statusFlags,
               IntPtr userData) =>
            {
                int samplesRequested = (int)(frameCount * sampleProvider.WaveFormat.Channels);
                float[] floatBuffer = new float[samplesRequested];

                int samplesRead = sampleProvider.Read(floatBuffer, 0, samplesRequested);

                //// Zero-fill if underrun
                //if (samplesRead < samplesRequested)
                //{
                //    Array.Clear(floatBuffer, samplesRead, samplesRequested - samplesRead);
                //}

                if (startTime < 0)
                    startTime = timeInfo.currentTime;

                double elapsed = timeInfo.currentTime - startTime;

                CurrentTime = TimeSpan.FromSeconds(elapsed);

                OnPlay?.Invoke(CurrentTime);

                Marshal.Copy(floatBuffer, 0, output, samplesRequested);

                return samplesRead == 0 ? StreamCallbackResult.Complete : StreamCallbackResult.Continue;
            };

            outputStream = new PortAudioSharp.Stream(null, parameters, sampleProvider.WaveFormat.SampleRate, 0, StreamFlags.ClipOff, callback, IntPtr.Zero);
            outputStream.Start();

        }

        public void Stop()
        {
            if (outputStream != null)
            {
                if (outputStream.IsActive)
                    outputStream.Stop();

            }
        }

        public void Dispose()
        {
            if (outputStream != null)
                outputStream.Dispose();
        }
    }
}
