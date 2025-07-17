namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;

    public class TrackVolumeService : ITrackVolumeService
    {
        private readonly IProjectProvider m_projectProvider;

        public TrackVolumeService(IProjectProvider projectProvider)
        {
            m_projectProvider = projectProvider;
        }

        public void MuteTrack(int trackId)
        {
            var track = m_projectProvider.Project.Tracks.FirstOrDefault(t => t.ID == trackId);
            if (track == null)
            {
                return;
            }

            if (track.IsMuted)
            {
                track.Unmute();
            }
            else
            {
                track.Mute();
            }
        }

        public void SoloTrack(int trackId)
        {
            var allTracks = m_projectProvider.Project.Tracks;
            var soloTrack = allTracks.FirstOrDefault(t => t.ID == trackId);
            if (soloTrack == null)
            {
                return;
            }

            bool isAlreadySolo = soloTrack.IsSolo;

            if (isAlreadySolo)
            {
                foreach (var track in allTracks)
                {
                    track.IsSolo = false;
                    track.Unmute();
                }
            }
            else
            {
                foreach (var track in allTracks)
                {
                    if (track.ID == trackId)
                    {
                        track.IsSolo = true;
                        track.Unmute();
                    }
                    else
                    {
                        track.IsSolo = false;
                        track.Mute();
                    }
                }
            }
        }

        public void MuteAllTracks()
        {
            var project = m_projectProvider.Project;

            if (project.IsAllTracksMuted == false)
            {
                foreach (var track in project.Tracks)
                {
                    track.SetVolume(0);
                    project.IsAllTracksMuted = true;
                }
            }
            else
            {
                foreach (var track in project.Tracks)
                {
                    track.SetVolume(1);
                    project.IsAllTracksMuted = false;
                }
            }
        }

        public void SetTrackVolume(int trackId, float volume)
        {
            var track = m_projectProvider.Project.Tracks.Where(t => t.ID == trackId).First();

            if (track != null)
            {
                track.SetVolume(volume);
            }
        }
    }
}
