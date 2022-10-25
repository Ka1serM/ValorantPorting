using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Design;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using ValorantPorting.AppUtils;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using ValorantPorting.Export;
using ValorantPorting.Export.Blender;
using ValorantPorting.Services;
using ValorantPorting.Views;
using ValorantPorting.Views.Controls;

namespace ValorantPorting.ViewModels;

public partial class MainViewModel : ObservableObject
{

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StyleImage))]
    [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private AssetSelectorItem currentAsset;

    public ImageSource StyleImage => currentAsset.FullSource;
    public Visibility StyleVisibility => currentAsset is null ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> maps = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> bundles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> dances = new();
    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;

    public EAssetType CurrentAssetType;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;

    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            var loadTime = new Stopwatch();
            loadTime.Start();
            var ArchiveP = "C:\\ValContentEvent\\ShooterGame\\Content\\Paks";
            AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath);
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
        if (ObjectStyle.Count > 0)
        {
            return ObjectStyle[0];
        }

        return null;
    }
    public Tuple<USkeletalMesh,UMaterialInstanceConstant[],UMaterialInstanceConstant[],UStaticMesh> GetSelectedLevels()
    {
        var sant = AppVM.MainVM.CurrentAsset.MainAsset;
        // 
        USkeletalMesh highestMeshUsed = null;
        UMaterialInstanceConstant[] highestWeapMaterialUsed = new UMaterialInstanceConstant[] { };
        UMaterialInstanceConstant[] highestMagMaterialUsed = new UMaterialInstanceConstant[] { };
        UStaticMesh highestMagMeshUsed = null;
        //
        sant.TryGetValue(out UBlueprintGeneratedClass[] levels, "Levels");
        for (int i = 0; i < levels.Length; i++)
        {
            var activeO = levels[i];
            var cdoLo = activeO.ClassDefaultObject.Load();
            UBlueprintGeneratedClass localUob;
            if (cdoLo.TryGetValue(out localUob, "SkinAttachment"))
            {
                var ready = localUob.ClassDefaultObject.Load();
                ready.TryGetValue(out USkeletalMesh localMeshUsed, "Weapon 1P Cosmetic","NewMesh");
                if (localMeshUsed != null)  highestMeshUsed = localMeshUsed;
                ready.TryGetValue(out UMaterialInstanceConstant[] localMatUsed , "1p MaterialOverrides");
                if (localMatUsed != null) highestWeapMaterialUsed = localMatUsed;
                ready.TryGetValue(out UMaterialInstanceConstant[] magOverrides, "1pMagazine MaterialOverrides");
                if (magOverrides != null) highestMagMaterialUsed = magOverrides;
                ready.TryGetValue(out  UStaticMesh magMesh, "Magazine 1P");
                if (magMesh != null) highestMagMeshUsed = magMesh;
            }
        }
        return Tuple.Create(highestMeshUsed,highestWeapMaterialUsed,highestMagMaterialUsed,highestMagMeshUsed);
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
        }
    }

    [RelayCommand]
    public async Task ExportBlender()
    {
        var loadTimez = new Stopwatch();
        loadTimez.Start();
        var data = await ExportData.Create(CurrentAsset.Asset, CurrentAssetType, GetSelectedStyles(),GetSelectedLevels());
        data.Name = currentAsset.DisplayName;
        BlenderService.Send(data, new BlenderExportSettings
        {
            ReorientBones = false
        });
        loadTimez.Stop();
        AppLog.Information($"Finished exporting {data.Name} in {Math.Round(loadTimez.Elapsed.TotalSeconds, 3)}s");
    }

    [RelayCommand]
    public async Task ExportUnreal()
    {

    }

}