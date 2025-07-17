public record LoopCommand(TimeSpan loopStart, TimeSpan loopEnd, TimeSpan jumpToPosition, bool shouldSetLoop, bool shouldJump);
