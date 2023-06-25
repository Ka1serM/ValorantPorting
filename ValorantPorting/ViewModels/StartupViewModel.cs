using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Versions;
using ValorantPorting.AppUtils;
using Newtonsoft.Json;
using YamlDotNet;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ValorantPorting.ViewModels;

public class StartupViewModel : ObservableObject
{
    public string ArchivePath
    {
        get => AppSettings.Current.ArchivePath;
        set
        {
            AppSettings.Current.ArchivePath = value;
            OnPropertyChanged();
        }
    }
    
    public ELanguage Language
    {
        get => AppSettings.Current.Language;
        set
        {
            AppSettings.Current.Language = value;
            OnPropertyChanged();
        }
    }
    
    public void CheckForInstallation()
    {
        string resultJ = "";
        foreach (var drive in DriveInfo.GetDrives())
        {
            var launcherInstalledPath = $"{drive.Name}ProgramData\\Riot Games\\Metadata\\valorant.live\\valorant.live.product_settings.yaml";
            if (!File.Exists(launcherInstalledPath)) continue;
            var ymlContents = File.ReadAllText(launcherInstalledPath);
            Regex cusRegex = new Regex("product_install_full_path: .*");
            resultJ = cusRegex.Match(ymlContents).Value.Replace("product_install_full_path: ", string.Empty).Replace("\"", "");
        }
        ArchivePath = resultJ.Replace("\r","") + "/ShooterGame/Content/Paks";
        Log.Information("Detected VALORANT Installation at {0}", ArchivePath);
    }
    
}