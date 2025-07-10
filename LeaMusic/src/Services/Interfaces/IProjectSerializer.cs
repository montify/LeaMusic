namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;

    public interface IProjectSerializer
    {
        string Serialize(Project project);

        Project Deserialize(string data);
    }
}
