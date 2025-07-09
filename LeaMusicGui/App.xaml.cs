namespace LeaMusicGui
{
    using System.Windows;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.ResourceManager_;
    using LeaMusic.src.Services;
    using LeaMusic.Src.Services;
    using LeaMusicGui.Views.DialogServices;
    using Microsoft.Extensions.DependencyInjection;
    using LeaMusicGui.Views;
    using SkiaSharp;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider m_serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

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