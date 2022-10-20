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
        UBlueprintGeneratedClass[] van;
        USkeletalMesh local_mesh_used;
        UMaterialInstanceConstant[] local_mat_used;
        UMaterialInstanceConstant[] mag_overrides;
        UStaticMesh mag_mesh;
        // 
        USkeletalMesh Vlocal_mesh_used = null;
        UMaterialInstanceConstant[] Vlocal_mat_used = new UMaterialInstanceConstant[] { };
        UMaterialInstanceConstant[] Vmag_overrides = new UMaterialInstanceConstant[] { };
        UStaticMesh Vmag_mesh = null;
        //
        var nab = sant.TryGetValue(out van, "Levels");
        if (!nab) return null;
        for (int i = 0; i < van.Length; i++)
        {
            var active_o = van[i];
            var cdo_lo = active_o.ClassDefaultObject.Load();
            UBlueprintGeneratedClass local_uob;
            if (cdo_lo.TryGetValue(out local_uob, "SkinAttachment"))
            {
                var ready = local_uob.ClassDefaultObject.Load();
                ready.TryGetValue(out local_mesh_used, "Weapon 1P Cosmetic","NewMesh");
                if (local_mesh_used != null) Vlocal_mesh_used = local_mesh_used;
                ready.TryGetValue(out local_mat_used, "1p MaterialOverrides");
                if (local_mat_used != null) Vlocal_mat_used = local_mat_used;
                ready.TryGetValue(out mag_overrides, "1pMagazine MaterialOverrides");
                if (mag_overrides != null) Vmag_overrides = mag_overrides;
                ready.TryGetValue(out mag_mesh, "Magazine 1P");
                if (mag_mesh != null) Vmag_mesh = mag_mesh;
            }
        }
        return Tuple.Create(Vlocal_mesh_used,Vlocal_mat_used,Vmag_overrides,Vmag_mesh);
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