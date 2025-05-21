using NAudio.Wave;
using System.Diagnostics;

namespace LeaMusic.src.AudioEngine_
{

    //Project is Data, ProjectManager is the Controller of it 
    public class Project : IDisposable
    {
        public string Name { get; set; }
        public string ProjectPath { get; set; }
        public List<Track> Tracks { get; set; }
        public WaveFormat WaveFormat { get; set; }
        public TimeSpan Duration { get; set; }

        public List<Marker> BeatMarkers { get; set; } = new List<Marker>();


        public Project(string name)
        {
            Name = name;
            Tracks = new List<Track>();
        }

        public static Project CreateEmptyProject(string name)
        {
            var project = new Project(name);
            project.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            project.Duration = TimeSpan.FromSeconds(1);

                
           return project;
        }

        public Project()
        {
            
        }
 
        public void AddTrack(Track track)
        {
            if (track == null || track.ClipDuration == null)
            {
                throw new Exception("");
            }

            if(WaveFormat == null)
            {
                WaveFormat = track.Waveformat;
                Duration = track.ClipDuration;
            }

            //Empty Project have  Duration of 1, set the Duration based on the first AudioClip that is imported
            if (Tracks.Count == 0)
            {
                Duration = track.ClipDuration;
            }
               

            if(track.ClipDuration != Duration)
                throw new Exception("Track Length/Duration must be the same for all Tracks");

            if (track.Waveformat.SampleRate != WaveFormat.SampleRate)
                throw new Exception("Waveformat must be the same for all Tracks");

            Debug.WriteLine("Track ADDED");

           
            Tracks.Add(track);
        }

        public void SetTempo(double speed)
        {
            foreach (var track in Tracks)
            {
                track.rubberBandWaveStream.Tempo = speed;
            }
        }
       
        public void ResetTracks()
        {
            foreach (var track in Tracks)
            {
                track.rubberBandWaveStream.Reset();
               
            }
        }

        public void JumpToSeconds(TimeSpan position)
        {
            foreach (var track in Tracks)
            {
                track.loopStream.JumpToSeconds(position.TotalSeconds);
            }
        }

        public Memory<float> RequestSample(int trackId, double viewStartTimeSec, double viewEndTimeSec, int renderWidth)
        {
            if (Tracks.Count == 0)
                return default;

            if (trackId > Tracks.Count)
                throw new Exception("TrackID is to big");

            return Tracks[trackId].waveformProvider.RequestSamples(viewStartTimeSec, viewEndTimeSec, renderWidth);

        }

        public void AddMarker(Marker marker)
        {
            BeatMarkers.Add(marker);
        }

        public void Dispose()
        {
            foreach (var track in Tracks)
            {
                track.Dispose();
            }
        }
    }
}