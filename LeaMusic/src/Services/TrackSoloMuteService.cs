namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;

    public class TrackSoloMuteService : ITrackSoloMuteService
    {
        private readonly AudioEngine m_audioEngine;

        public TrackSoloMuteService(AudioEngine audioEngine)
        {
            m_audioEngine = audioEngine;
        }

        public void MuteTrack(int trackId)
        {
            var track = m_audioEngine.Project.Tracks.Where(t => t.ID == trackId).FirstOrDefault();

            if (track == null)
            {
                return;
            }

            if (track.IsMuted)
            {
                track.SetVolumte(1);
                track.IsMuted = false;
            }
            else
            {
                track.SetVolumte(0);
                track.IsMuted = true;
            }
        }

        public void SoloTrack(int trackId)
        {
            var allTracks = m_audioEngine.Project.Tracks;
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
                    track.IsMuted = false;
                    track.SetVolumte(1);
                }
            }
            else
            {
                foreach (var track in allTracks)
                {
                    if (track.ID == trackId)
                    {
                        track.IsSolo = true;
                        track.IsMuted = false;
                        track.SetVolumte(1);
                    }
                    else
                    {
                        track.IsSolo = false;
                        track.IsMuted = true;
                        track.SetVolumte(0);
                    }
                }
            }
        }

        public void MuteAllTracks()
        {
            var project = m_audioEngine.Project;

            if (project.IsAllTracksMuted == false)
            {
                foreach (var track in project.Tracks)
                {
                    track.SetVolumte(0);
                    project.IsAllTracksMuted = true;
                }
            }
            else
            {
                foreach (var track in project.Tracks)
                {
                    track.SetVolumte(1);
                    project.IsAllTracksMuted = false;
                }
            }
        }
    }
}
