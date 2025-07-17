namespace LeaMusic.src.AudioEngine_.Interfaces
{
    public interface IViewWindowProvider
    {
        public TimeSpan ViewStartTime { get; }

        public TimeSpan ViewEndTime { get; }

        public TimeSpan ViewDuration { get; }

        public TimeSpan HalfViewWindow { get; }
    }
}
