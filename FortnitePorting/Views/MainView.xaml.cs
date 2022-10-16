using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Controls;
using FortnitePorting.Export;
using FortnitePorting.Export.Blender;
using FortnitePorting.Views.Extensions;
using Serilog;
using StyleSelector = FortnitePorting.Views.Controls.StyleSelector;
using SharpGLTF.Schema2;

namespace FortnitePorting.Views;

public partial class MainView
{
    public MainView()
    {
        InitializeComponent();
        AppVM.MainVM = new MainViewModel();
        DataContext = AppVM.MainVM;

        AppLog.Logger = LoggerRtb;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AppSettings.Current.ArchivePath))
        {
            AppHelper.OpenWindow<StartupView>();
            return;
        }
        
        await AppVM.MainVM.Initialize();
    }

    private async void OnAssetTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl) return;
        if (AppVM.AssetHandlerVM is null) return;
        
        var assetType = (EAssetType) tabControl.SelectedIndex;
        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
        {
            if (handlerType == assetType)
            {
                handlerData.PauseState.Unpause();
            }
            else
            {
                handlerData.PauseState.Pause();
            }
        }
        
        if (!handlers[assetType].HasStarted)
        {
            await handlers[assetType].Execute();
        }
        
        DiscordService.Update(assetType);
        AppVM.MainVM.CurrentAssetType = assetType;
    }

    private async void OnStyleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;
        Console.WriteLine(selected);
        
    }
    private async void OnVariantSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;
        Console.WriteLine(selected);
        
    }
    private async void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;
        if (selected.IsRandom)
        {
            listBox.SelectedIndex = App.RandomGenerator.Next(0, listBox.Items.Count);
            return;
        }

        AppVM.MainVM.CurrentAsset = selected;
        StyleList.Children.Clear();

        var styles = selected.Asset.GetOrDefault("Chromas", Array.Empty<UObject>());
        foreach (UBlueprintGeneratedClass style in styles)
        {
            var style_asset = style.ClassDefaultObject.Load();
            var channel = style_asset.GetOrDefault("UIData", new UObject());
            var bpChannel = (UBlueprintGeneratedClass)channel;
            // create return from await
            
            var UIData = await ExportData.CreateUIData(bpChannel);
            UIData.TryGetValue(out FText DName, "DisplayName");
            UIData.TryGetValue(out UTexture2D image, "Swatch");
            var styleSelector = new StyleSelector(style_asset, UIData, image);
            StyleList.Children.Add(styleSelector);
        }
    }

    private void StupidIdiotBadScroll(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        switch (e.Delta)
        {
            case < 0:
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 88);
                break;
            case > 0:
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 88);
                break;
        }
    }
}