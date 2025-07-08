namespace LeaMusicGui
{
    using System.Collections.ObjectModel;
    using System.Windows.Media;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.ResourceManager_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using Point = System.Windows.Point;

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        public double speed = 1;

        [ObservableProperty]
        public int pitch;

        [ObservableProperty]
        public double scroll;

        [ObservableProperty]
        public double jumpToPositionInSec;

        [ObservableProperty]
        public double playheadPercentage;

        [ObservableProperty]
        public double zoom = 1.0f;

        [ObservableProperty]
        public double sliderZoom = 1.0f;

        [ObservableProperty]
        public double selectionStartPercentage;

        [ObservableProperty]
        public double selectionEndPercentage;

        [ObservableProperty]
        public double progressInPercentage;

        [ObservableProperty]
        public int renderWidth;

        [ObservableProperty]
        public string projectName;

        [ObservableProperty]
        public string statusMessages;

        [ObservableProperty]
        public double currentPlayTime;

        [ObservableProperty]
        public double totalProjectDuration;

        [ObservableProperty]
        public int projectBpm;

        [ObservableProperty]
        public bool isProjectLoading;

        public bool IsLoopBeginDragLeftHandle { get; set; }

        public bool IsLoopBeginDragRightHandle { get; set; }

        public bool IsMarkerMoving { get; set; }

        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();

        public ObservableCollection<MarkerDTO> BeatMarkers { get; set; } = new ObservableCollection<MarkerDTO>();

        public bool IsProjectLoaded => Project != null && Project.Duration > TimeSpan.FromSeconds(1);

        private Project Project { get; set; }

        private bool IsSyncEnabled { get; set; } = true;

        private AudioEngine m_audioEngine;
        private TimelineService m_timelineService;
        private LeaResourceManager m_resourceManager;
        private ProjectService m_projectService;
        private TimelineCalculator m_timelineCalculator;
        private LoopService m_loopService;
        private IResourceHandler m_resourceHandler;
        private IDialogService m_dialogService;
        private Action<string> m_updateStatus;

        public MainViewModel(ProjectService projectService,
                             LeaResourceManager resourceManager,
                             TimelineService timelineService,
                             AudioEngine audioEngine,
                             TimelineCalculator timelineCalculator,
                             LoopService loopService,
                             IDialogService dialogService)
        {
            m_projectService = projectService;
            m_resourceManager = resourceManager;
            m_timelineService = timelineService;
            m_audioEngine = audioEngine;
            m_dialogService = dialogService;
            m_timelineCalculator = timelineCalculator;
            m_loopService = loopService;

            statusMessages = "";

            // Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            ProjectName = "NOT SET";

            m_resourceHandler = new FileHandler();

            audioEngine.MountProject(Project);

            audioEngine.OnUpdate += AudioEngine_OnPlayHeadChange;
            audioEngine.OnProgressChange += AudioEngine_OnProgressChange;
            audioEngine.OnLoopChange += AudioEngine_OnLoopChange;

            CompositionTarget.Rendering += (sender, e) => audioEngine.Update();

            m_updateStatus = (message) =>
            {
                StatusMessages = message;
            };
        }

        private async Task SaveProject()
        {
            await m_projectService.SaveProject(Project, m_updateStatus);
        }

        private async Task LoadProject()
        {
            IsSyncEnabled = true;

            var project = await m_projectService.LoadProjectAsync(isGoogleDriveSync: IsSyncEnabled, m_updateStatus);

            if (project == null)
            {
                StatusMessages = "Cant Load Project";
                return;
            }

            Project?.Dispose();

            Project = project;

            ProjectName = Project.Name;

            m_audioEngine.MountProject(Project);
            m_audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

            CreateTrackDTO();
            CreateMarkerDTO();

            //Prevent when user doubleclick, that WPF register as a mouseclick
            await Task.Delay(100);
        }

        public void UpdateWaveformDTO(double newWidth)
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, (int)newWidth));
            }

            UpdateTrackDTO(trackDTOList);
        }

        private void UpdateMarkers()
        {
            for (int i = 0; i < BeatMarkers.Count; i++)
            {
                if (IsMarkerVisible(BeatMarkers[i]))
                {
                    BeatMarkers[i].Visible = true;
                    BeatMarkers[i].PositionRelativeView = m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(BeatMarkers[i].Marker.Position, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration);
                }
                else
                {
                    BeatMarkers[i].Visible = false;
                }
            }

            ProjectBpm = m_audioEngine.CalculateBpm(beatsPerMeasure: 4);
        }

        // currentMarkerID is set when the User click on the marker(Command) in the View,
        // after that OnRightMouseDown() invoke this Method and currentMarkerID is used to find the Marker the User clicked on.
        private int m_currentMarkerID = 0;

        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = m_audioEngine.Project.BeatMarkers.Where(marker => marker.ID == m_currentMarkerID).FirstOrDefault();

            if (marker != null)
            {
                marker.Position = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(position.X, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, renderWidth));
            }

            UpdateMarkers();
        }

        private void UpdateTrackDTO(List<Memory<float>> waveforms)
        {
            for (int i = 0; i < waveforms.Count; i++)
            {
                if (WaveformWrappers.Count < waveforms.Count)
                {
                    WaveformWrappers.Add(new TrackDTO());
                }

                WaveformWrappers[i].Waveform = waveforms[i];
                WaveformWrappers[i].Name = m_audioEngine.Project.Tracks[i].Name ?? "No Name Set";
                WaveformWrappers[i].TrackID = m_audioEngine.Project.Tracks[i].ID;
            }
        }

        private void CreateTrackDTO()
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, 1200));
            }

            UpdateTrackDTO(trackDTOList);
        }

        //Maybe Create if the Marker Count > WrapperCount?!
        internal void CreateMarkerDTO()
        {
            BeatMarkers.Clear();
            for (int i = 0; i < m_audioEngine.Project.BeatMarkers.Count; i++)
            {
                var marker = m_audioEngine.Project.BeatMarkers[i];

                var w = new MarkerDTO();
                w.Marker = marker;
                w.PositionRelativeView = m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(marker.Position, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration);
                w.Marker.ID = marker.ID;
                w.Marker.Description = marker.Description;
                BeatMarkers.Add(w);
            }
        }

        private bool IsMarkerVisible(MarkerDTO testMarker)
        {
            if (testMarker.Marker.Position < m_audioEngine.ViewStartTime || testMarker.Marker.Position > m_audioEngine.ViewEndTime)
            {
                return false;
            }

            return true;
        }

        private void AudioEngine_OnProgressChange(TimeSpan positionInSec)
        {
            TotalProjectDuration = Project.Duration.TotalSeconds;
            CurrentPlayTime = positionInSec.TotalSeconds;

            ProgressInPercentage = (m_audioEngine.CurrentPosition.TotalSeconds / m_audioEngine.TotalDuration.TotalSeconds) * 100;

            //scroll view when Playhead reach the end of the view
            if (m_audioEngine.CurrentPosition >= m_audioEngine.ViewEndTime)
            {
                m_audioEngine.ZoomViewWindow(Zoom, m_audioEngine.CurrentPosition + m_audioEngine.HalfViewWindow);
                UpdateWaveformDTO(RenderWidth);
            }
        }

        private void AudioEngine_OnLoopChange(TimeSpan startSec, TimeSpan endSec)
        {
            SelectionStartPercentage = m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(startSec, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration);
            SelectionEndPercentage = m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(endSec, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration);
        }

        private void AudioEngine_OnPlayHeadChange(TimeSpan positionInSeconds)
        {
            PlayheadPercentage = m_timelineCalculator.CalculateSecRelativeToViewWindowPercentage(positionInSeconds, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration);
            UpdateMarkers();
        }

        partial void OnPitchChanged(int value)
        {
            m_audioEngine.ChangePitch(value);
        }

        partial void OnProjectNameChanged(string value)
        {
            if (Project != null)
                Project.Name = value;
        }

        partial void OnScrollChanged(double value)
        {
            m_audioEngine.ScrollWaveForm(value);

            UpdateWaveformDTO(RenderWidth);
        }

        partial void OnSpeedChanged(double value)
        {
            m_audioEngine.ChangeSpeed(value);
        }

        partial void OnZoomChanged(double value)
        {
            Zoom = value;
        }

        partial void OnSliderZoomChanged(double value)
        {
            Zoom = value;

            m_audioEngine.ZoomViewWindow(value, m_audioEngine.CurrentPosition);
            UpdateWaveformDTO(RenderWidth);
        }

        public void SetTextMarker()
        {
            m_audioEngine.AddMarker(m_audioEngine.CurrentPosition, "B");
            CreateMarkerDTO();
        }

        public void ResetZoomParameter()
        {
            m_timelineCalculator.ResetZoomParameter();

            //zoomStartPositionSetOnce = false;
        }

        /// <summary>
        /// Performs a smooth zoom operation on the waveform based on mouse movement.
        /// The zoom is anchored at the horizontal position of the ZoomStart Position
        /// </summary>
        /// <param name="p">The current mouse position.</param>
        /// <param name="width">The width of the waveform display in pixels.</param>
        public void ZoomWaveformMouse(Point p, double width)
        {
            var point = new System.Drawing.Point((int)p.X, (int)p.Y);

            var result = m_timelineCalculator.ZoomWaveformMouse(point, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration,  Zoom, width);

            Zoom = result.newZoomFactor;
            SliderZoom = result.newZoomFactor;

            m_audioEngine.ZoomViewWindowRelative(result.newZoomFactor, result.zoomStartPosition);

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

        public void LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(startPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            var endSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(endPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            SetOrAdjustLoop(startSec, endSec);
        }

        public void LoopSelectionStart(double startPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(startPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            SetOrAdjustLoop(startSec, null); // Only proposing a new start
        }

        public void LoopSelectionEnd(double endPixel, double renderWidth)
        {
            var endSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(endPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            SetOrAdjustLoop(null, endSec); // Only proposing a new end
        }

        private void SetOrAdjustLoop(TimeSpan? proposedStart, TimeSpan? proposedEnd)
        {
            // Determine the actual start and end for the loop operation
            // Use existing LoopStart/LoopEnd if not explicitly provided
            TimeSpan currentLoopStart = proposedStart ?? m_audioEngine.LoopStart;
            TimeSpan currentLoopEnd = proposedEnd ?? m_audioEngine.LoopEnd;

            if (proposedStart != null)
            {
                currentLoopStart = SnappingService.SnapToMarkers(
                currentLoopStart,
                m_audioEngine.Project.BeatMarkers,
                m_audioEngine.ViewStartTime,
                m_audioEngine.ViewDuration,
                RenderWidth,
                thresholdInMs: 10);
            }

            if (proposedEnd != null)
            {
                currentLoopEnd = SnappingService.SnapToMarkers(
                currentLoopEnd,
                m_audioEngine.Project.BeatMarkers,
                m_audioEngine.ViewStartTime,
                m_audioEngine.ViewDuration,
                RenderWidth,
                thresholdInMs: 10);
            }

            // Use the LoopPlaybackService to determine the final action
            var loopAction = m_loopService.DetermineLoopAction(currentLoopStart, currentLoopEnd);

            if (!loopAction.shouldSetLoop && !loopAction.shouldJump)
            {
                return; // Service decided to ignore this input
            }

            if (loopAction.shouldSetLoop)
            {
                m_audioEngine.Loop(loopAction.loopStart, loopAction.loopEnd);
            }

            if (loopAction.shouldJump)
            {
                m_audioEngine.AudioJumpToSec(loopAction.jumpToPosition);
            }

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

        [RelayCommand]
        public void ZoomFromMouse(double value)
        {
            SliderZoom = value;
        }

        [RelayCommand]
        public void ZoomFromSlider(double value)
        {
            Zoom = value;

            m_audioEngine.ZoomViewWindow(value, m_audioEngine.CurrentPosition);

            UpdateWaveformDTO(RenderWidth);
        }

        [RelayCommand]
        public void FitLoopToView()
        {
            var result = m_timelineCalculator.FitLoopToView(m_audioEngine.LoopStart, m_audioEngine.LoopEnd, m_audioEngine.TotalDuration);

            m_audioEngine.ZoomViewWindow(result.zoomFactor, result.paddedStart, result.paddedEnd);

            var trackDTOList = new List<Memory<float>>();
            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, RenderWidth, result.paddedStart, result.paddedEnd));
            }

            UpdateTrackDTO(trackDTOList);
            CreateMarkerDTO();

            Zoom = result.zoomFactor;
        }

        [RelayCommand]
        private void MarkerClick(MarkerDTO marker)
        {
            m_currentMarkerID = marker.Marker.ID;
        }

        [RelayCommand]
        private void MarkerDelete(MarkerDTO marker)
        {
            m_audioEngine.Project.DeleteMarker(marker.Marker.ID);

            CreateMarkerDTO();
        }

        [RelayCommand]
        private void Replay()
        {
            m_audioEngine.Replay();
        }

        [RelayCommand]
        private void MuteAllTracks()
        {
            m_audioEngine.MuteAllTracks();
        }

        [RelayCommand]
        private async Task SaveProjectFile()
        {
            m_resourceHandler = new FileHandler();
            await SaveProject();
        }

        [RelayCommand]
        private void Pause()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_audioEngine.Pause();
        }

        [RelayCommand]
        private async Task LoadProjectFile()
        {
            m_resourceHandler = new FileHandler();
            await LoadProject();

            StatusMessages = $"Project: {Project.Name} loaded!";
        }

        [RelayCommand]
        private void AddTrack()
        {
            var filePath = m_dialogService.OpenFile("Mp3 (*.mp3)|*.mp3");

            if (!string.IsNullOrEmpty(filePath))
            {
                m_audioEngine.Stop();

                var projectfilePath = filePath;
                var track = m_resourceManager.ImportTrack(new FileLocation(projectfilePath), m_resourceHandler);

                Project.AddTrack(track);

                if (Project.Tracks.Count == 1)
                {
                    ProjectName = Project.Tracks[0].Name ?? "No Name Set";
                }

                Project.SetTempo(Speed);
                CreateTrackDTO();

                // Check if audioengine is playing while addTrack, when true continue playing
                m_audioEngine.MountProject(Project);
                m_audioEngine.AudioJumpToSec(m_audioEngine.CurrentPosition);
            }
        }

        [RelayCommand]
        private void Play()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_audioEngine.Play();
        }

        [RelayCommand]
        private void Mute(object param)
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            if (param is TrackDTO wrapper)
            {
                m_audioEngine.MuteTrack(wrapper.TrackID);
            }
        }

        [RelayCommand]
        private void Delete(object param)
        {
            if (param is TrackDTO wrapper)
            {
                var track = m_audioEngine.Project.Tracks.Where(t => t.ID == wrapper.TrackID).FirstOrDefault();

                if (track != null)
                {
                    m_audioEngine.Project.Tracks.Remove(track);
                    WaveformWrappers.Remove(wrapper);

                    m_audioEngine.ReloadMixerInputs();

                    StatusMessages = $"Track: {track.Name} deleted!";
                }
            }
        }

        [RelayCommand]
        private void JumpToSec()
        {
            if (TimeSpan.FromSeconds(JumpToPositionInSec) < TimeSpan.Zero)
            {
                StatusMessages = "Please enter a positive Number";
                return;
            }

            m_audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }

        [RelayCommand]
        private void JumpToLoopStart()
        {
            if (m_audioEngine.LoopStart > TimeSpan.Zero)
            {
                m_audioEngine.AudioJumpToSec(m_audioEngine.LoopStart);
                Play();
            }
        }
    }
}