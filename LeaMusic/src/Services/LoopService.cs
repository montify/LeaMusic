namespace LeaMusic.src.Services
{
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.AudioEngine_.Interfaces;

    public class LoopService
    {
        private readonly SnappingService m_snappingService;
        private readonly IProjectProvider m_projectProvider;
        private readonly IViewWindowProvider m_viewWindowProvider;
        private readonly AudioEngine m_audioEngine;

        public LoopService(
            SnappingService snappingService,
            IProjectProvider projectProvider, 
            IViewWindowProvider viewWindowProvider,
            AudioEngine audioEngine)
        {
            m_snappingService = snappingService;
            m_projectProvider = projectProvider;
            m_viewWindowProvider = viewWindowProvider;
            m_audioEngine = audioEngine;
        }

        public async Task SetOrAdjustLoop(TimeSpan? proposedStart, TimeSpan? proposedEnd, int renderWidth)
        {
            TimeSpan currentLoopStart = proposedStart ?? m_audioEngine.LoopStart;
            TimeSpan currentLoopEnd = proposedEnd ?? m_audioEngine.LoopEnd;

            if (proposedStart != null)
            {
                currentLoopStart = m_snappingService.SnapToMarkers(
                currentLoopStart,
                m_projectProvider.Project.BeatMarkers,
                m_viewWindowProvider.ViewStartTime,
                m_viewWindowProvider.ViewDuration,
                renderWidth,
                thresholdInMs: AppConstants.SnappingTreshholdInMs);
            }

            if (proposedEnd != null)
            {
                currentLoopEnd = m_snappingService.SnapToMarkers(
                currentLoopEnd,
                m_projectProvider.Project.BeatMarkers,
                m_viewWindowProvider.ViewStartTime,
                m_viewWindowProvider.ViewDuration,
                renderWidth,
                thresholdInMs: AppConstants.SnappingTreshholdInMs);
            }


            var loopAction = DetermineLoopAction(currentLoopStart, currentLoopEnd);

            if (!loopAction.shouldSetLoop && !loopAction.shouldJump)
            {
                return; // Service decided to ignore this input
            }

            if (loopAction.shouldSetLoop)
            {
                m_audioEngine.Loop(loopAction.loopStart, loopAction.loopEnd);
            }

            if (loopAction.shouldJump)
            {
                m_audioEngine.AudioJumpToSec(loopAction.jumpToPosition);
            }
        }

        private LoopCommand DetermineLoopAction(TimeSpan startSec, TimeSpan endSec)
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
