public record LoopCommand(
        TimeSpan loopStart,
        TimeSpan loopEnd,
        TimeSpan jumpToPosition,
        bool shouldSetLoop,
        bool shouldJump);

namespace LeaMusic.src.Services
{
    public class LoopService
    {
        public LoopCommand DetermineLoopAction(TimeSpan startSec, TimeSpan endSec)
        {
            // Your existing logic from the first LoopSelection method
            if (startSec >= endSec || endSec - startSec <= TimeSpan.Zero)
            {
                return new LoopCommand(TimeSpan.Zero, TimeSpan.Zero, startSec, shouldSetLoop: true, shouldJump: true);
            }
            else if (endSec - startSec < TimeSpan.FromMilliseconds(AppConstants.MinimumLoopTimeInMs))
            {
                return new LoopCommand(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, shouldSetLoop: false, shouldJump: false);
            }
            else
            {
                return new LoopCommand(startSec, endSec, startSec, shouldSetLoop: true, shouldJump: true);
            }
        }
    }
}
