namespace LeaMusicGui
{
    using System.Windows;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;
    using LeaMusic.src.Services.ResourceServices_.GoogleDrive_;
    using LeaMusicGui.Views.DialogServices;
    using Microsoft.Extensions.DependencyInjection;
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.AudioEngine_;
    using LeaMusicGui.Controls.TrackControl_;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services;

        public App()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IGoogleContext, GoogleContext>();
            services.AddTransient<TrackControlViewModel>();
            services.AddSingleton<ILocalFileHandler, LocalFileHandler>();
            services.AddSingleton<IGoogleDriveHandler, GoogleDriveHandler>();
            services.AddSingleton<IResourceManager, LeaResourceManager>();
            services.AddSingleton<ProjectService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<TimelineService>();
            services.AddSingleton<IMixer, NaudioMixer>();
            services.AddSingleton<IAudioPlayer, WaveOutAudioPlayer>();
            services.AddSingleton<AudioEngine>();
            services.AddSingleton<TimelineCalculator>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<LoopService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<LoadProjectViewModel>();
            services.AddSingleton<ConnectionMonitorService>();
            services.AddSingleton<IProjectSerializer, ProjectJsonSerializer>();
            services.AddSingleton<IWaveformService, WaveformService>();
            services.AddSingleton<IMetadataService, LocalFileMetaDataService>();
            services.AddSingleton<GoogleDriveMetaDataService>();
            services.AddSingleton<IFileSystemService, LocalFileSystemService>();
            services.AddSingleton<IZipService, ZipService>();
            services.AddSingleton<IBinaryWriter, WaveformBinaryWriter>();

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