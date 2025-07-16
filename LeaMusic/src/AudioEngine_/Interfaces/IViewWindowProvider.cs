namespace LeaMusic.src.AudioEngine_.Interfaces
{
    public interface IViewWindowProvider
    {
        public TimeSpan ViewStartTime { get; }
        public TimeSpan ViewEndTime { get; }
    }
}
