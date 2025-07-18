using LeaMusic.src.DTOs;
using System.Drawing;

namespace LeaMusic.src.Services.Interfaces
{
    public interface IBeatMarkerService
    {
        public IEnumerable<BeatMarkerDTO> UpdateMarkersVisibility();

        public void MarkerDelete(int markerId);

        public void MarkerClick(int markerId);

        public void MoveMarker(Point position, int renderWidth);

    }
}
