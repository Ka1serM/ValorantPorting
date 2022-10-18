using System;
using System.Collections.Generic;
using System.Linq;
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

    }
    private async void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;
        if (selected.aType == EAssetType.Weapon)
        {
        }

        AppVM.MainVM.CurrentAsset = selected;
        AppVM.MainVM.Styles.Clear();
        var styles = selected.MainAsset.GetOrDefault("Chromas", Array.Empty<UObject>());
        var NStyles = new List<UObject>();
        foreach (UBlueprintGeneratedClass VARIABLE in styles)
        {
            if (VARIABLE == null)
            {
                continue;
            }
            var CDO = VARIABLE.ClassDefaultObject.Load();
            var channel = CDO.GetOrDefault("UIData", new UObject());
            var bpChannel = (UBlueprintGeneratedClass)channel;
            var UIData = await ExportData.CreateUIData(bpChannel);
            NStyles.Add(UIData);
        }
        var styleSelector = new StyleSelector(NStyles.ToArray(),styles);
        if (styleSelector.Options.Items.Count == 0) return;
        AppVM.MainVM.Styles.Add(styleSelector);
        //foreach (UBlueprintGeneratedClass  style in styles)
        //{
            //var cdo = style.ClassDefaultObject.Load();
            //var ui_data_get = cdo.GetOrDefault("UIData", new UObject());
            //var ui_data_bp = (UBlueprintGeneratedClass)ui_data_get;
            //var UIData = await ExportData.CreateUIData(ui_data_bp);
            //UIData.TryGetValue(out FText channel, "DisplayName");
            //var styleSelector = new StyleSelector(UIData);
            //if (styleSelector.Options.Items.Count == 0) continue;
            //AppVM.MainVM.Styles.Add(styleSelector);
        //}
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