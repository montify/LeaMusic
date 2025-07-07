public record LoopCommand(
        TimeSpan LoopStart,
        TimeSpan LoopEnd,
        TimeSpan JumpToPosition,
        bool ShouldSetLoop,
        bool ShouldJump);

namespace LeaMusic.src.Services
{
    public class LoopService
    {
        public LoopCommand DetermineLoopAction(TimeSpan startSec, TimeSpan endSec)
        {
            // Your existing logic from the first LoopSelection method
            if (startSec >= endSec || endSec - startSec <= TimeSpan.Zero)
            {
                return new LoopCommand(TimeSpan.Zero, TimeSpan.Zero, startSec, ShouldSetLoop: true, ShouldJump: true);
            }
            else if (endSec - startSec < TimeSpan.FromMilliseconds(50))
            {
                return new LoopCommand(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, ShouldSetLoop: false, ShouldJump: false);
            }
            else
            {
                return new LoopCommand(startSec, endSec, startSec, ShouldSetLoop: true, ShouldJump: true);
            }
        }
    }
}
