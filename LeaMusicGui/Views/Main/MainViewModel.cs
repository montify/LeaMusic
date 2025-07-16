namespace LeaMusicGui
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Media;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using LeaMusic;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;
    using LeaMusicGui.Controls.TrackControl_;
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

        public ObservableCollection<MarkerDTO> BeatMarkers { get; set; } =[];

        public ObservableCollection<TrackControlViewModel> Tracks { get; set; }

        public bool IsProjectLoaded => Project != null && Project.Duration > TimeSpan.FromSeconds(1);

        private Project Project { get; set; }

        private bool IsSyncEnabled { get; set; } = true;

        private readonly AudioEngine m_audioEngine;
        private readonly TimelineService m_timelineService;
        private readonly IResourceManager m_resourceManager;
        private readonly ProjectService m_projectService;
        private readonly TimelineCalculator m_timelineCalculator;
        private readonly LoopService m_loopService;
        private readonly IDialogService m_dialogService;
        private readonly ITrackVolumeService m_trackSoloMuteService;
        private readonly SnappingService m_snappingService;

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
                             IDialogService dialogService,
                             ITrackVolumeService trackSoloMuteService,
                             SnappingService snappingService)
        {
            m_projectService = projectService;
            m_resourceManager = resourceManager;
            m_timelineService = timelineService;
            m_audioEngine = audioEngine;
            m_dialogService = dialogService;
            m_timelineCalculator = timelineCalculator;
            m_loopService = loopService;
            m_trackSoloMuteService = trackSoloMuteService;
            m_snappingService = snappingService;
            statusMessages = string.Empty;

            Tracks = new ObservableCollection<TrackControlViewModel>();

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

        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = m_audioEngine.Project.BeatMarkers.Where(marker => marker.ID == m_currentMarkerID).FirstOrDefault();

            if (marker != null)
            {
                marker.Position = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(position.X, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, renderWidth));
            }

            UpdateMarkers();
        }

        public async Task LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(startPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            var endSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(endPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            await SetOrAdjustLoop(startSec, endSec);
        }

        public async Task LoopSelectionStart(double startPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(startPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            await SetOrAdjustLoop(startSec, null); // Only proposing a new start
        }

        public async Task LoopSelectionEnd(double endPixel, double renderWidth)
        {
            var endSec = TimeSpan.FromSeconds(m_timelineCalculator.ConvertPixelToSecond(endPixel, m_audioEngine.ViewStartTime.TotalSeconds, m_audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            await SetOrAdjustLoop(null, endSec); // Only proposing a new end
        }

        public async Task ZoomWaveformMouse(Point p, double width)
        {
            var point = new System.Drawing.Point((int)p.X, (int)p.Y);

            var (newZoomFactor, zoomStartPosition) = m_timelineCalculator.ZoomWaveformMouse(point, m_audioEngine.ViewStartTime, m_audioEngine.ViewDuration, Zoom, width);

            Zoom = newZoomFactor;

            // Note: use lowerCase sliderZoom to avoid Trigger OnSliderZoomChange()
            sliderZoom = newZoomFactor;
            OnPropertyChanged(nameof(SliderZoom));

            m_audioEngine.ZoomViewWindowRelative(newZoomFactor, zoomStartPosition);

            await UpdateWaveformDTOAsync(RenderWidth);
            CreateMarkerDTO();
        }

        [RelayCommand]
        public async Task ZoomFromSlider(double value)
        {
            Zoom = value;

            m_audioEngine.ZoomViewWindow(value, m_audioEngine.CurrentPosition);

            await UpdateWaveformDTOAsync(RenderWidth);
        }

        [RelayCommand]
        public void ZoomFromMouse(double value)
        {
            Zoom = value;
        }

        [RelayCommand]
        public async Task FitLoopToView()
        {
            var (paddedStart, paddedEnd, zoomFactor) = m_timelineCalculator.FitLoopToView(m_audioEngine.LoopStart, m_audioEngine.LoopEnd, m_audioEngine.TotalDuration);

            m_audioEngine.ZoomViewWindow(zoomFactor, paddedStart, paddedEnd);

            var trackDTOList = new List<Memory<float>>();
            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(m_timelineService.RequestSample(i, RenderWidth, paddedStart, paddedEnd));
            }

            await UpdateWaveformDTOAsync(RenderWidth);
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
        public async Task AddTrack()
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
                await UpdateWaveformDTOAsync(RenderWidth);

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

        [RelayCommand]
        public async Task RequestWaveformUpdate(double newWidth)
        {
            RenderWidth = (int)newWidth;
            await UpdateWaveformDTOAsync(newWidth);
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

            await UpdateWaveformDTOAsync(RenderWidth);
            CreateMarkerDTO();

            // Prevent when user doubleclick, that WPF register as a mouseclick
            await Task.Delay(100);
        }

        private void OnMuteRequest(TrackControlViewModel trackViewModel)
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_trackSoloMuteService.MuteTrack(trackViewModel.TrackId);

            _ = UpdateWaveformDTOAsync(RenderWidth);
        }

        private void OnSoloRequest(TrackControlViewModel trackViewModel)
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_trackSoloMuteService.SoloTrack(trackViewModel.TrackId);

            _ = UpdateWaveformDTOAsync(RenderWidth);
        }

        private void OnVolumeChange(TrackControlViewModel trackViewModel, float volume)
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            m_trackSoloMuteService.SetTrackVolume(trackViewModel.TrackId, volume);

            _ = UpdateWaveformDTOAsync(RenderWidth);
        }

        private void OnDeleteTrackRequested(TrackControlViewModel trackViewModel)
        {
            var track = m_audioEngine.Project.Tracks.Where(t => t.ID == trackViewModel.TrackId).FirstOrDefault();

            if (track != null)
            {
                m_audioEngine.Project.Tracks.Remove(track);
                Tracks.Remove(trackViewModel);

                m_audioEngine.ReloadMixerInputs();

                StatusMessages = $"Track: {track.Name} deleted!";
            }
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

        private async Task SetOrAdjustLoop(TimeSpan? proposedStart, TimeSpan? proposedEnd)
        {
            // Determine the actual start and end for the loop operation
            // Use existing LoopStart/LoopEnd if not explicitly provided
            TimeSpan currentLoopStart = proposedStart ?? m_audioEngine.LoopStart;
            TimeSpan currentLoopEnd = proposedEnd ?? m_audioEngine.LoopEnd;

            if (proposedStart != null)
            {
                currentLoopStart = m_snappingService.SnapToMarkers(
                currentLoopStart,
                m_audioEngine.Project.BeatMarkers,
                m_audioEngine.ViewStartTime,
                m_audioEngine.ViewDuration,
                RenderWidth,
                thresholdInMs: AppConstants.SnappingTreshholdInMs);
            }

            if (proposedEnd != null)
            {
                currentLoopEnd = m_snappingService.SnapToMarkers(
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

            await UpdateWaveformDTOAsync(RenderWidth);
            CreateMarkerDTO();
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
                _ = UpdateWaveformDTOAsync(RenderWidth);
            }
        }

        private async Task UpdateWaveformDTOAsync(double newWidth)
        {
            var projectTracks = m_audioEngine.Project.Tracks;

            var waveformGenerationTasks = projectTracks.Select(async (projectTrack, index) =>
            {
                var existingTrackVM = Tracks.FirstOrDefault(t => t.TrackId == projectTrack.ID);
                if (existingTrackVM == null)
                {
                    existingTrackVM = new TrackControlViewModel(OnDeleteTrackRequested, OnMuteRequest, OnSoloRequest, OnVolumeChange);
                    existingTrackVM.TrackId = projectTrack.ID;
                    existingTrackVM.Name = projectTrack.Name;
                }

                var waveform = await Task.Run(() => m_timelineService.RequestSample(index, (int)newWidth));
                existingTrackVM.Waveform = waveform;
                existingTrackVM.IsSolo = projectTrack.IsSolo;
                existingTrackVM.IsMuted = projectTrack.IsMuted;
                existingTrackVM.Volume = projectTrack.Volume;

                return existingTrackVM;
            }).ToList();

            var updatedViewModels = await Task.WhenAll(waveformGenerationTasks);

            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = Tracks.Count - 1; i >= 0; i--)
                {
                    if (!projectTracks.Any(pt => pt.ID == Tracks[i].TrackId))
                    {
                        Tracks.RemoveAt(i);
                    }
                }

                foreach (var vm in updatedViewModels.OrderBy(v => v.TrackId))
                {
                    if (!Tracks.Any(t => t.TrackId == vm.TrackId))
                    {
                        Tracks.Add(vm);
                    }
                }
            });
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

            _ = UpdateWaveformDTOAsync(RenderWidth);
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
                _ = UpdateWaveformDTOAsync(RenderWidth);
            }
        }
    }
}