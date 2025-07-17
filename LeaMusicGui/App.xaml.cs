namespace LeaMusicGui
{
    using System.Windows;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;
    using LeaMusic.src.Services.ResourceServices_.GoogleDrive_;
    using LeaMusicGui.Controls.TrackControl_;
    using LeaMusicGui.Views.DialogServices;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services;

        public App()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LoadProjectViewModel>();
            services.AddTransient<TrackControlViewModel>();

            services.AddSingleton<IGoogleContext, GoogleContext>();
            services.AddSingleton<ILocalFileHandler, LocalFileHandler>();
            services.AddSingleton<IGoogleDriveHandler, GoogleDriveHandler>();
            services.AddSingleton<IResourceManager, LeaResourceManager>();
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<ITimelineService, TimelineService>();
            services.AddSingleton<IMixer, NaudioMixer>();
            services.AddSingleton<IAudioPlayer, WaveOutAudioPlayer>();
            services.AddSingleton<ITimelineCalculator, TimelineCalculator>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<ILoopService, LoopService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IConnectionMonitorService, ConnectionMonitorService>();
            services.AddSingleton<IProjectSerializer, ProjectJsonSerializer>();
            services.AddSingleton<IWaveformService, WaveformService>();
            services.AddSingleton<ILocalFileMetaDataService, LocalFileMetaDataService>();
            services.AddSingleton<IGoogleDriveMetaDataService, GoogleDriveMetaDataService>();
            services.AddSingleton<IFileSystemService, LocalFileSystemService>();
            services.AddSingleton<IZipService, ZipService>();
            services.AddSingleton<IBinaryWriter, WaveformBinaryWriter>();
            services.AddSingleton<ISnappingService, SnappingService>();
            services.AddSingleton<ITrackVolumeService, TrackVolumeService>();
            services.AddSingleton<IAudioEngine, AudioEngine>();
            services.AddSingleton<IProjectProvider>(sp => (IProjectProvider)sp.GetRequiredService<IAudioEngine>());
            services.AddSingleton<IViewWindowProvider>(sp => (IViewWindowProvider)sp.GetRequiredService<IAudioEngine>());

            Services = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>(),
            };

            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}