namespace LeaMusic.src.Services
{
    using System.Drawing;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.DTOs;
    using LeaMusic.src.Services.Interfaces;

    public class BeatMarkerService : IBeatMarkerService
    {
        private readonly ITimelineCalculator m_timelineCalculator;
        private readonly IProjectProvider m_projectProvider;
        private readonly IViewWindowProvider m_viewWindowProvider;

        private int m_currentMarkerID;

        public BeatMarkerService(
            ITimelineCalculator timelineCalculator,
            IProjectProvider projectProvider,
            IViewWindowProvider viewWindowProvider
        )
        {
            m_timelineCalculator = timelineCalculator;
            m_projectProvider = projectProvider;
            m_viewWindowProvider = viewWindowProvider;
        }

        public IEnumerable<BeatMarkerDTO> UpdateMarkersVisibility()
        {
            var beatMarkers = m_projectProvider.Project.BeatMarkers;

            var beatMarkerDTO = new List<BeatMarkerDTO>();

            for (int i = 0; i < beatMarkers.Count; i++)
            {
                var dto = new BeatMarkerDTO();
                dto.Id = beatMarkers[i].ID;
                dto.PositionRelativeView =
                    m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(
                        beatMarkers[i].Position,
                        m_viewWindowProvider.ViewStartTime,
                        m_viewWindowProvider.ViewDuration
                    );
                dto.Visible = IsMarkerVisible(beatMarkers[i]);
                dto.Description = beatMarkers[i].Description;

                beatMarkerDTO.Add(dto);
            }

            return beatMarkerDTO;
        }

        public void MarkerDelete(int markerId)
        {
            m_projectProvider.Project.DeleteMarker(markerId);
        }

        public void MarkerClick(int markerId)
        {
            m_currentMarkerID = markerId;
        }

        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = m_projectProvider.Project.BeatMarkers.FirstOrDefault(marker =>
                marker.ID == m_currentMarkerID
            );

            if (marker != null)
            {
                marker.Position = TimeSpan.FromSeconds(
                    m_timelineCalculator.ConvertPixelToSecond(
                        position.X,
                        m_viewWindowProvider.ViewStartTime.TotalSeconds,
                        m_viewWindowProvider.ViewDuration.TotalSeconds,
                        renderWidth
                    )
                );
            }
        }

        private bool IsMarkerVisible(BeatMarker beatmarker)
        {
            if (
                beatmarker.Position < m_viewWindowProvider.ViewStartTime
                || beatmarker.Position > m_viewWindowProvider.ViewEndTime
            )
            {
                return false;
            }

            return true;
        }
    }
}
