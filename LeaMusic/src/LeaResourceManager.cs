using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Text.Json;

namespace LeaMusic.src
{

    // Import: Create a track from an audio file that does not yet exist in the project (generate a new waveform file).
    // Load: Load a track from an existing project (load an existing waveform file).
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
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("Path cant be null");

            var file = await File.ReadAllTextAsync(path);

            var project = JsonSerializer.Deserialize<Project>(file);

            if (project == null)
                throw new NullReferenceException($"Cant load Project Path: {path}");

            var projectPath = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentNullException($"Cant find Project path {projectPath}");

            await Task.Run(() =>
            {
                for (int i = 0; i < project.Tracks.Count; i++)
                {
                    var track = project.Tracks[i];
                    var originalFilePath = track.OriginFilePath;

                    var trackPath = Path.Combine(projectPath, track.AudioRelativePath);

                    track = LoadTrack(projectPath, track);
                }
            });

            //We assume, for this Programm, that every Audio is the same ( Stems from the Same Audio file) so its safe to use the First track´s Waveformat
            project.WaveFormat = project.Tracks[0].Waveformat;

            return project;
        }

        public void SaveProject(Project project, string projectPathFolder)
        {
            var projectDirectory = OpenOrCreateDirectory(projectPathFolder);
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


        //Import: When the Audio IS NOT in the Project/Audio Folder
        //Load: When the Audio IS IN the Project/Audio Folder
        //So Import copy the AudioFile from Source to Project/audioFolder
        public Track ImportTrack(string trackPath)
        {
            var track = new Track();
            track.OriginFilePath = trackPath;
            track.LoadAudioFile(trackPath, this);

            track.waveformProvider = ImportWaveform(track);


            Debug.WriteLine($"Create new Track, : {track.AudioFileName}");
            return track;
        }

        private WaveformProvider ImportWaveform(Track track)
        {
            var downsampleAudio = ResampleWav(track.audio);
            var waveformProvider = new WaveformProvider(downsampleAudio, (int)track.audio.TotalTime.TotalSeconds);

            Debug.WriteLine($"Create new Waveform from new ImportedTrack: {track.AudioFileName}");
            return waveformProvider;
        }

        private Track LoadTrack(string projectPath, Track track)
        {
            var audioFilePath = Path.Combine(projectPath, track.AudioRelativePath);
            track.LoadAudioFile(audioFilePath, this);
            track.waveformProvider = LoadWaveform(projectPath, track);

            Debug.WriteLine($"Load Existing Track: {track.AudioFileName}");
            return track;
        }

        private WaveformProvider LoadWaveform(string projectPath, Track track)
        {
            var waveformPath = Path.Combine(projectPath, track.WaveformRelativePath);

            if (track == null || string.IsNullOrEmpty(track.OriginFilePath))
                throw new NullReferenceException("Cant load Track");

            WaveformProvider waveformProvider;
            string projectFolder = track.OriginFilePath;
            var waveFormRelativePath = track.WaveformRelativePath;


            if (!Path.Exists(waveformPath))
                throw new FileNotFoundException($"Cant load Waveform: {waveformPath}");

            var waveform = ReadWaveformBinary(waveformPath);
            waveformProvider = new WaveformProvider(waveform, new WaveFormat(8000, 32, 2));

            Debug.WriteLine($"Load existing Waveform from existing Track: {track.AudioFileName}");

            return waveformProvider;
        }

        private DirectoryInfo OpenOrCreateDirectory(string path)
        {
            var projectDirectory = new DirectoryInfo(path);
            if (!projectDirectory.Exists)
                projectDirectory = Directory.CreateDirectory($"{path}");

            return projectDirectory;
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

            Debug.WriteLine($"Write WaveformBinary to: {path}");
        }
    }
}
