using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;

namespace LeaMusic.src
{
    
    public class Track
    {
        public WaveStream audio;
        public RubberBandWaveStream rubberBandWaveStream;
        public LoopStream loopStream;
        public WaveformProvider waveformProvider;


        public VolumeSampleProvider volumeStream;
        public TimeSpan ClipDuration { get; }
        public WaveFormat Waveformat { get; set; }
        public string RelativePath { get;  set; }
        public string FileName { get;  set; }

        public string OriginFilePath { get; set; }

        public int ID { get; set; }

        public static int UNIQUE_ID = 0;
        public bool IsMuted { get; set; }

        public Track()
        {
            
        }

        public Track(string path)
        {
            OriginFilePath = path;
            RelativePath = path;
            FileName = Path.GetFileName(path);
            audio = ConvertMp3ToWav(path);
            waveformProvider = new WaveformProvider(audio.ToSampleProvider(), (int)audio.TotalTime.TotalSeconds);
            ClipDuration = audio.TotalTime;
            Waveformat = audio.WaveFormat;
            loopStream = new LoopStream(audio, 0, ClipDuration.TotalSeconds);
            rubberBandWaveStream = new RubberBandWaveStream(new WaveFloatTo16Provider(loopStream));
            volumeStream = new VolumeSampleProvider(rubberBandWaveStream.ToSampleProvider());

           ID = ++UNIQUE_ID;
           
        }

        public void JumpToPosition(TimeSpan position)
        {
            loopStream.JumpToSeconds(position.TotalSeconds);
        }

        public void SetVolumte(float volume)
        {
            volumeStream.Volume = volume;
        }

        private static WaveStream ConvertMp3ToWav(string inputPath)
        {
            var outputPath = Path.GetDirectoryName(inputPath);
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            outputPath = outputPath + $"\\{fileName}_AUTO_CONVERT_TO_WAV.wav";

            if (Path.Exists(outputPath))
            {
                Debug.WriteLine("No Conversation needed, .wav exists");
                return new AudioFileReader(outputPath);
            }


            using (Mp3FileReader mp3 = new Mp3FileReader(inputPath))
            {
                using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
                {
                    WaveFileWriter.CreateWaveFile(outputPath, pcm);
                }
            }

            return new AudioFileReader(outputPath);
        }
 
    }
}