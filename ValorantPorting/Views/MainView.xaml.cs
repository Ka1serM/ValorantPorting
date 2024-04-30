using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;
using ValorantPorting.AppUtils;
using ValorantPorting.Export;
using ValorantPorting.Services;
using ValorantPorting.ViewModels;
using ValorantPorting.Views.Controls;
using StyleSelector = ValorantPorting.Views.Controls.StyleSelector;

namespace ValorantPorting.Views;

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

        var assetType = (EAssetType)tabControl.SelectedIndex;
        var handlers = AppVM.AssetHandlerVM.Handlers;
        foreach (var (handlerType, handlerData) in handlers)
            if (handlerType == assetType)
                handlerData.PauseState.Unpause();
            else
                handlerData.PauseState.Pause();

        if (!handlers[assetType].HasStarted) await handlers[assetType].Execute();

        DiscordService.Update(assetType);
        AppVM.MainVM.CurrentAssetType = assetType;
    }

    private async void OnStyleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchBox = (TextBox)sender;
        foreach (var tab in AssetControls.Items.OfType<TabItem>())
        {
            var listBox = (ListBox)tab.Content;
            listBox.Items.Filter = o => ((AssetSelectorItem)o).Match(searchBox.Text);
            listBox.Items.Refresh();
        }
    }

    private async void OnAssetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (AssetSelectorItem)listBox.SelectedItem;

        AppVM.MainVM.CurrentAsset = selected;
        AppVM.MainVM.Styles.Clear();
        var chromas = selected.MainAsset.GetOrDefault("Chromas", Array.Empty<UObject>());
        var styles = new List<UObject>();
        foreach (UBlueprintGeneratedClass style in chromas)
        {
            if (style == null) continue;

            var cdo = style.ClassDefaultObject.Load();
            var channel = cdo.GetOrDefault("UIData", new UObject());
            var bpChannel = (UBlueprintGeneratedClass)channel;
            var uiData = await ExportData.CreateUiData(bpChannel);
            styles.Add(uiData);
        }

        var styleSelector = new StyleSelector(styles.ToArray(), chromas);
        if (styleSelector.Options.Items.Count == 0) return;
        AppVM.MainVM.Styles.Add(styleSelector);
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