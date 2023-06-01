using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Design;
using System.Windows.Input;
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
using ValorantPorting.Export.Unreal;
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

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(StyleImage))] 
    [NotifyPropertyChangedFor(nameof(StyleVisibility))]
    private List<IExportableAsset> extendedAssets = new();
    
    public ImageSource StyleImage => currentAsset.FullSource;
    public Visibility StyleVisibility => currentAsset is null ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty] private ObservableCollection<AssetSelectorItem> outfits = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> maps = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> bundles = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> weapons = new();
    [ObservableProperty] private ObservableCollection<AssetSelectorItem> gunbuddies = new();
    [ObservableProperty] private ObservableCollection<StyleSelector> styles = new();
    [ObservableProperty] private SuppressibleObservableCollection<TreeItem> meshes = new();
    [ObservableProperty] private SuppressibleObservableCollection<AssetItem> assets = new();

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isReady;

    public EAssetType CurrentAssetType;

    public Visibility LoadingVisibility => IsReady ? Visibility.Collapsed : Visibility.Visible;
    
    private static readonly string[] AllowedMeshTypes =
    {
        "Skeleton",
        "SkeletalMesh",
        "StaticMesh"
    };

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
        AppLog.Information($"Finished exporting {data.Name} to BLENDER in {Math.Round(loadTimez.Elapsed.TotalSeconds, 3)}s");
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
        AppLog.Information($"Finished exporting {data.Name} to UNREAL in {Math.Round(loadTimez.Elapsed.TotalSeconds, 3)}s");
    }
    
    public async Task SetupMeshSelection(string path)
    {
        ExtendedAssets.Clear();
        //MeshPreviews.Clear();
        //OptionTabText = "SELECTED MESHES";
        
        var meshObject = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(path);
        CurrentAsset = AllowedMeshTypes.Contains(meshObject.ExportType) ? new MeshAssetItem(meshObject) : null;
    }
    
    // public async Task SetupMeshSelection(AssetItem[] extendedItems)
    // {
    //     ExtendedAssets.Clear();
    //     MeshPreviews.Clear();
    //     OptionTabText = "SELECTED MESHES";
    //     
    //     var index = 0;
    //     var validMeshSelected = false;
    //     foreach (var item in extendedItems)
    //     {
    //         var meshObject = await AppVM.CUE4ParseVM.Provider.TryLoadObjectAsync(item.PathWithoutExtension);
    //         meshObject ??= await Task.Run(() =>
    //         {
    //             return AppVM.CUE4ParseVM.Provider.LoadAllObjects(item.PathWithoutExtension).FirstOrDefault(x => AllowedMeshTypes.Contains(x.ExportType));
    //         });
    //         if (meshObject is null) continue;
    //         
    //         if (AllowedMeshTypes.Contains(meshObject.ExportType))
    //         {
    //             var meshItem = new MeshAssetItem(meshObject);
    //             if (index == 0) CurrentAsset = meshItem;
    //             ExtendedAssets.Add(meshItem);
    //             index++;
    //             validMeshSelected = true;
    //         }
    //     }
    //     
    //     MeshPreviews.Add(new StyleSelector(ExtendedAssets));
    //   
    //     if (!validMeshSelected) CurrentAsset = null;
    // }
    
    private async void AssetFolderTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var treeView = (TreeView) sender;
        var treeItem = (TreeItem) treeView.SelectedItem;
        if (treeItem is null) return;
        if (treeItem.AssetType == ETreeItemType.Folder) return;

        await AppVM.MainVM.SetupMeshSelection(treeItem.FullPath!);
    }
    
    //
    // private async void AssetFlatView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    // {
    //     var listBox = (ListBox) sender;
    //     var selectedItem = (AssetItem) listBox.SelectedItem;
    //     if (selectedItem is null) return;
    //     
    //     await AppVM.MainVM.SetupMeshSelection(listBox.SelectedItems.OfType<AssetItem>().ToArray());
    // }

    private void AssetFlatView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var listBox = (ListBox) sender;
        var selectedItem = (AssetItem) listBox.SelectedItem;
        if (selectedItem is null) return;
        
        JumpToAsset(selectedItem.PathWithoutExtension);
    }
    
    private void JumpToAsset(string directory)
    {
        var children = AppVM.MainVM.Meshes;

        var i = 0;
        var folders = directory.Split('/');
        while (true)
        {
            foreach (var folder in children)
            {
                if (!folder.Header.Equals(folders[i], StringComparison.OrdinalIgnoreCase))
                    continue;

                if (folder.AssetType == ETreeItemType.Asset)
                {
                    folder.IsSelected = true;
                    return;
                }

                folder.IsExpanded = true;
                children = folder.Children;
                break;
            }

            i++;
            if (children.Count == 0) break;
        }
    }
}