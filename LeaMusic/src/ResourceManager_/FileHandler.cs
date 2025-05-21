using LeaMusic.src.AudioEngine_;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Text.Json;

namespace LeaMusic.src.ResourceManager_
{
    public class FileHandler : IResourceHandler
    {
        public FileHandler()
        {
        }

        public Task SaveProject(Location projectLocation, Project project)
        {

            if (projectLocation is FileLocation fileLoccation)
            {
                var projectDirectory = OpenOrCreateDirectory(fileLoccation.Path);
                var audioFilesDirectory = OpenOrCreateDirectory($"{projectDirectory.FullName}/AudioFiles");
                var waveformDirectory = OpenOrCreateDirectory($"{projectDirectory.FullName}/Waveforms");

                foreach (var track in project.Tracks)
                {
                    track.AudioRelativePath = $"{audioFilesDirectory.Name}/{track.AudioFileName}";
                    track.WaveformRelativePath = $"{waveformDirectory.Name}/{track.AudioFileName}.waveformat";

                    //only trigger for first time Save
                    if (!File.Exists(audioFilesDirectory.FullName + "/" + track.AudioFileName))
                        File.Copy(track.OriginFilePath, audioFilesDirectory.FullName + "/" + track.AudioFileName, overwrite: true);

                    var waveform = track.waveformProvider.waveformBuffer;

                    var waveFormFilePath = waveformDirectory.FullName + $"\\{track.AudioFileName}.waveformat";

                    if (!File.Exists(waveFormFilePath))
                        WriteWaveformBinary(waveform, waveFormFilePath);
                }

                JsonSerializerOptions o = new JsonSerializerOptions();
                o.WriteIndented = true;

                var metaData = JsonSerializer.Serialize(project, o);
                File.WriteAllText(Path.Combine(projectDirectory.ToString(), project.Name + ".prj"), metaData);
            }

            return Task.CompletedTask;
        }

        public Track? ImportTrack(Location trackLocation, LeaResourceManager resourceManager)
        {
            if (trackLocation is FileLocation location)
            {
                var track = new Track();
                track.OriginFilePath = location.Path;
                track.LoadAudioFile(location.Path, resourceManager);
                track.waveformProvider = ImportWaveform(track);

                Debug.WriteLine($"Create new Track, : {track.AudioFileName}");

                return track;
            }
            else
                throw new NotSupportedException("Handler is not a FileHandler");

        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            var audioFilePath = Path.Combine(projectPath, track.AudioRelativePath);
            track.LoadAudioFile(audioFilePath, resourceManager);
            track.waveformProvider = LoadWaveform(projectPath, track);

            Debug.WriteLine($"Load Existing Track: {track.AudioFileName}");
            return track;
        }


        public async Task<Project> LoadProject(Location projectLocation, LeaResourceManager resourceManager)
        {
            if (projectLocation is FileLocation location)
            {
                if (string.IsNullOrEmpty(location.Path))
                    throw new ArgumentNullException("Path cant be null");

                var file = await File.ReadAllTextAsync(location.Path);

                var project = JsonSerializer.Deserialize<Project>(file);

                if (project == null)
                    throw new NullReferenceException($"Cant load Project Path: {location.Path}");

                var projectPath = Path.GetDirectoryName(location.Path);

                if (string.IsNullOrEmpty(projectPath))
                    throw new ArgumentNullException($"Cant find Project path {projectPath}");

                await Task.Run(() =>
                {
                    for (int i = 0; i < project.Tracks.Count; i++)
                    {
                        var track = project.Tracks[i];
                        //var originalFilePath = track.OriginFilePath;

                        var trackPath = Path.Combine(projectPath, track.AudioRelativePath);

                        track = LoadAudio(track, projectPath, resourceManager);
                    }
                });

                //We assume, for this Programm, that every Audio is the same ( Stems from the Same Audio file) so its safe to use the First track´s Waveformat
                project.WaveFormat = project.Tracks[0].Waveformat;

                return project;

            }
            else
                throw new NotSupportedException("Handler is not a FileHandler");
        }

        //PRIVATE STUFF
        private ISampleProvider ResampleWav(WaveStream wavestream)
        {
            var resampledAudio = new WdlResamplingSampleProvider(wavestream.ToSampleProvider(), 8000);

            //  WaveFileWriter.CreateWaveFile("C:/t/re.wav", resampledAudio.ToWaveProvider());
            return resampledAudio;
        }

        private WaveformProvider ImportWaveform(Track track)
        {
            var downsampleAudio = ResampleWav(track.audio);
            var waveformProvider = new WaveformProvider(downsampleAudio, (int)track.audio.TotalTime.TotalSeconds);

            Debug.WriteLine($"Create new Waveform from new ImportedTrack: {track.AudioFileName}");
            return waveformProvider;
        }

        private DirectoryInfo OpenOrCreateDirectory(string path)
        {
            var projectDirectory = new DirectoryInfo(path);
            if (!projectDirectory.Exists)
                projectDirectory = Directory.CreateDirectory($"{path}");

            return projectDirectory;
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

            Debug.WriteLine($"Write WaveformBinary to: {path}");
        }

        private float[] ReadWaveformBinary(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            float[] result = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        private WaveformProvider LoadWaveform(string projectPath, Track track)
        {
            var waveformPath = Path.Combine(projectPath, track.WaveformRelativePath);

            WaveformProvider waveformProvider;
        
            var waveFormRelativePath = track.WaveformRelativePath;

            if (!Path.Exists(waveformPath))
                throw new FileNotFoundException($"Cant load Waveform: {waveformPath}");

            var waveform = ReadWaveformBinary(waveformPath);
            waveformProvider = new WaveformProvider(waveform, new WaveFormat(8000, 32, 2));

            Debug.WriteLine($"Load existing Waveform from existing Track: {track.AudioFileName}");

            return waveformProvider;
        }
    }
}