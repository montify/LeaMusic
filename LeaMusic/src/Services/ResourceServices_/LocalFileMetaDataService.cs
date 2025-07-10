using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;

namespace LeaMusic.src.Services.ResourceServices_
{
    public class LocalFileMetaDataService : IMetadataService
    {
        private IProjectSerializer m_ProjectSerializer;

        public LocalFileMetaDataService(IProjectSerializer projectSerializer)
        {
            m_ProjectSerializer = projectSerializer;
        }

        public ProjectMetadata? GetMetaData(Location projectLocation)
        {
            if (projectLocation is FileLocation location)
            {
                if (string.IsNullOrEmpty(location.Path))
                {
                    throw new ArgumentNullException("Path cant be null");
                }

                var file = File.ReadAllText(location.Path);

                var project = m_ProjectSerializer.Deserialize(file);

                if (project == null)
                {
                    throw new NullReferenceException($"Cant load Project Path: {location.Path}");
                }

                if (project.LastSaveAt == null)
                {
                    throw new NullReferenceException("Cant Fetch MetaData from Project");
                }

                var projectMetadata = new ProjectMetadata(project.Name, project.LastSaveAt);

                return projectMetadata;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }
    }
}
