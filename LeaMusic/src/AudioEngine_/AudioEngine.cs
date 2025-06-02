using LeaMusic.Extensions;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;

namespace LeaMusic.src.AudioEngine_
{
    public class AudioEngine
    {
        public Project Project { get; private set; }
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

        private MixingSampleProvider mixer;
        private WaveOutEvent waveOut;
        private Stopwatch sw = new Stopwatch();
        private TimeSpan m_oldPosition;
        private TimeSpan m_oldLoopStart = TimeSpan.FromSeconds(-1);
        private double Speed = 1;
        private double Zoom { get; set; } = 1;
        private double m_oldZoom = -1;

        public event Action<TimeSpan> OnUpdate;
        public event Action<TimeSpan> OnProgressChange;
        public event Action<TimeSpan, TimeSpan> OnLoopChange;

        public AudioEngine()
        {
        }

        public void MountProject(Project project)
        {
            Zoom = 1;

            waveOut?.Stop();
            waveOut =  new WaveOutEvent();
            Project = project;
           
            TotalDuration = TimeSpan.FromSeconds(Project.Duration.TotalSeconds);
            
            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            
            ReloadMixerInputs();

            ViewStartTime = TimeSpan.Zero;
            ViewEndTime = TotalDuration;
            waveOut.DesiredLatency = 450;
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

        public void ReloadMixerInputs()
        {
            mixer.RemoveAllMixerInputs();

            foreach (var track in Project.Tracks)
            {
                mixer.AddMixerInput(track.volumeStream);
            }
        }
        
        public void AddMarker(TimeSpan position, string text)
        {

            var m = new Marker(position , text);
            Project.AddTimeMarker(m);
        }

        public TimeSpan halfViewWindow;
        public void ZoomWaveForm(double zoomFactor, TimeSpan zoomPosition)
        {
           // Zoom = zoomFactor;

            if (Zoom != 1)
            {
                TimeSpan baseWindow = TotalDuration;
                TimeSpan timeWindow = baseWindow / zoomFactor;
                halfViewWindow = timeWindow / 2.0f;

                ViewStartTime = zoomPosition - halfViewWindow;
                ViewEndTime = zoomPosition + halfViewWindow;

                ViewStartTime = TimeSpan.FromSeconds(Math.Max(0, ViewStartTime.TotalSeconds));
                ViewEndTime = TimeSpan.FromSeconds(Math.Min(TotalDuration.TotalSeconds, ViewEndTime.TotalSeconds)); 
            }
            else
            {
                ViewStartTime = TimeSpan.FromSeconds(0);
                ViewEndTime = TimeSpan.FromSeconds(TotalDuration.TotalSeconds);
            }


            OnLoopChange?.Invoke(LoopStart, LoopEnd);
        }

        //Used for Zoom with mouse
        public void ZoomWaveFormRelative(double zoomFactor, TimeSpan zoomPosition)
        {
            Zoom = zoomFactor;

            if (Zoom != 1)
            {
                TimeSpan baseWindow = TotalDuration;
                TimeSpan timeWindow = baseWindow / Zoom;

                double relativePos = (zoomPosition - ViewStartTime) / ViewDuration;

                ViewStartTime = zoomPosition- timeWindow * relativePos;
                ViewEndTime = ViewStartTime + timeWindow;

                // New view start time to keep zoomPosition at the same relative position
                ViewStartTime = ViewStartTime.Clamp(TimeSpan.Zero, TotalDuration);
                ViewEndTime = ViewEndTime.Clamp(TimeSpan.Zero, TotalDuration);
            }
            else
            {
                ViewStartTime = TimeSpan.Zero;
                ViewEndTime = TotalDuration;
            }

            OnLoopChange?.Invoke(LoopStart, LoopEnd);
        }

        private double oldScrollValue = 0;

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

        public int CalculateBpm(int beatsPerMeasure)
        {
           

            var markers = Project.BeatMarkers;
            if (markers.Count < 2)
                return 0;

            var bpmList = new List<double>();

            for (int i = 1; i < markers.Count; i++)
            {
                var seconds = (markers[i].Position - markers[i - 1].Position).TotalSeconds;
                double bpm = (60.0 * beatsPerMeasure) / seconds;
                bpmList.Add(bpm);
            }

            return (int)Math.Round(bpmList.Average());

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

            var old = CurrentPosition;
            var oldPlaybackState = waveOut.PlaybackState;

            if (waveOut.PlaybackState == PlaybackState.Playing)
                Pause();

            ////hm this keeps palyhead in sync when slowdown while play
            AudioJumpToSec(TimeSpan.FromSeconds(CurrentPosition.TotalMilliseconds + 1));

            Project.SetTempo(Math.Round(Speed, 2));

            AudioJumpToSec(old);

            if (oldPlaybackState == PlaybackState.Playing)
                Play();
        }

        public void ChangePitch(int semitones)
        {
            var pitch = Math.Pow(2.0, semitones / 12.0);

            foreach (var track in Project.Tracks)
                track.rubberBandWaveStream.SetPitch(pitch);  
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

          var track = Project.Tracks.Where(t => t.ID == trackID).FirstOrDefault();

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

        public void MuteAllTracks()
        {
            if(Project.IsAllTracksMuted == false)
            {
                foreach (var track in Project.Tracks)
                {
                    track.SetVolumte(0);
                    Project.IsAllTracksMuted = true;
                }
            }
            else
            {
                foreach (var track in Project.Tracks)
                {
                    track.SetVolumte(1);
                    Project.IsAllTracksMuted = false;
                }
            }
        }


        public void AudioJumpToSec(TimeSpan sec)
        {

            if (waveOut == null)
                return;

             waveOut.Stop();
         
          
           // Project.ResetTracks();
            CurrentPosition = sec;

            sw.Reset();
            InitStart = sec;

            Project.JumpToSeconds(sec);
            
            LastUpdateTime = TimeSpan.Zero;
            AccumulatedProgress = TimeSpan.Zero;
        }

        //public void TurnLoopAround(TimeSpan sec)
        //{
        //    waveOut.Stop();

           
        //    // Project.ResetTracks();
        //    CurrentPosition = sec;

        //    sw.Reset();
        //    InitStart = sec;

        //    Project.JumpToSeconds(sec);
            
        //    LastUpdateTime = TimeSpan.Zero;
        //    AccumulatedProgress = TimeSpan.Zero;
        //    waveOut.Play();
        //}


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
            OnLoopChange?.Invoke(LoopStart, LoopEnd);

            // AudioJumpToSec(startSec);
            // Play();
        }

        private void CalculateProgress()
        {
            TimeSpan now = sw.Elapsed;
            TimeSpan deltaRealTime = now - LastUpdateTime;

            AccumulatedProgress += deltaRealTime * Speed;
            LastUpdateTime = now;
            CurrentPosition = InitStart + AccumulatedProgress;
        }

        private void UpdateLoop()
        {
            bool isLoop = LoopEnd.TotalSeconds - LoopStart.TotalSeconds > 0;
            if (isLoop)
            {
                //when reach end of Loop, go back to loopStart
                if (CurrentPosition >= LoopEnd)
                {
                    AudioJumpToSec(LoopStart);
                    Play();
                }

                //if zoom OR LoopStartSelection change
                if (m_oldZoom != Zoom || LoopStart.TotalSeconds != m_oldLoopStart.TotalSeconds)
                {
                    Debug.WriteLine("AudioEngine.UpdateLoop INVOKE");
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

     
    }
}