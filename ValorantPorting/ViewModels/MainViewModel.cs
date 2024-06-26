﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using ValorantPorting.AppUtils;
using ValorantPorting.Export;
using ValorantPorting.Export.Blender;
using ValorantPorting.Services;
using ValorantPorting.Views;
using ValorantPorting.Views.Controls;
using StyleSelector = ValorantPorting.Views.Controls.StyleSelector;

namespace ValorantPorting.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StyleImage))]
    [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private IExportableAsset? currentAsset;

    public EAssetType CurrentAssetType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StyleImage))]
    [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private List<IExportableAsset> extendedAssets = new();

    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;
    
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gunbuddies = new();

    public ImageSource StyleImage => currentAsset?.FullSource;
    public Visibility StyleVisibility => currentAsset is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;

    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            var loadTime = new Stopwatch();
            loadTime.Start();

            AppVM.CUE4ParseVM =
                new CUE4ParseViewModel(AppSettings.Current.ArchivePath, AppSettings.Current.InstallType);
            await AppVM.CUE4ParseVM.Initialize();
            loadTime.Stop();

            AppLog.Information($"Finished loading game files in {Math.Round(loadTime.Elapsed.TotalSeconds, 3)}s");
            IsReady = true;

            AppVM.AssetHandlerVM = new AssetHandlerViewModel();
            await AppVM.AssetHandlerVM.Initialize();
        });
    }

    public UObject GetSelectedStyles()
    {
        var ObjectStyle = Styles.Select(style =>
            ((StyleSelectorItem)style.Options.Items[style.Options.SelectedIndex]).ObjectData).ToList();
        if (ObjectStyle.Count > 0) return ObjectStyle[0];
        return null;
    }

    [RelayCommand]
    public void Menu(string parameter)
    {
        switch (parameter)
        {
            case "Open_Assets":
                AppHelper.Launch(App.AssetsFolder.FullName);
                break;
            case "Open_Data":
                AppHelper.Launch(App.DataFolder.FullName);
                break;
            case "Open_Exports":
                AppHelper.Launch(App.ExportsFolder.FullName);
                break;
            case "File_Restart":
                AppVM.Restart();
                break;
            case "File_Quit":
                AppVM.Quit();
                break;
            case "Settings_Options":
                AppHelper.OpenWindow<SettingsView>();
                break;
            case "Settings_Startup":
                AppHelper.OpenWindow<StartupView>();
                break;
            case "Tools_Update":
                // TODO
                break;
            case "Help_Discord":
                AppHelper.Launch(Globals.DISCORD_URL);
                break;
            case "Help_GitHub":
                AppHelper.Launch(Globals.GITHUB_URL);
                break;
            case "Help_About":
                // TODO
                break;
            case "Settings_Blender":
                AppHelper.OpenWindow<BlenderView>();
                break;
        }
    }

    [RelayCommand]
    public async Task ExportBlender()
    {
        var loadTimez = new Stopwatch();
        loadTimez.Start();
        var data = await ExportData.Create(CurrentAsset.Asset, CurrentAssetType, GetSelectedStyles());
        data.Name = currentAsset.DisplayName;
        var reorient = CurrentAssetType != EAssetType.Weapon;
        BlenderService.Send(data, new BlenderExportSettings
        {
            ReorientBones = reorient
        });
        loadTimez.Stop();
        AppLog.Information(
            $"Finished exporting {data.Name} to BLENDER in {Math.Round(loadTimez.Elapsed.TotalSeconds, 3)}s");
    }

    [RelayCommand]
    public async Task ExportUnreal()
    {
        var loadTimez = new Stopwatch();
        loadTimez.Start();
        var data = await ExportData.Create(CurrentAsset.Asset, CurrentAssetType, GetSelectedStyles());
        data.Name = currentAsset.DisplayName;
        UnrealService.Send(data);
        loadTimez.Stop();
        AppLog.Information(
            $"Finished exporting {data.Name} to UNREAL in {Math.Round(loadTimez.Elapsed.TotalSeconds, 3)}s");
    }
}