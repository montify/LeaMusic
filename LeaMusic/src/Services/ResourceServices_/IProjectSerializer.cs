namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;

    public interface IProjectSerializer
    {
        string Serialize(Project project);

        Project Deserialize(string data);
    }
}
