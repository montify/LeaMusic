namespace LeaMusic.Src.AudioEngine_
{
    // TODO: Maybe make this abstract, so i can have Beatmarkers, Textmarkers,...
    public class BeatMarker
    {
        private static int m_uniqueId = 0;

        public int ID { get; set; }

        public TimeSpan Position { get; set; }

        public string Description { get; set; }

        public BeatMarker(TimeSpan position, string description)
        {
            Position = position;
            Description = description;
            ID = ++m_uniqueId;
        }
    }
}
