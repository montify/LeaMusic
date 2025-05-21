using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;
using LeaMusic.src.ResourceManager_.GoogleDrive_;
using LeaMusicGui.Views;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Application = System.Windows.Application;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace LeaMusicGui
{
    public partial class TrackDTO : ObservableObject
    {
        [ObservableProperty]
        private ReadOnlyMemory<float> _waveform;

        public int TrackID { get; set; }
    }

    public partial class MarkerDTO : ObservableObject
    {
        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private Marker marker;

        [ObservableProperty]
        public bool visible = true;
    }

    public partial class MainViewModel : ObservableObject
    {
        private AudioEngine audioEngine;
        private TimelineService TimelineService;

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


        private LeaResourceManager resourceManager;

        public Project Project { get; private set; }
        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();
        public ObservableCollection<MarkerDTO> TestMarkers { get; set; } = new ObservableCollection<MarkerDTO>();

        //is used when Zoom with mouse, to prevent to fetch waveform twice
        private bool supressZoom;

        private IResourceHandler resourceHandler;


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
           // resourceHandler = new GoogleDriveHandler("1.1.1.1", "alex", "123", new FileHandler());

            CompositionTarget.Rendering += (sender, e) => audioEngine.Update();
        }

        public void UpdateWaveform(double newWidth)
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
            {
                trackDTOList.Add(TimelineService.RequestSample(i, (int)newWidth));
            }

            UpdateTrackDTO(trackDTOList);
        }

        private void CreateTrackDTO()
        {
            var trackDTOList = new List<Memory<float>>();

            for (int i = 0; i < Project.Tracks.Count; i++)
                trackDTOList.Add(TimelineService.RequestSample(i, 1200));

            UpdateTrackDTO(trackDTOList);
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
                TestMarkers.Add(w);
            }
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

        private bool IsMarkerVisible(MarkerDTO testMarker)
        {
            if (testMarker.Marker.Position < audioEngine.ViewStartTime || testMarker.Marker.Position > audioEngine.ViewEndTime)
                return false;

            return true;
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

        private void AudioEngine_OnProgressChange(TimeSpan positionInSec)
        {
            ProgressInPercentage = (audioEngine.CurrentPosition.TotalSeconds / audioEngine.TotalDuration.TotalSeconds) * 100;

            //scroll view when Playhead reach the end of the view
            if (audioEngine.CurrentPosition >= audioEngine.ViewEndTime)
            {
                OnZoomChanged(Zoom); //HACK?? :D   
            }
        }

        public double CalculateSecRelativeToViewWindowPercentage(TimeSpan positionSec, TimeSpan viewStartTimeSec, TimeSpan viewDurationSec)
        {
            TimeSpan positionInViewSec = positionSec - viewStartTimeSec;

            double relativePercentage = positionInViewSec.TotalSeconds / viewDurationSec.TotalSeconds;

            relativePercentage = Math.Max(0.0f, Math.Min(1.0f, relativePercentage));

            return relativePercentage * 100;
        }

        partial void OnPitchChanged(int value)
        {
            audioEngine.ChangePitch(value);
        }

        partial void OnProjectNameChanged(string value)
        {
            if(Project != null)
            Project.Name = value;

        }
        partial void OnScrollChanged(double value)
        {
            audioEngine.ScrollWaveForm(value);

            UpdateWaveform(RenderWidth);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderWidth = (int)e.NewSize.Width;
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

                UpdateWaveform(RenderWidth);
            }
        }

        public void SetTextMarker()
        {
            audioEngine.AddMarker(audioEngine.CurrentPosition, "B");
            CreateMarkerDTO();
        }

        partial void OnSpeedChanged(double value)
        {
            audioEngine.ChangeSpeed(value);
        }

        private TimeSpan zoomMouseStartPosition;
        private double oldZoomFactor = 1.0f;
        private double zoomStartMouseY;

        private bool zoomStartPositionSetOnce;
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

            double relativeScale  = 1 + (delta * zoomSensitivity);

            //Avoid crazy Zoom :D
            relativeScale = Math.Clamp(relativeScale, 0.5, 2.0); 

            double newZoomFactor = oldZoomFactor * relativeScale;

            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);
            // audioEngine.Zoom = newZoomFactor;

            supressZoom = true;
            Zoom = newZoomFactor;
            supressZoom = false;
            //Debug.WriteLine($"relativeScale: {relativeScale}");
            //Debug.WriteLine($"oldZoomFactor: {oldZoomFactor}");
            //Debug.WriteLine($"newZoomFactor: {newZoomFactor}");

           // audioEngine.ZoomPositon = zoomStartPosition;
            audioEngine.ZoomWaveForm(newZoomFactor, zoomMouseStartPosition);


            UpdateWaveform(RenderWidth);

            //audioEngine.AddMarker(TimeSpan.FromSeconds(second), "lol");

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

        [RelayCommand]
        private async Task Replay()
        {
            audioEngine.Replay();
        }

        
        private async Task SaveProject()
        {
            //Maybe Stop audioEngine here, and play again if previous state was play 

            if (resourceHandler is FileHandler fileHandler)
            {
                var saveDialog = new FolderBrowserDialog();
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    resourceManager.SaveProject(Project, new FileLocation(saveDialog.SelectedPath), fileHandler);
                }
            }
            else if (resourceHandler is GoogleDriveHandler googleDriveHandler)
            {
                //Todo: save rootFolder in GoogleDriveHandler
                var driveLocation = new GDriveLocation(rootFolder:"LeaRoot", localPath:"", projectName:"");

                resourceManager.SaveProject(Project, driveLocation, googleDriveHandler);
            }
        }

        [RelayCommand]
        private async Task SaveProjectFile()
        {
            resourceHandler = new FileHandler();
            await SaveProject();
        }

        [RelayCommand]
        private async Task SaveProjectGDrive()
        {
            resourceHandler = new GoogleDriveHandler("", "", "", new FileHandler());
            await SaveProject();
        }

        [RelayCommand]
        private async Task Pause()
        {
            audioEngine.Pause();
        }

        [ObservableProperty]
        bool isProjectLoading;

       
        private async Task LoadProject()
        {
            
            Project?.Dispose();
            Project = null;
            IsProjectLoading = true;

            if (resourceHandler == null)
                throw new Exception("ResourceHandler invalid");

            if (resourceHandler is FileHandler fileHandler)
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Project (*.prj)|*.prj"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {

                    var location = new FileLocation(dialog.FileName);

                    Project = await resourceManager.LoadProject(new FileLocation(dialog.FileName), fileHandler);
                }
                else
                {
                    IsProjectLoading = false;
                    return;
                }
            }
            else if (resourceHandler is GoogleDriveHandler googleDriveHandler)
            {
                if (string.IsNullOrEmpty(ProjectName))
                    throw new ArgumentNullException("Project cant be null");
                //TODO: Dynamic Modal window to specify projectname for download

                var driveLocation = new GDriveLocation(rootFolder: "LeaRoot", localPath: "C:/t", projectName: ProjectName);
                Project = await resourceManager.LoadProject(driveLocation, googleDriveHandler);
            }

            if (Project == null)
                throw new Exception("Cant load Project");

            ProjectName = Project.Name;

            audioEngine.MountProject(Project);
            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));

            CreateTrackDTO();
            CreateMarkerDTO();

            //Prevent when user doubleclick, that WPF register as a mouseclick
            await Task.Delay(100);
            IsProjectLoading = false;

        }

        
        [RelayCommand]
        private async Task LoadProjectGDrive()
        {
            resourceHandler = new GoogleDriveHandler("", "", "", new FileHandler());
            var ownerWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

            if (ownerWindow == null)
                throw new NullReferenceException("Cant find Parent window");

            var vm = new LoadProjectViewModel((MainViewModel)ownerWindow.DataContext);

            var projectWindow = new LoadProjectWindow(vm);
            projectWindow.DataContext = vm;
            projectWindow.Owner = ownerWindow;
            projectWindow.ShowDialog();

            if (ProjectName == null)
            {
                return; 
            }

            await LoadProject();

        }

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

                var track = ImportTrackHandler(resourceHandler, fileDialog.FileName);

                Project.AddTrack(track);
                Project.SetTempo(Speed);
                CreateTrackDTO();

                //Check if audioengine is playing while addTrack, when true continue playing
                audioEngine.MountProject(Project);
                audioEngine.AudioJumpToSec(audioEngine.CurrentPosition);
            }
        }
        private Track ImportTrackHandler(IResourceHandler resourceHandler, string path)
        {
            //DatabaseHandler requiere a FileHandler for Importin track from Disk
            Track track = resourceManager.ImportTrack(new FileLocation(path), resourceHandler);

            return track;
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
     
        public List<string>? GetProjectFromGoogleDrive()
        {
            if (resourceHandler is GoogleDriveHandler googleDriveHandler)
            {
                var projectNameList = googleDriveHandler.GetAllProjectsName("LeaRoot");

                return projectNameList.ToList();
            }
            else
                throw new Exception("Handler is not a GoogleDriveHandler");
        }
    }
}