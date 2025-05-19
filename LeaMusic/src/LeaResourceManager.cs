using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Text.Json;

namespace LeaMusic.src
{
    public class LeaResourceManager
    {
        //TODO: Cache Resources!
        //TODO: Relative Paths!
        public WaveStream LoadAudioFile(string path)
        {
            return new Mp3FileReader(path);
        }

        public async Task<Project> LoadProjectFromFileAsync(string path)
        {
            var file = await File.ReadAllTextAsync(path);

            var project = JsonSerializer.Deserialize<Project>(file);

            if (project == null)
                throw new NullReferenceException($"Cant load Project Path: {path}");

            var projectPath = Path.GetDirectoryName(path);

            await Task.Run(() =>
            {
                for (int i = 0; i < project.Tracks.Count; i++)
                {
                    var originalFilePath = project.Tracks[i].OriginFilePath;

                    project.Tracks[i] = LoadOrImportTrackFromFile(originalFilePath);
                }
            });

            project.WaveFormat = project.Tracks[0].Waveformat;

            return project;
        }

        public void SaveProject(Project project, string projectPathFolder)
        {
            var projectDirectory = new DirectoryInfo(projectPathFolder);
            if (!projectDirectory.Exists)
                projectDirectory = Directory.CreateDirectory($"{projectPathFolder}");

            var audioFilesDirectory = new DirectoryInfo($"{projectDirectory.FullName}/AudioFiles");
            if (!audioFilesDirectory.Exists)
                audioFilesDirectory = Directory.CreateDirectory($"{audioFilesDirectory.FullName}");

            var waveformDirectory = new DirectoryInfo($"{projectDirectory.FullName}/Waveforms");
            if (!waveformDirectory.Exists)
                waveformDirectory = Directory.CreateDirectory($"{waveformDirectory.FullName}");


            foreach (var track in project.Tracks)
            {
                track.RelativePath = audioFilesDirectory.Name;

                var oldPath = track.OriginFilePath;

                if (string.IsNullOrEmpty(oldPath))
                    throw new NullReferenceException("Path cant be null");

                track.OriginFilePath = audioFilesDirectory.FullName + "/" + track.FileName;
                track.WaveformBinaryFilePath = waveformDirectory.FullName + $"\\{track.FileName}.waveformat";

                if (!File.Exists(audioFilesDirectory.FullName + "/" + track.FileName))
                    File.Copy(oldPath, audioFilesDirectory.FullName + "/" + track.FileName, overwrite: true);

                var waveform = track.waveformProvider.waveformBuffer;

                WriteWaveformBinary(waveform, waveformDirectory.FullName + $"\\{track.FileName}.waveformat");
            }

            JsonSerializerOptions o = new JsonSerializerOptions();
            o.WriteIndented = true;

            var metaData = JsonSerializer.Serialize(project, o);
            File.WriteAllText(Path.Combine(projectDirectory.ToString(), project.Name + ".prj"), metaData);
        }


        //Import: When the Audio IS NOT in the Project/Audio Folder
        //Load: When the Audio IS IN the Project/Audio Folder
        //So Import copy the AudioFile from Source to Project/audioFolder
        public Track LoadOrImportTrackFromFile(string path)
        {
            var track = new Track();
            track.ImportTrack(path, this);
            track.waveformProvider = LoadOrImportWaveform(track);

            return track;
        }

        private WaveformProvider LoadOrImportWaveform(Track track)
        {
            if (track == null || string.IsNullOrEmpty(track.OriginFilePath))
                throw new NullReferenceException("Cant load Track");

            WaveformProvider waveformProvider;
            string projectFolder = track.OriginFilePath;

            if (Path.Exists($"{projectFolder}\\Waveforms\\{track.FileName}.waveformat"))
            {
                var waveform = ReadWaveformBinary($"{projectFolder}\\Waveforms\\{track.FileName}.waveformat");
                waveformProvider = new WaveformProvider(waveform, new WaveFormat(8000, 32, 2));
            }
            else
            {
                var downsampleAudio = ResampleWav(track.audio);
                waveformProvider = new WaveformProvider(downsampleAudio, (int)track.audio.TotalTime.TotalSeconds);
            }

            return waveformProvider;
        }

        private float[] ReadWaveformBinary(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            float[] result = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        private ISampleProvider ResampleWav(WaveStream wavestream)
        {
            var resampledAudio = new WdlResamplingSampleProvider(wavestream.ToSampleProvider(), 8000);

            //  WaveFileWriter.CreateWaveFile("C:/t/re.wav", resampledAudio.ToWaveProvider());

            return resampledAudio;
        }

        private void WriteWaveformBinary(float[] audioSamples, string path)
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
