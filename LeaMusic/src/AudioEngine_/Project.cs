using LeaMusic.Src.AudioEngine_;
using NAudio.Wave;

namespace LeaMusic.src.AudioEngine_
{
    public class Project : IDisposable
    {
        public string Name { get; set; }

        public List<Track> Tracks { get; set; }

        public WaveFormat? WaveFormat { get; set; }

        public TimeSpan Duration { get; set; }

        public List<BeatMarker> BeatMarkers { get; set; } = new List<BeatMarker>();

        public DateTime LastSaveAt { get; set; }

        public bool IsAllTracksMuted { get; set; }

        public Project(string name)
        {
            Name = name;
            Tracks = new List<Track>();
            LastSaveAt = default(DateTime);
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

        /// <summary>
        /// Adds a new audio track to the project
        /// The track must match the project's duration and sample rate.
        /// If this is the first track, it sets the project's wave format and duration.
        /// </summary>
        /// <param name="track">The audio track to be added.</param>
        /// <exception cref="Exception">
        /// Thrown if the track is null, has no duration, or if its duration or sample rate
        /// does not match the existing project's settings.
        /// </exception>
        public void AddTrack(Track track)
        {
            if (track == null || track.ClipDuration == TimeSpan.Zero)
            {
                throw new ArgumentNullException("Track cant be null");
            }

            if (WaveFormat == null)
            {
                WaveFormat = track.Waveformat;
                Duration = track.ClipDuration;
            }

            // Empty Project have  Duration of 1, set the Duration based on the first AudioClip that is imported
            if (Tracks.Count == 0)
            {
                Duration = track.ClipDuration;
            }

            if (track.ClipDuration != Duration)
            {
                throw new Exception("Track Length/Duration must be the same for all Tracks");
            }

            if (track.Waveformat.SampleRate != WaveFormat.SampleRate)
            {
                throw new Exception("Waveformat must be the same for all Tracks");
            }

            Tracks.Add(track);
        }

        /// <summary>
        /// Sets the playback tempo for the entire project by applying the specified speed factor to all tracks.
        /// </summary>
        /// <param name="speed">
        /// The tempo multiplier to apply.
        /// A value of 1.0 means normal speed, values greater than 1.0 increase the tempo, and values less than 1.0 decrease it.
        /// </param>
        public void SetTempo(double speed)
        {
            foreach (var track in Tracks)
            {
                track.RubberBandWaveStream.Tempo = speed;
            }
        }

        /// <summary>
        /// Resets the internal buffers of all track streams.
        /// Typically used after changing the audio position, after you call <see cref="JumpToSeconds(TimeSpan)"/>.
        /// </summary>
        public void ResetTracks()
        {
            foreach (var track in Tracks)
            {
                track.RubberBandWaveStream.Reset();
            }
        }

        /// <summary>
        /// Jumps all tracks to a specific position in time.
        /// </summary>
        /// <param name="position">The target time position to jump to.</param>
        public void JumpToSeconds(TimeSpan position)
        {
            var offset = TimeSpan.FromMilliseconds(AppConstants.AudioOffsetInMs);

            foreach (var track in Tracks)
            {
                track.RubberBandWaveStream.SeekTo(position - offset);
            }

            // LeaLog.Instance.LogInfoAsync($"Project: JumpToSeconds: {position}");
        }

        /// <summary>
        /// Requests Waveform Samples from a specific track within a specific visible Timerange.
        /// </summary>
        /// <param name="trackId">The ID of the track for which to request waveform data.</param>
        /// <param name="viewStartTimeSec">The start time (in seconds) of the visible waveform range.</param>
        /// <param name="viewEndTimeSec">The end time (in seconds) of the visible waveform range.</param>
        /// <param name="renderWidth">The width (in pixels) of the rendered waveform image.</param>
        /// <returns>A Memory view of audio samples in the range 0.0–1.0, with a size corresponding to <paramref name="renderWidth"/>.</returns>
        /// <exception cref="Exception"></exception>
        public Memory<float> RequestSample(int trackId, double viewStartTimeSec, double viewEndTimeSec, int renderWidth)
        {
            if (Tracks.Count == 0)
            {
                return default;
            }

            if (trackId > Tracks.Count)
            {
                throw new Exception("TrackID is to big");
            }

            return Tracks[trackId].WaveformProvider.RequestSamples(viewStartTimeSec, viewEndTimeSec, renderWidth);
        }

        /// <summary>
        /// Adds a time marker to the track, which can be used for user orientation or BPM (beats per minute) detection.
        /// </summary>
        /// <param name="marker">The marker to add, representing a specific point in time within the track.</param>
        public void AddBeatMarker(BeatMarker marker)
        {
            BeatMarkers.Add(marker);
        }

        public void DeleteMarker(int id)
        {
            var marker = BeatMarkers.Where(marker => marker.ID == id).FirstOrDefault();

            if (marker != null)
            {
                BeatMarkers.Remove(marker);
            }
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