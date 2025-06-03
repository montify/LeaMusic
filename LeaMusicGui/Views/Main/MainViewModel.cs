using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;
using LeaMusic.src.ResourceManager_.GoogleDrive_;
using LeaMusicGui.Views.DialogServices;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

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

        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();
        public ObservableCollection<MarkerDTO> TestMarkers { get; set; } = new ObservableCollection<MarkerDTO>();

        private AudioEngine audioEngine;
        private TimelineService TimelineService;
        private LeaResourceManager resourceManager;
        private Project Project { get; set; }
        private bool isSyncEnabled { get; set; } = true;
        //is used when Zoom with mouse, to prevent to fetch waveform twice
        public bool supressZoom;
        private IResourceHandler resourceHandler;
        private TimeSpan zoomMouseStartPosition;
        private double zoomStartMouseY;
        private bool zoomStartPositionSetOnce;

        public IDialogService? DialogService { get; set; }

        public bool IsProjectLoaded => Project != null && Project.Duration > TimeSpan.FromSeconds(1);

        public MainViewModel()
        {
            resourceManager = new LeaResourceManager();
            audioEngine = new AudioEngine();

            TimelineService = new TimelineService(audioEngine);

            //Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            // Project.AddTrack(new Track("C:\\Users\\alexlapi\\Desktop\\v1\\AudioFiles\\Hairflip.mp3"));
            ProjectName = "NOT SET";
            resourceHandler = new FileHandler();

            audioEngine.MountProject(Project);

            audioEngine.OnUpdate += AudioEngine_OnPlayHeadChange;
            audioEngine.OnProgressChange += AudioEngine_OnProgressChange;
            audioEngine.OnLoopChange += AudioEngine_OnLoopChange;

            CompositionTarget.Rendering += (sender, e) => audioEngine.Update();
        }

        private async Task SaveProject()
        {
            //Maybe Stop audioEngine here, and play again if previous state was play 
            if (Project.Duration == TimeSpan.FromSeconds(1))
            {
                StatusMessages = "Cant save project, you have to load a project!";
                return;
            }
            var oldLastSave = Project.LastSaveAt;
            Project.LastSaveAt = DateTime.Now;

            try
            {
                if (resourceHandler is FileHandler fileHandler)
                {
                    string? dialogResult = DialogService.Save();

                    if (!string.IsNullOrEmpty(dialogResult))
                    {
                        resourceManager.SaveProject(Project, new FileLocation(dialogResult), fileHandler);

                        StatusMessages = $"Project is saved Local at: {dialogResult}";

                        //ifSyncEnabled && isTokenValid(Auth Token google)

                        if (DialogService.EnableSync())
                        {
                            var gDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);
                            //Todo: save rootFolder in GoogleDriveHandler
                            // var gDriveLocation = new GDriveLocation(gDriveRootFolder: "", gDrivelocalPath: "", projectName: "");
                            StatusMessages = $"Begin Project to Google Drive!";
                            await resourceManager.SaveProject(Project, default, gDriveHandler);
                            StatusMessages = $"Uploaded Project to Google Drive!";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Project.LastSaveAt = oldLastSave;
                StatusMessages = $"Error: {e.Message}";
                return;
            }
        }

        private async Task LoadProject()
        {
            try
            {
                if (resourceHandler == null)
                    throw new Exception("ResourceHandler invalid");

                if (resourceHandler is not FileHandler fileHandler)
                    throw new Exception("ResourceHandler is not a FileHandler");

                var dialogResult = DialogService?.OpenFile("Project (*.prj)|*.prj");

                if (string.IsNullOrEmpty(dialogResult))
                    return;

                IsProjectLoading = true;

                var location = new FileLocation(dialogResult);
                var projectName = Path.GetFileNameWithoutExtension(location.Path);
                var googleDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

                //Fetch project Metadata, and compare on Date
                ProjectMetadata? fileMetaData = resourceManager.GetProjectMetaData($"{projectName}.zip", location, fileHandler);
                ProjectMetadata? gDriveMetaData = resourceManager.GetProjectMetaData($"{projectName}", null, googleDriveHandler);

                Project?.Dispose();
                Project = null;


                if (isSyncEnabled &&
                    gDriveMetaData?.lastSavedAt > fileMetaData?.lastSavedAt &&
                      DialogService.AskDownloadGoogleDrive(localDate: fileMetaData.lastSavedAt, googleDriveDate: gDriveMetaData.lastSavedAt))
                {
                    StatusMessages = $"Download Project: {projectName} from google Drive";
                    var gdriveLocation = new GDriveLocation("LeaRoot", dialogResult, projectName);

                    Project = await resourceManager.LoadProject(gdriveLocation, googleDriveHandler);
                }
                else
                {
                    StatusMessages = $"Load Project: {ProjectName} from LocalFile";

                    Project = await resourceManager.LoadProject(new FileLocation(dialogResult), fileHandler);
                }


                if (Project == null)
                {
                    //Expose to View
                    StatusMessages = "Cant load Project";
                    return;
                }

                ProjectName = Project.Name;

                audioEngine.MountProject(Project);
                audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

                CreateTrackDTO();
                CreateMarkerDTO();

                //Prevent when user doubleclick, that WPF register as a mouseclick
                await Task.Delay(100);

            }
            catch (Exception e)
            {
                StatusMessages = $"Cant load Project, maybe File is corrupt";
            }
            finally
            {
                IsProjectLoading = false;
            }
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
            for (int i = 0; i < TestMarkers.Count; i++)
            {
                if (IsMarkerVisible(TestMarkers[i]))
                {
                    TestMarkers[i].Visible = true;
                    TestMarkers[i].PositionRelativeView = CalculateSecRelativeToViewWindowPercentage(TestMarkers[i].Marker.Position, audioEngine.ViewStartTime, audioEngine.ViewDuration);
                }
                else
                {
                    TestMarkers[i].Visible = false;
                }
            }

            ProjectBpm = audioEngine.CalculateBpm(beatsPerMeasure: 4);
        }

        // currentMarkerID is set when the User click on the marker(Command) in the View,
        // after that OnRightMouseDown() invoke this Method and currentMarkerID is used to find the Marker the User clicked on.

        int currentMarkerID = 0;
        public void MoveMarker(Point position, int renderWidth)
        {
            var marker = audioEngine.Project.BeatMarkers.Where(marker => marker.ID == currentMarkerID).FirstOrDefault();

            if (marker != null)
                marker.Position = TimeSpan.FromSeconds(ConvertPixelToSecond(position.X, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, renderWidth));

            UpdateMarkers();
        }

        private void UpdateTrackDTO(List<Memory<float>> waveforms)
        {
            for (int i = 0; i < waveforms.Count; i++)
            {
                if (WaveformWrappers.Count < waveforms.Count)
                    WaveformWrappers.Add(new TrackDTO());

                WaveformWrappers[i].Waveform = waveforms[i];
                WaveformWrappers[i].Name = audioEngine.Project.Tracks[i].Name;
                WaveformWrappers[i].TrackID = audioEngine.Project.Tracks[i].ID;
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
        private void CreateMarkerDTO()
        {
            TestMarkers.Clear();
            for (int i = 0; i < audioEngine.Project.BeatMarkers.Count; i++)
            {
                var marker = audioEngine.Project.BeatMarkers[i];

                var w = new MarkerDTO();
                w.Marker = marker;
                w.PositionRelativeView = CalculateSecRelativeToViewWindowPercentage(marker.Position, audioEngine.ViewStartTime, audioEngine.ViewDuration);
                w.Marker.ID = marker.ID;
                w.Marker.Description = marker.Description;
                TestMarkers.Add(w);
            }
        }

        private bool IsMarkerVisible(MarkerDTO testMarker)
        {
            if (testMarker.Marker.Position < audioEngine.ViewStartTime || testMarker.Marker.Position > audioEngine.ViewEndTime)
                return false;

            return true;
        }

        private void AudioEngine_OnProgressChange(TimeSpan positionInSec)
        {
            TotalProjectDuration = Project.Duration.TotalSeconds;
            CurrentPlayTime = positionInSec.TotalSeconds;

            ProgressInPercentage = (audioEngine.CurrentPosition.TotalSeconds / audioEngine.TotalDuration.TotalSeconds) * 100;

            //scroll view when Playhead reach the end of the view
            if (audioEngine.CurrentPosition >= audioEngine.ViewEndTime)
            {
                audioEngine.ZoomViewWindow(Zoom, audioEngine.CurrentPosition + audioEngine.halfViewWindow);
                UpdateWaveformDTO(RenderWidth);
            }
        }

        public double CalculateSecRelativeToViewWindowPercentage(TimeSpan positionSec, TimeSpan viewStartTimeSec, TimeSpan viewDurationSec)
        {
            TimeSpan positionInViewSec = positionSec - viewStartTimeSec;

            double relativePercentage = positionInViewSec.TotalSeconds / viewDurationSec.TotalSeconds;

            relativePercentage = Math.Max(0.0f, Math.Min(1.0f, relativePercentage));

            return relativePercentage * 100;
        }

        private void AudioEngine_OnLoopChange(TimeSpan startSec, TimeSpan endSec)
        {
            SelectionStartPercentage = CalculateSecRelativeToViewWindowPercentage(startSec, audioEngine.ViewStartTime, audioEngine.ViewDuration);
            SelectionEndPercentage = CalculateSecRelativeToViewWindowPercentage(endSec, audioEngine.ViewStartTime, audioEngine.ViewDuration);
        }

        private void AudioEngine_OnPlayHeadChange(TimeSpan positionInSeconds)
        {
            PlayheadPercentage = CalculateSecRelativeToViewWindowPercentage(positionInSeconds, audioEngine.ViewStartTime, audioEngine.ViewDuration);
            UpdateMarkers();
        }

        partial void OnPitchChanged(int value)
        {
            audioEngine.ChangePitch(value);
        }

        partial void OnProjectNameChanged(string value)
        {
            if (Project != null)
                Project.Name = value;

        }

        partial void OnScrollChanged(double value)
        {
            audioEngine.ScrollWaveForm(value);

            UpdateWaveformDTO(RenderWidth);
        }

        partial void OnSpeedChanged(double value)
        {
            audioEngine.ChangeSpeed(value);
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
                audioEngine.ZoomViewWindow(value, audioEngine.CurrentPosition);

                UpdateWaveformDTO(RenderWidth);
            }
        }

        public void SetTextMarker()
        {
            audioEngine.AddMarker(audioEngine.CurrentPosition, "B");
            CreateMarkerDTO();
        }

        public void ResetZoomParameter()
        {
            zoomStartPositionSetOnce = false;
        }

        [RelayCommand]
        public void FitLoopToView()
        {
            if (audioEngine.LoopEnd - audioEngine.LoopStart <= TimeSpan.Zero)
                return;

            var loopStart = audioEngine.LoopStart;
            var loopEnd = audioEngine.LoopEnd;
            var loopDuration = loopEnd - loopStart;

            //Add Padding left and right 
            double paddingFactor = 0.05;
            var padding = TimeSpan.FromSeconds(loopDuration.TotalSeconds * paddingFactor);
            var paddedStart = loopStart - padding;
            var paddedEnd = loopEnd + padding;
            paddedStart = TimeSpan.FromSeconds(Math.Max(0, paddedStart.TotalSeconds));
            paddedEnd = TimeSpan.FromSeconds(Math.Min(audioEngine.TotalDuration.TotalSeconds, paddedEnd.TotalSeconds));

            var paddedDuration = paddedEnd - paddedStart;

            double zoomFactor = audioEngine.TotalDuration.TotalSeconds / paddedDuration.TotalSeconds;

            // Zoom to padded loo
            audioEngine.ZoomWaveForm(zoomFactor, paddedStart, paddedEnd);

            //audioEngine.Update();

            var trackDTOList = new List<Memory<float>>();
            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(TimelineService.RequestSample(i, RenderWidth, paddedStart, paddedEnd));
            }
            UpdateTrackDTO(trackDTOList);

            CreateMarkerDTO();

            supressZoom = true;
            Zoom = zoomFactor;
            supressZoom = false;
        }

        /// <summary>
        /// Performs a smooth zoom operation on the waveform based on mouse movement.
        /// The zoom is anchored at the horizontal position of the ZoomStart Position
        /// </summary>
        /// <param name="p">The current mouse position.</param>
        /// <param name="width">The width of the waveform display in pixels.</param>
        public void ZoomWaveformMouse(Point p, double width)
        {
            if (!zoomStartPositionSetOnce)
            {
                zoomMouseStartPosition = TimeSpan.FromSeconds(ConvertPixelToSecond(p.X, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)width));
                zoomStartPositionSetOnce = true;
                zoomStartMouseY = p.Y;
            }

            double zoomSensitivity = 0.002f;
            double maxZoom = 60;
            double minZoom = 1;
            double zoomRange = maxZoom - minZoom;

            var delta = p.Y - zoomStartMouseY;

            double relativeScale = 1 + (delta * zoomSensitivity);

            //Avoid crazy Zoom :D
            relativeScale = Math.Clamp(relativeScale, 0.5, 2.0);

            double newZoomFactor = Zoom * relativeScale;
            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);

            supressZoom = true;
            Zoom = newZoomFactor;
            supressZoom = false;

            audioEngine.ZoomViewWindowRelative(newZoomFactor, zoomMouseStartPosition);

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();
        }

        //TODO: LoopSelection is the wrong name, because this function make a Loop AND reposition the Playhead, find a better Solution
        public void LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            var startSec = TimeSpan.FromSeconds(ConvertPixelToSecond(startPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            var endSec = TimeSpan.FromSeconds(ConvertPixelToSecond(endPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            //When Loop is <=0 than treat it as the user Jump to a new Position
            if (startSec >= endSec || endSec - startSec <= TimeSpan.Zero)
            {
                audioEngine.Loop(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0));
                audioEngine.AudioJumpToSec(startSec);
            }
            //If loop is to shoort, ignore it 
            else if ((endSec - startSec) < TimeSpan.FromMilliseconds(50))
                return;


            audioEngine.Loop(startSec, endSec);
            audioEngine.AudioJumpToSec(startSec);
        }

        public void LoopSelectionStart(double startPixel, double renderWidth)
        {
            var beatmarkers = audioEngine.Project.BeatMarkers;
            var startSec = TimeSpan.FromSeconds(ConvertPixelToSecond(startPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));
            var endSec = audioEngine.LoopEnd;

            // TODO: Optimize — stop looping through beat markers once the nearest valid point is found.
            // TODO: Should the snapping logic be moved to the code-behind?
            foreach (var beatMarker in beatmarkers)
                startSec = Helpers.ChechSnapping(audioEngine, renderWidth, beatMarker.Position, startSec, treshholdInMs: 10);

            audioEngine.Loop(startSec, endSec);
        }

        public void LoopSelectionEnd(double endPixel, double renderWidth)
        {
            var beatmarkers = audioEngine.Project.BeatMarkers;
            var startSec = audioEngine.LoopStart;
            var endSec = TimeSpan.FromSeconds(ConvertPixelToSecond(endPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth));

            // TODO: Optimize — stop looping through beat markers once the nearest valid point is found.
            // TODO: Should the snapping logic be moved to the code-behind?
            foreach (var beatMarker in beatmarkers)
                endSec = Helpers.ChechSnapping(audioEngine, renderWidth, beatMarker.Position, endSec, treshholdInMs: 10);

            audioEngine.Loop(startSec, endSec);
        }

        public double ConvertPixelToSecond(double pixelPos, double viewStartTimeSec, double viewDurationSec, int renderWidth)
        {
            double pixelPercentage = pixelPos / renderWidth;
            double timeInSeconds = viewStartTimeSec + (pixelPercentage * viewDurationSec);

            return timeInSeconds;
        }

        [RelayCommand]
        private async Task MarkerClick(MarkerDTO marker)
        {
            currentMarkerID = marker.Marker.ID;
        }
        [RelayCommand]
        private async Task MarkerDelete(MarkerDTO marker)
        {
            audioEngine.Project.DeleteMarker(marker.Marker.ID);

            CreateMarkerDTO();
        }

        [RelayCommand]
        private async Task Replay()
        {
            audioEngine.Replay();
        }

        [RelayCommand]
        private async Task MuteAllTracks()
        {
            audioEngine.MuteAllTracks();
        }

        [RelayCommand]
        private async Task SaveProjectFile()
        {
            resourceHandler = new FileHandler();
            await SaveProject();
        }

        [RelayCommand]
        private async Task Pause()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            audioEngine.Pause();
        }

        [RelayCommand]
        private async Task LoadProjectFile()
        {
            resourceHandler = new FileHandler();
            await LoadProject();

            StatusMessages = $"Project: {Project.Name} loaded!";
        }

        [RelayCommand]
        private async Task AddTrack()
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Mp3 (*.mp3)|*.mp3"
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                audioEngine.Stop();

                var projectfilePath = fileDialog.FileName;
                var track = resourceManager.ImportTrack(new FileLocation(projectfilePath), resourceHandler);

                Project.AddTrack(track);

                if (Project.Tracks.Count == 1)
                    ProjectName = Project.Tracks[0].Name;

                Project.SetTempo(Speed);
                CreateTrackDTO();

                //Check if audioengine is playing while addTrack, when true continue playing
                audioEngine.MountProject(Project);
                audioEngine.AudioJumpToSec(audioEngine.CurrentPosition);
            }
        }

        [RelayCommand]
        private async Task Play()
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }
            audioEngine.Play();
        }

        [RelayCommand]
        private async Task Mute(object param)
        {
            if (!IsProjectLoaded)
            {
                StatusMessages = "Pleas load a Project...";
                return;
            }

            if (param is TrackDTO wrapper)
            {
                audioEngine.MuteTrack(wrapper.TrackID);
            }
        }

        [RelayCommand]
        private async Task Delete(object param)
        {
            if (param is TrackDTO wrapper)
            {
                var track = audioEngine.Project.Tracks.Where(t => t.ID == wrapper.TrackID).FirstOrDefault();

                if (track != null)
                {
                    audioEngine.Project.Tracks.Remove(track);
                    WaveformWrappers.Remove(wrapper);

                    audioEngine.ReloadMixerInputs();

                    StatusMessages = $"Track: {track.Name} deleted!";
                }
            }
        }

        [RelayCommand]
        private async Task JumpToSec()
        {
            if (TimeSpan.FromSeconds(JumpToPositionInSec) < TimeSpan.Zero)
            {
                StatusMessages = "Please enter a positive Number";
                return;
            }

            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }


        [RelayCommand]
        private async Task JumpToLoopStart()
        {
            if (audioEngine.LoopStart > TimeSpan.Zero)
            {
                audioEngine.AudioJumpToSec(audioEngine.LoopStart);
                await Play();
            }
        }

    }
}