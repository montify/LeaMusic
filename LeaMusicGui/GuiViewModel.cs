using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeaMusic;
using LeaMusic.src;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace LeaMusicGui
{

    public partial class TrackDTO : ObservableObject
    {
        [ObservableProperty]
        private ReadOnlyMemory<float> _waveform;

        public int TrackID { get; set; }

    }

    public partial class MarkerWrapper : ObservableObject
    {
        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private Marker marker;

        [ObservableProperty]
        public bool visible = true;
    }

    public partial class GuiViewModel : ObservableObject
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

        public Project Project { get; private set; }
        public ObservableCollection<TrackDTO> WaveformWrappers { get; set; } = new ObservableCollection<TrackDTO>();
        public ObservableCollection<MarkerWrapper> TestMarkers { get; set; } = new ObservableCollection<MarkerWrapper>();

       
        public GuiViewModel()
        {
            audioEngine = new AudioEngine();
            TimelineService = new TimelineService(audioEngine);

            //Project = new Project("HALLO");

            //Project.AddTrack(new Track("C:/t/Hairflip.mp3"));
            //Project.AddTrack(new Track("C:/t/Hairflip.mp3"));
            //audioEngine.LoadProject(Project);

            

            //Create Empty Project for StartUp
            Project = Project.CreateEmptyProject("TEST");
            Project.AddTrack(new Track("C:\\Users\\alexlapi\\Desktop\\v1\\AudioFiles\\1_Josh Smith - Wheres My Baby(No Other).mp3"));
            audioEngine.LoadProject(Project);
            CreateTrackDTO();
            ////Init WaveformLoad Startup
            //CreateWaveFormWrapper();
            //CreateMarkerWrapper();

            //audioEngine.AddMarker(TimeSpan.FromSeconds(11), "TestMarker 1");
            //audioEngine.AddMarker(TimeSpan.FromSeconds(audioEngine.Project.Duration.TotalSeconds/2), "TestMarker 2");



            audioEngine.OnUpdate += AudioEngine_OnPlayHeadChange;
            audioEngine.OnProgressChange += AudioEngine_OnProgressChange;
            audioEngine.OnLoopChange += AudioEngine_OnLoopChange;

       
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
            {
                trackDTOList.Add(TimelineService.RequestSample(i, 1200));
            }

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
        private void CreateMarkerWrapper()
        {
            TestMarkers.Clear();
            for (int i = 0; i < audioEngine.Project.Markers.Count; i++)
            {
                var marker = audioEngine.Project.Markers[i];

                var w = new MarkerWrapper();
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

        private bool IsMarkerVisible(MarkerWrapper testMarker)
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
            ProgressInPercentage = CalculateSecRelativeToViewWindowPercentage(positionInSec, audioEngine.ViewStartTime, audioEngine.ViewDuration);

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
            audioEngine.ZoomWaveForm(value);

            //var trackDTOList = new List<Memory<float>>();

            //for (int i = 0; i < Project.Tracks.Count; i++)
            //{
            //    trackDTOList.Add(TimelineService.RequestSample(i, RenderWidth));
            //}

            UpdateWaveform(RenderWidth);

            //UpdateTrackDTO(trackDTOList);
            //CreateTrackDTO();

        }
      
        public void SetTextMarker()
        {
            audioEngine.AddMarker(audioEngine.CurrentPosition, "MAKER CREATE WITH T");
            CreateMarkerWrapper();
        }

        partial void OnSpeedChanged(double value)
        {
            audioEngine.ChangeSpeed(value);
        }

        [RelayCommand]
        private async Task Pause()
        {
            audioEngine.Pause();

        }

        [RelayCommand]
        private async Task Replay()
        {
           audioEngine.Replay();
        }


        [RelayCommand]
        private async Task SaveProject()
        {
            var saveDialog = new FolderBrowserDialog();

            if(saveDialog.ShowDialog() == DialogResult.OK)
            {
                //Maybe Stop audioEngine here
                audioEngine.SaveProject(saveDialog.SelectedPath);

                Debug.WriteLine("PROJECT SAVED");
            }
        }

        [RelayCommand]
        private async Task LoadProject()
        {

            var dialog = new OpenFileDialog
            {
                Filter = "Project (*.prj)|*.prj"
            };

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                
                var project = AudioEngine.LoadProjectFromFile(dialog.FileName);
                audioEngine.LoadProject(project);
                Project = project;

                audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(0));
                

                CreateTrackDTO();
                CreateMarkerWrapper();

                Debug.WriteLine("PROJECT LOADED");
            }
        }

       
        public void MouseClick(Point p, double width)
        {
            
            var second = ConvertPixelToSecond(p.X, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)width);
            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(second));

            audioEngine.AddMarker(TimeSpan.FromSeconds(second), "lol");

            CreateMarkerWrapper();
        }

        public void LoopSelection(double startPixel, double endPixel, double renderWidth)
        {
            var startSec = ConvertPixelToSecond(startPixel, audioEngine.ViewStartTime.TotalSeconds, audioEngine.ViewDuration.TotalSeconds, (int)renderWidth);
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
        private async Task AddTrack()
        {
            var fileDialog = new OpenFileDialog();

            if(fileDialog.ShowDialog() == DialogResult.OK)
            {
                audioEngine.Stop();

                var track = new Track(fileDialog.FileName);

                track.JumpToPosition(audioEngine.CurrentPosition);

                Project.AddTrack(track);
                
                CreateTrackDTO();
              
                //Check if audioengine is playing while addTrack, when true continue playing
                audioEngine.LoadProject(Project);
                audioEngine.AudioJumpToSec(audioEngine.CurrentPosition);

                audioEngine.Play();
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
                var ass = audioEngine.Project.Tracks.Where(t => t.ID == wrapper.TrackID).First();
                audioEngine.Project.Tracks.Remove(ass);
                WaveformWrappers.Remove(wrapper);

               // CreateTrackDTO();
                audioEngine.ReloadMixerInputs();

            }
        }

        [RelayCommand]
        private async Task JumpToSec()
        {
            audioEngine.AudioJumpToSec(TimeSpan.FromSeconds(JumpToPositionInSec));
        }
    }
}