namespace LeaMusic.src.AudioEngine_.Interfaces
{
    public interface IViewWindowProvider
    {
        public TimeSpan ViewStartTime { get; }

        public TimeSpan ViewEndTime { get; }

        public TimeSpan ViewDuration { get; }

        public TimeSpan HalfViewWindow { get; }

        public TimeSpan TotalDuration { get; }

        public double Zoom { get; }
    }
}
