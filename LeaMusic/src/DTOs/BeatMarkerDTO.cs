namespace LeaMusic.src.DTOs
{
    using LeaMusic.Src.AudioEngine_;

    public class BeatMarkerDTO
    {
        public int Id { get; set; }

        public double PositionRelativeView { get; set; }

        public BeatMarker Marker { get; set; }

        public bool Visible { get; set; }

        public string Description { get; set; }
    }
}
