namespace LeaMusic.src.Services.Interfaces
{
    public interface ILoopService
    {
        public Task SetOrAdjustLoop(TimeSpan? proposedStart, TimeSpan? proposedEnd, int renderWidth);

    }
}
