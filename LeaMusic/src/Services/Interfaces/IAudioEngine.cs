using LeaMusic.src.AudioEngine_;

namespace LeaMusic.src.Services.Interfaces
{
    public interface IAudioEngine
    {
        public TimeSpan LoopStart { get;  }

        public TimeSpan LoopEnd { get;  }

        public TimeSpan CurrentPosition { get; }

        public TimeSpan TotalDuration { get; }

        public event Action<TimeSpan> OnUpdate;

        public event Action<TimeSpan> OnProgressChange;

        public event Action<TimeSpan, TimeSpan> OnLoopChange;

        public void MountProject(Project project);

        public void Update();

        public void ReloadMixerInputs();

        public void AddBeatMarker(TimeSpan position, string text);

        public void ZoomViewWindow(double zoomFactor, TimeSpan start, TimeSpan end);

        public void ZoomViewWindow(double zoomFactor, TimeSpan zoomPosition);

        public void ZoomViewWindowRelative(double zoomFactor, TimeSpan zoomPosition);

        public void ScrollWaveForm(double scrollFactor);

        public int CalculateBpm(int beatsPerMeasure);

        public void Play();

        public void Pause();

        public void ChangeSpeed(double speed);

        public void ChangePitch(int semitones);

        public void Replay();

        public void Stop();

        public void AudioJumpToSec(TimeSpan sec);

        public void Loop(TimeSpan startSec, TimeSpan endSec);


    }
}
