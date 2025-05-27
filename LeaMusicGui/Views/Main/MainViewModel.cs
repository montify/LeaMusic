using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;
using LeaMusic.src.ResourceManager_.GoogleDrive_;
using LeaMusicGui.Views.DialogServices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

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
        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();
        public ObservableCollection<MarkerDTO> TestMarkers { get; set; } = new ObservableCollection<MarkerDTO>();

        private AudioEngine audioEngine;
        private TimelineService TimelineService;
        private LeaResourceManager resourceManager;
        private Project Project { get; set; }
        private bool isSyncEnabled { get; set; } = true;
        //is used when Zoom with mouse, to prevent to fetch waveform twice
        private bool supressZoom;
        private IResourceHandler resourceHandler;
        private TimeSpan zoomMouseStartPosition;
        private double oldZoomFactor = 1.0f;
        private double zoomStartMouseY;
        private bool zoomStartPositionSetOnce;

        public IDialogService? DialogService { get; set; }

        public MainViewModel()
        {
            resourceManager = new LeaResourceManager();
            audioEngine = new AudioEngine();
            
            TimelineService = new TimelineService(audioEngine);

            //Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            // Project.AddTrack(new Track("C:\\Users\\alexlapi\\Desktop\\v1\\AudioFiles\\Hairflip.mp3"));
            ProjectName = "NOT SET";

            audioEngine.MountProject(Project);

            audioEngine.OnUpdate += AudioEngine_OnPlayHeadChange;
            audioEngine.OnProgressChange += AudioEngine_OnProgressChange;
            audioEngine.OnLoopChange += AudioEngine_OnLoopChange;

            resourceHandler = new FileHandler();
         
            CompositionTarget.Rendering += (sender, e) => audioEngine.Update();
        }

        private async Task SaveProject()
        {
            //Maybe Stop audioEngine here, and play again if previous state was play 
           
            var oldLastSave = Project.LastSaveAt;
            Project.LastSaveAt = DateTime.Now;

            try
            {
                if (resourceHandler is FileHandler fileHandler)
                {
                    string? dialogResult = DialogService.Save();

                    if(!string.IsNullOrEmpty(dialogResult))
                    {
                        resourceManager.SaveProject(Project, new FileLocation(dialogResult), fileHandler);
                        Debug.WriteLine($"Project is saved Local at: {dialogResult}");

                        //ifSyncEnabled && isTokenValid(Auth Token google)
                        if (isSyncEnabled)
                        {
                            var gDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);
                            //Todo: save rootFolder in GoogleDriveHandler
                            // var gDriveLocation = new GDriveLocation(gDriveRootFolder: "", gDrivelocalPath: "", projectName: "");
                            resourceManager.SaveProject(Project, default, gDriveHandler);
                            Debug.WriteLine("Uploaded Project to Google Drive!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Project.LastSaveAt = oldLastSave;
                throw;
            }
        }

        private async Task LoadProject()
        {
            Project?.Dispose();
            Project = null;
          
            try
            {
                IsProjectLoading = true;

                if (resourceHandler == null)
                    throw new Exception("ResourceHandler invalid");

                if (resourceHandler is not FileHandler fileHandler)
                    throw new Exception("ResourceHandler is not a FileHandler");

                var dialogResult = DialogService?.OpenFile("Project (*.prj)|*.prj");

                if (string.IsNullOrEmpty(dialogResult))
                    return;

                var location = new FileLocation(dialogResult);
                var projectName = Path.GetFileNameWithoutExtension(location.Path);
                var googleDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

                //Fetch project Metadata, and compare on Date
                ProjectMetadata? fileMetaData = resourceManager.GetProjectMetaData($"{projectName}.zip", location, fileHandler);
                ProjectMetadata? gDriveMetaData = resourceManager.GetProjectMetaData($"{projectName}", null, googleDriveHandler);

                if (isSyncEnabled &&
                    gDriveMetaData?.lastSavedAt > fileMetaData?.lastSavedAt)
                {
                    Debug.WriteLine($"GoogleDrive Project: {projectName} is newer, DOWNLOAD IT!");
                    var gdriveLocation = new GDriveLocation("LeaRoot", dialogResult, projectName);

                    Project = await resourceManager.LoadProject(gdriveLocation, googleDriveHandler);
                }
                else
                {
                    Debug.WriteLine("LOCAL Project is newer, NO DOWNLOAD");
                    Project = await resourceManager.LoadProject(new FileLocation(dialogResult), fileHandler);
                }


                if (Project == null)
                {
                    //Expose to View
                    throw new Exception("Cant load Project");
                }

                ProjectName = Project.Name;

                audioEngine.MountProject(Project);
                audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

                CreateTrackDTO();
                CreateMarkerDTO();

                //Prevent when user doubleclick, that WPF register as a mouseclick
                await Task.Delay(100);

            }
            catch(Exception e)
            {
                Console.WriteLine();
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
            ProgressInPercentage = (audioEngine.CurrentPosition.TotalSeconds / audioEngine.TotalDuration.TotalSeconds) * 100;

            //scroll view when Playhead reach the end of the view
            if (audioEngine.CurrentPosition >= audioEngine.ViewEndTime)
            {
                audioEngine.ZoomWaveForm(Zoom, audioEngine.CurrentPosition + audioEngine.halfViewWindow); 

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
            // supressZoom Prevents OnZoomChanged from being called when Zoom is set manually (e.g. during mouse zoom),
            // so we don't zoom twice or from the wrong position.
            //I set Zoom in ZoomWaveformMouse() to reflect the ZoomValue in the UI
            //Supress is false when i zoom with Slider in the UI, because we want zoom in the CurrentPosition, not in the ZoomPosition(mouse)
            if (!supressZoom)
            {
                audioEngine.ZoomWaveForm(value, audioEngine.CurrentPosition);

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

            double newZoomFactor = oldZoomFactor * relativeScale;
            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);

            supressZoom = true;
            Zoom = newZoomFactor;
            supressZoom = false;

            audioEngine.ZoomWaveForm(newZoomFactor, zoomMouseStartPosition);

            UpdateWaveformDTO(RenderWidth);
            CreateMarkerDTO();

            oldZoomFactor = newZoomFactor;
        }

        public void LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            Debug.WriteLine($"LoopStart: {startPixel}, Loop end {endPixel}");

            var startSec = ConvertPixelToSecond(startPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth);
            var endSec = ConvertPixelToSecond(endPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth);

            audioEngine.Loop(TimeSpan.FromSeconds(startSec), TimeSpan.FromSeconds(endSec));
            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(startSec));
        }

        public void LoopSelectionStart(double startPixel, double renderWidth)
        {
            var startSec = ConvertPixelToSecond(startPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth);
            var endSec = audioEngine.LoopEnd.TotalSeconds;

            audioEngine.Loop(TimeSpan.FromSeconds(startSec), TimeSpan.FromSeconds(endSec));
        }

        public void LoopSelectionEnd(double endPixel, double renderWidth)
        {
            var startSec = audioEngine.LoopStart.TotalSeconds;
            var endSec = ConvertPixelToSecond(endPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth);

            audioEngine.Loop(TimeSpan.FromSeconds(startSec), TimeSpan.FromSeconds(endSec));
        }

        public double ConvertPixelToSecond(double pixelPos, double viewStartTimeSec, double viewDurationSec, int renderWidth)
        {
            double pixelPercentage = pixelPos / renderWidth;
            double timeInSeconds = viewStartTimeSec + (pixelPercentage * viewDurationSec);

            return timeInSeconds;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderWidth = (int)e.NewSize.Width;
        }

        [RelayCommand]
        private async Task MarkerClick(MarkerDTO marker)
        {
            currentMarkerID = marker.Marker.ID;
        }

        [RelayCommand]
        private async Task Replay()
        {
            audioEngine.Replay();
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
            audioEngine.Pause();
        }

        [ObservableProperty]
        bool isProjectLoading;

        [RelayCommand]
        private async Task LoadProjectFile()
        {
            resourceHandler = new FileHandler();
            await LoadProject();
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
            audioEngine.Play();
        }

        [RelayCommand]
        private async Task Mute(object param)
        {
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
                }
            }
        }

        [RelayCommand]
        private async Task JumpToSec()
        {
            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }
    }
}