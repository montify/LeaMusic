using LeaMusic.src.AudioEngine_;
using LeaMusic.src.ResourceManager_;
using LeaMusicGui.Views.DialogServices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace LeaMusicGui;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    public App()
    {
        var services = new ServiceCollection();

        services.AddSingleton<LeaResourceManager>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<TimelineService>();
        services.AddSingleton<AudioEngine>();
        services.AddSingleton<IDialogService, DialogService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
        };

        mainWindow.Show();

        base.OnStartup(e);
    }
}

