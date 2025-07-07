using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;
using LeaMusic.src.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace LeaMusicGui
{
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
        public double selectionStartPercentage;

        [ObservableProperty]
        public double selectionEndPercentage;

        [ObservableProperty]
        public double progressInPercentage;

        [ObservableProperty]
        public double test;

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
        bool isProjectLoading;

        public bool isLoopBeginDragLeftHandle;
        public bool isLoopBeginDragRightHandle;
        public bool isMarkerMoving;
        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();
        public ObservableCollection<MarkerDTO> BeatMarkers { get; set; } = new ObservableCollection<MarkerDTO>();

        private AudioEngine AudioEngine;
        private TimelineService TimelineService;
        private LeaResourceManager ResourceManager;
        private ProjectService ProjectService;
        private TimelineCalculator TimelineCalculator;
        private LoopService LoopService;
        private IResourceHandler resourceHandler;
        private IDialogService DialogService;

        private Project Project { get; set; }
        private bool isSyncEnabled { get; set; } = true;

        //is used when Zoom with mouse, to prevent to fetch waveform twice
        public bool supressZoom;

        public bool IsProjectLoaded => Project != null && Project.Duration > TimeSpan.FromSeconds(1);

        Action<string> updateStatus;

        public MainViewModel(ProjectService projectService,
                             LeaResourceManager resourceManager,
                             TimelineService timelineService,
                             AudioEngine audioEngine,
                             TimelineCalculator timelineCalculator,
                             LoopService loopService,
                             IDialogService dialogService)
        {

            ProjectService = projectService;
            ResourceManager = resourceManager;
            TimelineService = timelineService;
            AudioEngine = audioEngine;
            DialogService = dialogService;
            TimelineCalculator = timelineCalculator;
            LoopService = loopService;

            statusMessages = "";

            //Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            ProjectName = "NOT SET";

            resourceHandler = new FileHandler();

            audioEngine.MountProject(Project);

            audioEngine.OnUpdate += AudioEngine_OnPlayHeadChange;
            audioEngine.OnProgressChange += AudioEngine_OnProgressChange;
            audioEngine.OnLoopChange += AudioEngine_OnLoopChange;

            CompositionTarget.Rendering += (sender, e) => audioEngine.Update();

            updateStatus = (message) =>
            {
                StatusMessages = message;
            };
        }

        private async Task SaveProject()
        {
            await ProjectService.SaveProject(Project, updateStatus);
        }

        private async Task LoadProject()
        {
            //TMP
            isSyncEnabled = true;

            var project = await ProjectService.LoadProjectAsync(isGoogleDriveSync: isSyncEnabled, updateStatus);

            if (project == null)
            {
                StatusMessages = "Cant Load Project";
                return;
            }

            Project?.Dispose();

            Project = project;

            ProjectName = Project.Name;

            AudioEngine.MountProject(Project);
            AudioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

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
                trackDTOList.Add(TimelineService.RequestSample(i, (int)newWidth));
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
                    BeatMarkers[i].PositionRelativeView = TimelineCalculator.CalculateSecRelativeToViewWindowPercentage(BeatMarkers[i].Marker.Position, AudioEngine.ViewStartTime, AudioEngine.ViewDuration);
                }
                else
                {
                    BeatMarkers[i].Visible = false;
                }
            }

            ProjectBpm = AudioEngine.CalculateBpm(beatsPerMeasure: 4);
        }

        // currentMarkerID is set when the User click on the marker(Command) in the View,
        // after that OnRightMouseDown() invoke this Method and currentMarkerID is used to find the Marker the User clicked on.

        int currentMarkerID = 0;
        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = AudioEngine.Project.BeatMarkers.Where(marker => marker.ID == currentMarkerID).FirstOrDefault();

            if (marker != null)
                marker.Position = TimeSpan.FromSeconds(TimelineCalculator.ConvertPixelToSecond(position.X, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewDuration.TotalSeconds, renderWidth));

            UpdateMarkers();
        }

        private void UpdateTrackDTO(List<Memory<float>> waveforms)
        {
            for (int i = 0; i < waveforms.Count; i++)
            {
                if (WaveformWrappers.Count < waveforms.Count)
                    WaveformWrappers.Add(new TrackDTO());

                WaveformWrappers[i].Waveform = waveforms[i];
                WaveformWrappers[i].Name = AudioEngine.Project.Tracks[i].Name ?? "No Name Set";
                WaveformWrappers[i].TrackID = AudioEngine.Project.Tracks[i].ID;
            }
        }

        private void CreateTrackDTO()
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
                trackDTOList.Add(TimelineService.RequestSample(i, 1200));

            UpdateTrackDTO(trackDTOList);
        }

        //Maybe Create if the Marker Count > WrapperCount?!
        internal void CreateMarkerDTO()
        {
            BeatMarkers.Clear();
            for (int i = 0; i < AudioEngine.Project.BeatMarkers.Count; i++)
            {
                var marker = AudioEngine.Project.BeatMarkers[i];

                var w = new MarkerDTO();
                w.Marker = marker;
                w.PositionRelativeView = TimelineCalculator.CalculateSecRelativeToViewWindowPercentage(marker.Position, AudioEngine.ViewStartTime, AudioEngine.ViewDuration);
                w.Marker.ID = marker.ID;
                w.Marker.Description = marker.Description;
                BeatMarkers.Add(w);
            }
        }

        private bool IsMarkerVisible(MarkerDTO testMarker)
        {
            if (testMarker.Marker.Position < AudioEngine.ViewStartTime || testMarker.Marker.Position > AudioEngine.ViewEndTime)
                return false;

            return true;
        }

        private void AudioEngine_OnProgressChange(TimeSpan positionInSec)
        {
            TotalProjectDuration = Project.Duration.TotalSeconds;
            CurrentPlayTime = positionInSec.TotalSeconds;

            ProgressInPercentage = (AudioEngine.CurrentPosition.TotalSeconds / AudioEngine.TotalDuration.TotalSeconds) * 100;

            //scroll view when Playhead reach the end of the view
            if (AudioEngine.CurrentPosition >= AudioEngine.ViewEndTime)
            {
                AudioEngine.ZoomViewWindow(Zoom, AudioEngine.CurrentPosition + AudioEngine.halfViewWindow);
                UpdateWaveformDTO(RenderWidth);
            }
        }

        private void AudioEngine_OnLoopChange(TimeSpan startSec, TimeSpan endSec)
        {
            SelectionStartPercentage = TimelineCalculator.CalculateSecRelativeToViewWindowPercentage(startSec, AudioEngine.ViewStartTime, AudioEngine.ViewDuration);
            SelectionEndPercentage = TimelineCalculator.CalculateSecRelativeToViewWindowPercentage(endSec, AudioEngine.ViewStartTime, AudioEngine.ViewDuration);
        }

        private void AudioEngine_OnPlayHeadChange(TimeSpan positionInSeconds)
        {
            PlayheadPercentage = TimelineCalculator.CalculateSecRelativeToViewWindowPercentage(positionInSeconds, AudioEngine.ViewStartTime, AudioEngine.ViewDuration);
            UpdateMarkers();
        }

        partial void OnPitchChanged(int value)
        {
            AudioEngine.ChangePitch(value);
        }

        partial void OnProjectNameChanged(string value)
        {
            if (Project != null)
                Project.Name = value;
        }

        partial void OnScrollChanged(double value)
        {
            AudioEngine.ScrollWaveForm(value);

            UpdateWaveformDTO(RenderWidth);
        }

        partial void OnSpeedChanged(double value)
        {
            AudioEngine.ChangeSpeed(value);
        }

        partial void OnZoomChanged(double value)
        {
            Zoom = value;
            // supressZoom Prevents OnZoomChanged from being called when Zoom is set manually (e.g. during mouse zoom),
            // so we don't zoom twice or from the wrong position.
            //I set Zoom in ZoomWaveformMouse() to reflect the ZoomValue in the UI
            //Supress is false when i zoom with Slider in the UI, because we want zoom in the CurrentPosition, not in the ZoomPosition(mouse)
            if (!supressZoom)
            {
                AudioEngine.ZoomViewWindow(value, AudioEngine.CurrentPosition);

                UpdateWaveformDTO(RenderWidth);
            }
        }

        public void SetTextMarker()
        {
            AudioEngine.AddMarker(AudioEngine.CurrentPosition, "B");
            CreateMarkerDTO();
        }

        public void ResetZoomParameter()
        {
            TimelineCalculator.ResetZoomParameter();

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

            var result = TimelineCalculator.ZoomWaveformMouse(point, AudioEngine.ViewStartTime, AudioEngine.ViewDuration,  Zoom, width);
            supressZoom = true;
            Zoom = result.newZoomFactor;
            supressZoom = false;

            AudioEngine.ZoomViewWindowRelative(result.newZoomFactor, result.zoomStartPosition);

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

        private void SetOrAdjustLoop(TimeSpan? proposedStart, TimeSpan? proposedEnd)
        {
            // Determine the actual start and end for the loop operation
            // Use existing LoopStart/LoopEnd if not explicitly provided
            TimeSpan currentLoopStart = proposedStart ?? AudioEngine.LoopStart;
            TimeSpan currentLoopEnd = proposedEnd ?? AudioEngine.LoopEnd;

            if(proposedStart != null)
                currentLoopStart = SnappingService.SnapToMarkers(
                currentLoopStart,
                AudioEngine.Project.BeatMarkers,
                AudioEngine.ViewStartTime,
                AudioEngine.ViewDuration,
                RenderWidth, thresholdInMs: 10);


            if (proposedEnd != null)
                currentLoopEnd = SnappingService.SnapToMarkers(
                currentLoopEnd,
                AudioEngine.Project.BeatMarkers,
                AudioEngine.ViewStartTime,
                AudioEngine.ViewDuration,
                RenderWidth,
                thresholdInMs: 10);

            // Use the LoopPlaybackService to determine the final action
            var loopAction = LoopService.DetermineLoopAction(currentLoopStart, currentLoopEnd);

            if (!loopAction.ShouldSetLoop && !loopAction.ShouldJump)
            {
                return; // Service decided to ignore this input
            }

            if (loopAction.ShouldSetLoop)
            {
                AudioEngine.Loop(loopAction.LoopStart, loopAction.LoopEnd);
            }

            if (loopAction.ShouldJump)
            {
                AudioEngine.AudioJumpToSec(loopAction.JumpToPosition);
            }

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

     
        public void LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(TimelineCalculator.ConvertPixelToSecond(startPixel, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            var endSec = TimeSpan.FromSeconds(TimelineCalculator.ConvertPixelToSecond(endPixel, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            SetOrAdjustLoop(startSec, endSec);
        }

        public void LoopSelectionStart(double startPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(TimelineCalculator.ConvertPixelToSecond(startPixel, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            
            SetOrAdjustLoop(startSec, null); // Only proposing a new start
        }

        public void LoopSelectionEnd(double endPixel, double renderWidth)
        {
            var endSec = TimeSpan.FromSeconds(TimelineCalculator.ConvertPixelToSecond(endPixel, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
           
            SetOrAdjustLoop(null, endSec); // Only proposing a new end
        }

        [RelayCommand]
        public void FitLoopToView()
        {
            var result = TimelineCalculator.FitLoopToView(AudioEngine.LoopStart, AudioEngine.LoopEnd, AudioEngine.TotalDuration);
         
            AudioEngine.ZoomViewWindow(result.zoomFactor, result.paddedStart, result.paddedEnd);

            var trackDTOList = new List<Memory<float>>();
            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(TimelineService.RequestSample(i, RenderWidth, result.paddedStart, result.paddedEnd));
            }

            UpdateTrackDTO(trackDTOList);
            CreateMarkerDTO();

            supressZoom = true;
            Zoom = result.zoomFactor;
            supressZoom = false;
        }

        [RelayCommand]
        private void MarkerClick(MarkerDTO marker)
        {
            currentMarkerID = marker.Marker.ID;
        }

        [RelayCommand]
        private void MarkerDelete(MarkerDTO marker)
        {
            AudioEngine.Project.DeleteMarker(marker.Marker.ID);

            CreateMarkerDTO();
        }

        [RelayCommand]
        private void Replay()
        {
            AudioEngine.Replay();
        }

        [RelayCommand]
        private void MuteAllTracks()
        {
            AudioEngine.MuteAllTracks();
        }

        [RelayCommand]
        private async Task SaveProjectFile()
        {
            resourceHandler = new FileHandler();
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

            AudioEngine.Pause();
        }

        [RelayCommand]
        private async Task LoadProjectFile()
        {
            resourceHandler = new FileHandler();
            await LoadProject();

            StatusMessages = $"Project: {Project.Name} loaded!";
        }

        [RelayCommand]
        private void AddTrack()
        {
            var filePath = DialogService.OpenFile("Mp3 (*.mp3)|*.mp3");

            if (!string.IsNullOrEmpty(filePath))
            {
                AudioEngine.Stop();

                var projectfilePath = filePath;
                var track = ResourceManager.ImportTrack(new FileLocation(projectfilePath), resourceHandler);

                Project.AddTrack(track);

                if (Project.Tracks.Count == 1)
                    ProjectName = Project.Tracks[0].Name ?? "No Name Set";

                Project.SetTempo(Speed);
                CreateTrackDTO();

                //Check if audioengine is playing while addTrack, when true continue playing
                AudioEngine.MountProject(Project);
                AudioEngine.AudioJumpToSec(AudioEngine.CurrentPosition);
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
            AudioEngine.Play();
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
                AudioEngine.MuteTrack(wrapper.TrackID);
            }
        }

        [RelayCommand]
        private void Delete(object param)
        {
            if (param is TrackDTO wrapper)
            {
                var track = AudioEngine.Project.Tracks.Where(t => t.ID == wrapper.TrackID).FirstOrDefault();

                if (track != null)
                {
                    AudioEngine.Project.Tracks.Remove(track);
                    WaveformWrappers.Remove(wrapper);

                    AudioEngine.ReloadMixerInputs();

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

            AudioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }


        [RelayCommand]
        private void JumpToLoopStart()
        {
            if (AudioEngine.LoopStart > TimeSpan.Zero)
            {
                AudioEngine.AudioJumpToSec(AudioEngine.LoopStart);
                Play();
            }
        }

    }
}