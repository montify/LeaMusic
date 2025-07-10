namespace LeaMusicGui
{
    using System.Collections.ObjectModel;
    using System.Windows.Media;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using LeaMusic;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusic.src.Services.ResourceServices_;
    using Point = System.Windows.Point;

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private double speed = 1;

        [ObservableProperty]
        private int pitch;

        [ObservableProperty]
        private double scroll;

        [ObservableProperty]
        private double jumpToPositionInSec;

        [ObservableProperty]
        private double playheadPercentage;

        [ObservableProperty]
        private double zoom = 1.0f;

        [ObservableProperty]
        private double sliderZoom = 1.0f;

        [ObservableProperty]
        private double selectionStartPercentage;

        [ObservableProperty]
        private double selectionEndPercentage;

        [ObservableProperty]
        private double progressInPercentage;

        [ObservableProperty]
        private int renderWidth;

        [ObservableProperty]
        private string projectName;

        [ObservableProperty]
        private string statusMessages;

        [ObservableProperty]
        private double currentPlayTime;

        [ObservableProperty]
        private double totalProjectDuration;

        [ObservableProperty]
        private int projectBpm;

        [ObservableProperty]
        private bool isProjectLoading;

        public bool IsLoopBeginDragLeftHandle { get; set; }

        public bool IsLoopBeginDragRightHandle { get; set; }

        public bool IsMarkerMoving { get; set; }

        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } =[];

        public ObservableCollection<MarkerDTO> BeatMarkers { get; set; } =[];

        public bool IsProjectLoaded => Project != null && Project.Duration > TimeSpan.FromSeconds(1);

        private Project Project { get; set; }

        private bool IsSyncEnabled { get; set; } = true;

        private readonly AudioEngine m_audioEngine;
        private readonly TimelineService m_timelineService;
        private readonly IResourceManager m_resourceManager;
        private readonly ProjectService m_projectService;
        private readonly TimelineCalculator m_timelineCalculator;
        private readonly LoopService m_loopService;
        private readonly ConnectionMonitorService m_connectionMonitorService;
        private readonly IDialogService m_dialogService;
        private readonly Action<string> m_updateStatus;

        // currentMarkerID is set when the User click on the marker(Command) in the View,
        // after that OnRightMouseDown() invoke this Method and currentMarkerID is used to find the Marker the User clicked on.
        private int m_currentMarkerID = 0;

        public MainViewModel(
                             ProjectService projectService,
                             IResourceManager resourceManager,
                             TimelineService timelineService,
                             AudioEngine audioEngine,
                             TimelineCalculator timelineCalculator,
                             LoopService loopService,
                             ConnectionMonitorService connectionMonitorService,
                             IDialogService dialogService)
        {
            m_projectService = projectService;
            m_resourceManager = resourceManager;
            m_timelineService = timelineService;
            m_audioEngine = audioEngine;
            m_dialogService = dialogService;
            m_timelineCalculator = timelineCalculator;
            m_loopService = loopService;
            m_connectionMonitorService = connectionMonitorService;
            statusMessages = string.Empty;

            // Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            ProjectName = "NOT SET";

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

        public void UpdateWaveformDTO(double newWidth)
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, (int)newWidth));
            }

            UpdateTrackDTO(trackDTOList);
        }

        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = m_audioEngine.Project.BeatMarkers.Where(marker => marker.ID == m_currentMarkerID).FirstOrDefault();

            if (marker != null)
            {
                marker.Position = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(position.X, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, renderWidth));
            }

            UpdateMarkers();
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

        public void ZoomWaveformMouse(Point p, double width)
        {
            var point = new System.Drawing.Point((int)p.X, (int)p.Y);

            var (newZoomFactor, zoomStartPosition) = m_timelineCalculator.ZoomWaveformMouse(point, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration, Zoom, width);

            Zoom = newZoomFactor;

            // Note: use lowerCase sliderZoom to avoid Trigger OnSliderZoomChange()
            sliderZoom = newZoomFactor;
            OnPropertyChanged(nameof(SliderZoom));

            m_audioEngine.ZoomViewWindowRelative(newZoomFactor, zoomStartPosition);

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

        [RelayCommand]
        public void ZoomFromSlider(double value)
        {
            Zoom = value;

            m_audioEngine.ZoomViewWindow(value, m_audioEngine.CurrentPosition);

            UpdateWaveformDTO(RenderWidth);
        }

        [RelayCommand]
        public void ZoomFromMouse(double value)
        {
            Zoom = value;
        }

        [RelayCommand]
        public void FitLoopToView()
        {
            var (paddedStart, paddedEnd, zoomFactor) = m_timelineCalculator.FitLoopToView(m_audioEngine.LoopStart, m_audioEngine.LoopEnd, m_audioEngine.TotalDuration);

            m_audioEngine.ZoomViewWindow(zoomFactor, paddedStart, paddedEnd);

            var trackDTOList = new List<Memory<float>>();
            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, RenderWidth, paddedStart, paddedEnd));
            }

            UpdateTrackDTO(trackDTOList);
            CreateMarkerDTO();

            Zoom = zoomFactor;
        }

        [RelayCommand]
        public void MarkerClick(MarkerDTO marker)
        {
            m_currentMarkerID = marker.Marker.ID;
        }

        [RelayCommand]
        public void MarkerDelete(MarkerDTO marker)
        {
            m_audioEngine.Project.DeleteMarker(marker.Marker.ID);

            CreateMarkerDTO();
        }

        [RelayCommand]
        public void Replay()
        {
            m_audioEngine.Replay();
        }

        [RelayCommand]
        public void MuteAllTracks()
        {
            m_audioEngine.MuteAllTracks();
        }

        [RelayCommand]
        public async Task SaveProjectFile()
        {
            await SaveProject();
        }

        [RelayCommand]
        public void Pause()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_audioEngine.Pause();
        }

        [RelayCommand]
        public async Task LoadProjectFile()
        {
            await LoadProject();

            StatusMessages = $"Project: {Project.Name} loaded!";
        }

        [RelayCommand]
        public void AddTrack()
        {
            var filePath = m_dialogService.OpenFile("Mp3 (*.mp3)|*.mp3");

            if (!string.IsNullOrEmpty(filePath))
            {
                m_audioEngine.Stop();

                var projectfilePath = filePath;

                var track = m_projectService.ImportTrack(new FileLocation(projectfilePath));

                if (track == null)
                {
                    StatusMessages = "Cant import Track";
                    return;
                }

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
        public void Play()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_audioEngine.Play();
        }

        [RelayCommand]
        public void Mute(object param)
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
        public void Delete(object param)
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
        public void JumpToSec()
        {
            if (TimeSpan.FromSeconds(JumpToPositionInSec) < TimeSpan.Zero)
            {
                StatusMessages = "Please enter a positive Number";
                return;
            }

            m_audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }

        [RelayCommand]
        public void JumpToLoopStart()
        {
            if (m_audioEngine.LoopStart > TimeSpan.Zero)
            {
                m_audioEngine.AudioJumpToSec(m_audioEngine.LoopStart);
                Play();
            }
        }

        [RelayCommand]
        public void ShowGdriveExplorer()
        {
            var projectName = m_dialogService.ShowGDriveExplorer();

            // var gdriveLocation = new GDriveLocation("LeaRoot", projectName, projectName);

            // var gDriveHandler = new GoogleDriveHandler("LeaRoot", null);

            // var project = await m_resourceManager.LoadProject(gdriveLocation, gDriveHandler);

            // TODO: Implement to load from google Drive from Explorer
            throw new NotImplementedException();
        }

        internal void SetTextMarker()
        {
            m_audioEngine.AddMarker(m_audioEngine.CurrentPosition, "B");
            CreateMarkerDTO();
        }

        internal void ResetZoomParameter()
        {
            m_timelineCalculator.ResetZoomParameter();
        }

        private async Task SaveProject()
        {
            await m_projectService.SaveProject(Project, m_updateStatus);
        }

        private async Task LoadProject()
        {
            Project? project = null;

            project = await m_projectService.LoadProjectAsync(isGoogleDriveSync: IsSyncEnabled, m_updateStatus);

            if (project == null)
            {
                return;
            }

            Project?.Dispose();

            Project = project;

            ProjectName = Project.Name;

            m_audioEngine.MountProject(Project);
            m_audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

            CreateTrackDTO();
            CreateMarkerDTO();

            // Prevent when user doubleclick, that WPF register as a mouseclick
            await Task.Delay(100);
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
                thresholdInMs: AppConstants.SnappingTreshholdInMs);
            }

            if (proposedEnd != null)
            {
                currentLoopEnd = SnappingService.SnapToMarkers(
                currentLoopEnd,
                m_audioEngine.Project.BeatMarkers,
                m_audioEngine.ViewStartTime,
                m_audioEngine.ViewDuration,
                RenderWidth,
                thresholdInMs: AppConstants.SnappingTreshholdInMs);
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

        // Maybe Create if the Marker Count > WrapperCount?!
        private void CreateMarkerDTO()
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

            // scroll view when Playhead reach the end of the view
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

        partial void OnSliderZoomChanged(double value)
        {
            Zoom = value;

            if (value != -1)
            {
                m_audioEngine.ZoomViewWindow(value, m_audioEngine.CurrentPosition);
                UpdateWaveformDTO(RenderWidth);
            }
        }
    }
}