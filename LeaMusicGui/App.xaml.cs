namespace LeaMusicGui
{
    using System.Windows;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.ResourceManager_;
    using LeaMusic.src.ResourceManager_.GoogleDrive_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusic.src.Services.ResourceServices_;
    using LeaMusicGui.Views.DialogServices;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider m_serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILocalFileHandler, LocalFileHandler>();
            services.AddSingleton<IGoogleDriveHandler, GoogleDriveHandler>();
            services.AddSingleton<LeaResourceManager>();
            services.AddSingleton<ProjectService>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<TimelineService>();
            services.AddSingleton<AudioEngine>();
            services.AddSingleton<TimelineCalculator>();

            services.AddSingleton<LoopService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<LoadProjectViewModel>();
            services.AddSingleton<ConnectionMonitorService>();
            services.AddSingleton<IProjectSerializer, ProjectJsonSerializer>();
            services.AddSingleton<IWaveformService, WaveformService>();
            services.AddSingleton<IMetadataService, LocalFileMetaDataService>();
            services.AddSingleton<IFileSystemService, LocalFileSystemService>();
            services.AddSingleton<IZipService, ZipService>();
            m_serviceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var mainWindow = new MainWindow
            {
                DataContext = m_serviceProvider.GetRequiredService<MainViewModel>(),
            };

            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}