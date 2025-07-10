namespace LeaMusic.src.Services.ResourceServices_
{
    using System.Text.Json;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;

    public class ProjectJsonSerializer : IProjectSerializer
    {
        private readonly JsonSerializerOptions _options;

        public ProjectJsonSerializer()
        {
            _options = new JsonSerializerOptions { WriteIndented = true };
        }

        public string Serialize(Project project)
        {
            return JsonSerializer.Serialize(project, _options);
        }

        public Project Deserialize(string data)
        {
            var project = JsonSerializer.Deserialize<Project>(data);
            if (project == null)
            {
                throw new InvalidDataException("Failed to deserialize project data.");
            }

            return project;
        }
    }
}
