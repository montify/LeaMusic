namespace LeaMusic.src.Services
{
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
            var track = m_audioEngine.Project.Tracks.FirstOrDefault(t => t.ID == trackId);
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
            var project = m_audioEngine.Project;

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
            var track = m_audioEngine.Project.Tracks.Where(t => t.ID == trackId).First();

            if (track != null)
            {
                track.SetVolume(volume);
            }
        }
    }
}
