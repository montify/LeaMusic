namespace LeaMusic.src.AudioEngine_
{
    //TODO: Maybe make this abstract, so i can have Beatmarkers, Textmarkers,...
    public class Marker
    {
        public static int UNIQUE_ID = 0;
        public int ID { get; set; }
        public TimeSpan Position { get; set; }
        public string Description { get; set; }

        public Marker(TimeSpan position, string description)
        {
            Position = position;
            Description = description;
            ID = ++UNIQUE_ID;
        }
    }
}
