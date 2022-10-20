using System.Windows;
using ValorantPorting.AppUtils;
using ValorantPorting.Services;
using ValorantPorting.ViewModels;

namespace ValorantPorting.Views;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        AppVM.SettingsVM = new SettingsViewModel();
        DataContext = AppVM.SettingsVM;
    }

    private void OnClickOK(object sender, RoutedEventArgs e)
    {
        if (AppVM.SettingsVM.IsRestartRequired)
        {
            AppVM.RestartWithMessage("A restart is required.", "An option has been changed that requires a restart to take effect.");
        }

        if (AppVM.SettingsVM.DiscordRPC == ERichPresenceAccess.Always)
        {
            DiscordService.Initialize();
        }
        else
        {
            DiscordService.DeInitialize();
        }
        Close();
    }

    private void OnClickInstallation(object sender, RoutedEventArgs e)
    {
        if (AppHelper.TrySelectFolder(out var path))
        {
            AppVM.SettingsVM.ArchivePath = path;
        }
    }
}