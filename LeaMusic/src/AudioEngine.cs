using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Text.Json;

namespace LeaMusic.src
{
    public class AudioEngine
    {
        private WaveOutEvent waveOut;
        private Stopwatch sw = new Stopwatch();

        public TimeSpan ViewStartTime;
        public TimeSpan ViewEndTime;
        public TimeSpan ViewDuration;
        public TimeSpan InitStart { get; private set; }
        public TimeSpan AccumulatedProgress { get; private set; }
        public TimeSpan LastUpdateTime { get; private set; }
        public TimeSpan CurrentPosition { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan LoopStart { get; private set; }
        public TimeSpan LoopEnd { get; private set; }
        public TimeSpan JumpToPosition;

        private TimeSpan m_oldPosition;
        private TimeSpan m_oldLoopStart = TimeSpan.FromSeconds(-1);
        private MixingSampleProvider mixer;
        public Project Project { get; private set; }

        public double Speed = 1;
        public double Pitch;
        public double Zoom { get; set; } = 1;
        private double m_oldZoom = -1;

  
        public AudioEngine()
        {
         
        }

        public void ReloadMixerInputs()
        {
            mixer.RemoveAllMixerInputs();

            foreach (var track in Project.Tracks)
            {
                mixer.AddMixerInput(track.volumeStream);
            }
        }


        public void LoadProject(Project project)
        {
            waveOut?.Stop();
            waveOut =  new WaveOutEvent();
            Project = project;
            //if (audioTrack.ClipDuration != audioTrack2.ClipDuration)
            //    throw new Exception("Clip must be the same Length");

            TotalDuration = TimeSpan.FromSeconds(Project.Duration.TotalSeconds);

            mixer = new MixingSampleProvider(Project.WaveFormat);
            
            ReloadMixerInputs();

            ViewStartTime = TimeSpan.Zero;
            ViewEndTime = TotalDuration;
            waveOut.DesiredLatency = 150;

            Debug.WriteLine($"WavOut Latency: {waveOut.DesiredLatency}");
           
            //TODO: Init can happen only once in wavOut Lifetime, this is a Hack lol
            try
            {
                waveOut.Init(mixer);
               
            }
            catch (Exception)
            {              
            }
        }


        public void SaveProject(string projectPathFolder)
        {
            DirectoryInfo projectDirectory = new DirectoryInfo(projectPathFolder);
           
            if (!projectDirectory.Exists)
            {
                projectDirectory = Directory.CreateDirectory($"{projectPathFolder}");
            }

            DirectoryInfo audioFilesDirectory = new DirectoryInfo($"{projectDirectory.FullName}/AudioFiles");
            
            if (!audioFilesDirectory.Exists)
            {
                audioFilesDirectory = Directory.CreateDirectory($"{audioFilesDirectory.FullName}");
            }
       

            foreach (var track in Project.Tracks)
            {
                track.RelativePath = audioFilesDirectory.Name;

                var oldPath = track.OriginFilePath;

                track.OriginFilePath = audioFilesDirectory.FullName + "/" + track.FileName;

                waveOut.Stop();

                if(!File.Exists(audioFilesDirectory.FullName + "/" + track.FileName))
                    File.Copy(oldPath, audioFilesDirectory.FullName + "/" + track.FileName, overwrite: true);

            }

            JsonSerializerOptions o = new JsonSerializerOptions();
            o.WriteIndented = true;

            var metaData = JsonSerializer.Serialize(Project, o);
            File.WriteAllText(Path.Combine(projectDirectory.ToString(), Project.Name + ".prj"), metaData);
        }


        public static Project LoadProjectFromFile(string path)
        {
            var file = File.ReadAllText(path);
            var project = JsonSerializer.Deserialize<Project>(file);

            var projectPath = Path.GetDirectoryName(path);

            for (int i = 0; i < project.Tracks.Count; i++)
            {
                var originalFilePath = project.Tracks[i].OriginFilePath;

                project.Tracks[i] = new Track(originalFilePath);

            }
            project.WaveFormat = project.Tracks[0].Waveformat;
            return project;
        }

        public void Update()
        {
            CalculateProgress();
            ViewDuration = ViewEndTime - ViewStartTime;

            UpdateLoop();

            OnUpdate?.Invoke(CurrentPosition);

            //If CurrentPosition moves
            if (CurrentPosition != m_oldPosition)
            {
                
                OnProgressChange?.Invoke(CurrentPosition);

                m_oldPosition = CurrentPosition;
            }
        }

        private void CalculateProgress()
        {
            TimeSpan now = sw.Elapsed;
            TimeSpan deltaRealTime = now - LastUpdateTime;

            AccumulatedProgress += deltaRealTime * Speed;
            LastUpdateTime = now;

            CurrentPosition = (AccumulatedProgress ) + InitStart;
        }

        public void ZoomWaveForm(double zoomFactor)
        {
            Zoom = zoomFactor;

            if (Zoom != 1)
            {
                TimeSpan baseWindow = TotalDuration;
                TimeSpan timeWindow = baseWindow / Zoom;
                TimeSpan halfWindow = timeWindow / 2.0f;

                ViewStartTime = CurrentPosition - halfWindow;
                ViewEndTime = CurrentPosition + halfWindow;

                ViewStartTime = TimeSpan.FromSeconds(Math.Max(0, ViewStartTime.TotalSeconds));
                ViewEndTime = TimeSpan.FromSeconds(Math.Min(TotalDuration.TotalSeconds, ViewEndTime.TotalSeconds));
            }
            else
            {
                ViewStartTime = TimeSpan.Zero;
                ViewEndTime = TotalDuration;          
            }
        }

        double oldScrollValue = 0;

        public void ScrollWaveForm(double scrollFactor)
        {
            double diff = scrollFactor - oldScrollValue;

            //NOTE: When add Slider value direct, it never goes back to the origin Value, use difference :D
            ViewStartTime += TimeSpan.FromSeconds(diff);
            ViewEndTime += TimeSpan.FromSeconds(diff);

            var start = TimeSpan.FromSeconds(Math.Max(ViewStartTime.TotalSeconds, 0));
            var end = TimeSpan.FromSeconds(Math.Min(ViewEndTime.TotalSeconds, TotalDuration.TotalSeconds));

            ViewStartTime = start;
            ViewEndTime = end;

            UpdateLoop();
 
            OnLoopChange?.Invoke(LoopStart, LoopEnd);

            oldScrollValue = scrollFactor;
        }

        public void Play()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
                waveOut.Stop();

            waveOut.Play();
            sw.Start();
        }

        public void Pause()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
                waveOut.Pause();

            sw.Stop();
        }

        public void ChangeSpeed(double speed)
        {
            Speed = speed;

            Project.SetTempo(Speed);
        }

        public void ChangePitch(int semitones)
        {
            var pitch = Math.Pow(2.0, semitones / 12.0);

            foreach (var track in Project.Tracks)
            {
                track.rubberBandWaveStream.SetPitch(pitch);
            }
        }

        public void Replay()
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                AudioJumpToSec(LoopStart);
                Play();
            }
        }

        public void Stop()
        {
            waveOut?.Stop();
        }

        public void MuteTrack(int trackID)
        {

          var track =  Project.Tracks.Where(t => t.ID == trackID).FirstOrDefault();

            if (track == null)
                return;

            if(track.IsMuted)
            {
                track.SetVolumte(1);
                track.IsMuted = false;
            }
            else
            {
                track.SetVolumte(0);
                track.IsMuted = true;
            }
        }

        public void AudioJumpToSec(TimeSpan sec)
        {
            waveOut.Stop();

            Project.ResetTracks();
            CurrentPosition = sec;

            sw.Reset();
            InitStart = sec;

 
            Project.JumpToSeconds(sec);
            LastUpdateTime = TimeSpan.Zero;
            AccumulatedProgress = TimeSpan.Zero;
        }

        public void Loop(TimeSpan startSec, TimeSpan endSec)
        {
            if (startSec >= endSec || endSec.TotalSeconds - startSec.TotalSeconds <= 0)
            {
                LoopStart = TimeSpan.Zero;
                LoopEnd = TimeSpan.Zero;

                OnLoopChange?.Invoke(LoopStart, LoopEnd);
                return;
            }

            LoopStart = startSec;
            LoopEnd = endSec;

            AudioJumpToSec(startSec);
            Play();
        }

        private void UpdateLoop()
        {
            bool isLoop = LoopEnd.TotalSeconds - LoopStart.TotalSeconds > 0;
            if (isLoop)
            {
                //when reach end of Loop
                if (CurrentPosition >= LoopEnd)
                {
                    AudioJumpToSec(LoopStart);
                    Play();
                    Update();
                }

                //if zoom OR LoopStartSelection change
                if (m_oldZoom != Zoom || LoopStart.TotalSeconds != m_oldLoopStart.TotalSeconds)
                {
                    Debug.WriteLine("LOOP INVOKE");
                    OnLoopChange?.Invoke(LoopStart, LoopEnd);
                    m_oldLoopStart = LoopStart;

                    m_oldZoom = Zoom;
                }
            }
            else
            {
                LoopStart = TimeSpan.Zero;
                LoopEnd = TimeSpan.Zero;
            }
        }

        public void AddMarker(TimeSpan position, string text)
        {
            var m = new Marker(position, text);
            Project.AddMarker(m);
        }

      

        public event Action<TimeSpan> OnUpdate;
        public event Action<TimeSpan> OnProgressChange;
        public event Action<TimeSpan, TimeSpan> OnLoopChange;
    }
}