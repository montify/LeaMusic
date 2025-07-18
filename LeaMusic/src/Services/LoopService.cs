namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;

    public class LoopService : ILoopService
    {
        private readonly ISnappingService m_snappingService;
        private readonly IProjectProvider m_projectProvider;
        private readonly IViewWindowProvider m_viewWindowProvider;
        private readonly IAudioEngine m_audioEngine;
        private readonly ITimelineCalculator m_timelineCalculator;

        public LoopService(
            ISnappingService snappingService,
            IProjectProvider projectProvider,
            IViewWindowProvider viewWindowProvider,
            IAudioEngine audioEngine,
            ITimelineCalculator timelineCalculator)
        {
            m_snappingService = snappingService;
            m_projectProvider = projectProvider;
            m_viewWindowProvider = viewWindowProvider;
            m_audioEngine = audioEngine;
            m_timelineCalculator = timelineCalculator;
        }

        public async Task SetOrAdjustLoop(int startPixel, int endPixel, int renderWidth)
        {
            TimeSpan? proposedStart = startPixel != 0
                    ? TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(startPixel, m_viewWindowProvider.ViewStartTime.TotalSeconds, m_viewWindowProvider.ViewDuration.TotalSeconds, renderWidth))
                    : null;

            TimeSpan? proposedEnd = endPixel != 0
                    ? TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(endPixel, m_viewWindowProvider.ViewStartTime.TotalSeconds, m_viewWindowProvider.ViewDuration.TotalSeconds, renderWidth))
                    : null;

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
                return;
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
