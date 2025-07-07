namespace LeaMusic.src.AudioEngine_
{
    //TODO: Maybe make this abstract, so i can have Beatmarkers, Textmarkers,...
    public class BeatMarker
    {
        public static int UNIQUE_ID = 0;
        public int ID { get; set; }
        public TimeSpan Position { get; set; }
        public string Description { get; set; }

        public BeatMarker(TimeSpan position, string description)
        {
            Position = position;
            Description = description;
            ID = ++UNIQUE_ID;
        }
    }
}
