namespace LeaMusic.src
{
    public class Marker
    {
        public TimeSpan Position { get; set; }
        public string Description { get; set; }

        public Marker(TimeSpan position, string description)
        {
            Position = position;
            Description = description;
        }

    }
}
