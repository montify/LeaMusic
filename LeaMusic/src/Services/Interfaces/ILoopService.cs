namespace LeaMusic.src.Services.Interfaces
{
    public interface ILoopService
    {
        public Task SetOrAdjustLoop(int startPixel, int endPixel, int renderWidth);

    }
}
