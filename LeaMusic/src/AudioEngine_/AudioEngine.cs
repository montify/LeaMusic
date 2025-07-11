namespace LeaMusic.Src.AudioEngine_
{
    using System;
    using System.Diagnostics;
    using LeaMusic.Extensions;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;
    using PlaybackState = LeaMusic.src.AudioEngine_.PlaybackState;

    public class AudioEngine
    {
        public Project Project { get; private set; }

        public TimeSpan ViewStartTime { get; private set; }

        public TimeSpan ViewEndTime { get; private set; }

        public TimeSpan ViewDuration { get; private set; }

        public TimeSpan InitStart { get; private set; }

        public TimeSpan AccumulatedProgress { get; private set; }

        public TimeSpan LastUpdateTime { get; private set; }

        public TimeSpan CurrentPosition { get; set; }

        public TimeSpan TotalDuration { get; set; }

        public TimeSpan LoopStart { get; private set; }

        public TimeSpan LoopEnd { get; private set; }

        public TimeSpan HalfViewWindow { get; private set; }

        // private MixingSampleProvider m_mixer;
        private readonly IMixer m_mixer;

        private IAudioPlayer m_audioPlayer;
        private Stopwatch m_sw = new Stopwatch();
        private TimeSpan m_oldPosition;
        private TimeSpan m_oldLoopStart = TimeSpan.FromSeconds(-1);
        private double m_speed = 1;

        private double m_zoom = 1;

        private double m_oldZoom = -1;

        public event Action<TimeSpan> OnUpdate;

        public event Action<TimeSpan> OnProgressChange;

        public event Action<TimeSpan, TimeSpan> OnLoopChange;

        public AudioEngine(IAudioPlayer audioPlayer, IMixer mixer)
        {
            m_audioPlayer = audioPlayer;
            m_mixer = mixer;
        }

        public void MountProject(Project project)
        {
            m_zoom = 1;

            Project = project;

            TotalDuration = TimeSpan.FromSeconds(Project.Duration.TotalSeconds);

            ReloadMixerInputs();

            ViewStartTime = TimeSpan.Zero;
            ViewEndTime = TotalDuration;

            try
            {
                m_audioPlayer.Init(m_mixer);
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

            // If CurrentPosition moves
            if (CurrentPosition != m_oldPosition)
            {
                OnProgressChange?.Invoke(CurrentPosition);
                m_oldPosition = CurrentPosition;
            }
        }

        public void ReloadMixerInputs()
        {
            m_mixer.RemoveAllMixerInputs();

            foreach (var track in Project.Tracks)
            {
                m_mixer.AddMixerInput(track);
            }
        }

        public void AddMarker(TimeSpan position, string text)
        {
            var m = new BeatMarker(position, text);
            Project.AddBeatMarker(m);
        }

        public void ZoomViewWindow(double zoomFactor, TimeSpan start, TimeSpan end)
        {
            m_zoom = zoomFactor;
            ViewStartTime = start;
            ViewEndTime = end;

            ViewStartTime = TimeSpan.FromSeconds(Math.Max(0, ViewStartTime.TotalSeconds));
            ViewEndTime = TimeSpan.FromSeconds(Math.Min(TotalDuration.TotalSeconds, ViewEndTime.TotalSeconds));

            OnLoopChange?.Invoke(LoopStart, LoopEnd);
        }

        public void ZoomViewWindow(double zoomFactor, TimeSpan zoomPosition)
        {
            m_zoom = zoomFactor;

            if (m_zoom != 1)
            {
                TimeSpan baseWindow = TotalDuration;
                TimeSpan timeWindow = baseWindow / zoomFactor;
                HalfViewWindow = timeWindow / 2.0f;

                ViewStartTime = zoomPosition - HalfViewWindow;
                ViewEndTime = zoomPosition + HalfViewWindow;

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

        // Used for Zoom with mouse
        public void ZoomViewWindowRelative(double zoomFactor, TimeSpan zoomPosition)
        {
            m_zoom = zoomFactor;

            if (m_zoom != 1)
            {
                TimeSpan baseWindow = TotalDuration;
                TimeSpan timeWindow = baseWindow / m_zoom;

                double relativePos = (zoomPosition - ViewStartTime) / ViewDuration;

                ViewStartTime = zoomPosition - (timeWindow * relativePos);
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

        private double m_oldScrollValue = 0;

        public void ScrollWaveForm(double scrollFactor)
        {
            double diff = scrollFactor - m_oldScrollValue;

            // NOTE: When add Slider value direct, it never goes back to the origin Value, use difference :D
            ViewStartTime += TimeSpan.FromSeconds(diff);
            ViewEndTime += TimeSpan.FromSeconds(diff);

            var start = TimeSpan.FromSeconds(Math.Max(ViewStartTime.TotalSeconds, 0));
            var end = TimeSpan.FromSeconds(Math.Min(ViewEndTime.TotalSeconds, TotalDuration.TotalSeconds));

            ViewStartTime = start;
            ViewEndTime = end;

            UpdateLoop();

            OnLoopChange?.Invoke(LoopStart, LoopEnd);

            m_oldScrollValue = scrollFactor;
        }

        public int CalculateBpm(int beatsPerMeasure)
        {
            var markers = Project.BeatMarkers;
            if (markers.Count < 2)
            {
                return 0;
            }

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
            if (m_audioPlayer.GetAudioPlaybackState() == PlaybackState.Playing)
            {
                m_audioPlayer.Stop();
            }

            m_audioPlayer.Play();
            m_sw.Start();
        }

        public void Pause()
        {
            if (m_audioPlayer.GetAudioPlaybackState() == PlaybackState.Playing)
            {
                m_audioPlayer.Pause();
            }

            m_sw.Stop();
        }

        public void ChangeSpeed(double speed)
        {
            m_speed = speed;

            var old = CurrentPosition;
            var oldPlaybackState = m_audioPlayer.GetAudioPlaybackState();

            if (m_audioPlayer.GetAudioPlaybackState() == PlaybackState.Playing)
            {
                Pause();
            }

            ////hm this keeps palyhead in sync when slowdown while play
            AudioJumpToSec(TimeSpan.FromSeconds(CurrentPosition.TotalMilliseconds + 1));

            Project.SetTempo(Math.Round(m_speed, 2));

            AudioJumpToSec(old);

            if (oldPlaybackState == PlaybackState.Playing)
            {
                Play();
            }
        }

        public void ChangePitch(int semitones)
        {
            var pitch = Math.Pow(2.0, semitones / 12.0);

            foreach (var track in Project.Tracks)
            {
                track.RubberBandWaveStream.SetPitch(pitch);
            }
        }

        public void Replay()
        {
            if (m_audioPlayer.GetAudioPlaybackState() == PlaybackState.Playing)
            {
                AudioJumpToSec(LoopStart);
                Play();
            }
        }

        public void Stop()
        {
            m_audioPlayer.Stop();
        }

        public void MuteTrack(int trackID)
        {
            var track = Project.Tracks.Where(t => t.ID == trackID).FirstOrDefault();

            if (track == null)
            {
                return;
            }

            if (track.IsMuted)
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
            if (Project.IsAllTracksMuted == false)
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
            if (m_audioPlayer == null)
            {
                return;
            }

            m_audioPlayer.Stop();

            CurrentPosition = sec;

            m_sw.Reset();
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
            OnLoopChange?.Invoke(LoopStart, LoopEnd);
        }

        private void CalculateProgress()
        {
            TimeSpan now = m_sw.Elapsed;
            TimeSpan deltaRealTime = now - LastUpdateTime;

            AccumulatedProgress += deltaRealTime * m_speed;
            LastUpdateTime = now;
            CurrentPosition = InitStart + AccumulatedProgress;
        }

        private void UpdateLoop()
        {
            bool isLoop = LoopEnd.TotalSeconds - LoopStart.TotalSeconds > 0;
            if (isLoop)
            {
                // when reach end of Loop, go back to loopStart
                if (CurrentPosition >= LoopEnd)
                {
                    AudioJumpToSec(LoopStart);
                    Play();
                }

                // if zoom OR LoopStartSelection change
                if (m_oldZoom != m_zoom || LoopStart.TotalSeconds != m_oldLoopStart.TotalSeconds)
                {
                    Debug.WriteLine("AudioEngine.UpdateLoop INVOKE");
                    OnLoopChange?.Invoke(LoopStart, LoopEnd);
                    m_oldLoopStart = LoopStart;

                    m_oldZoom = m_zoom;
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