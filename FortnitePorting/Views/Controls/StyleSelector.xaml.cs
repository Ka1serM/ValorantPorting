using System;
using System.Linq;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.ViewModels;
using SharpGLTF.Schema2;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.Export;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelector
{
    public string ChannelName;
    public ObservableCollection<StyleSelectorItem> TCollection;


    public StyleSelector(UObject[] Chromas)
    {
        InitializeComponent();
        DataContext = this;
        //foreach (UBlueprintGeneratedClass style in Chromas)
        //{
            //Options.Items.Add(MakeSelectedItem(style).Result);
            // add StyleToUse to list
        //}

        var variantVm = new VariantHandlerViewModel();
        base.DataContext = variantVm;
    }
    public async Task<StyleSelectorItem> MakeSelectedItem(UBlueprintGeneratedClass Chroma)
    
    {
        var style_asset = Chroma.ClassDefaultObject.Load();
        var uiDataObject = style_asset.GetOrDefault("UIData", new UObject());
        var bpChannel = (UBlueprintGeneratedClass)uiDataObject;
        var uiData = await ExportData.CreateUIData(bpChannel);
        uiData.TryGetValue(out FText DName, "DisplayName");
        uiData.TryGetValue(out UTexture2D image, "Swatch");
        return new StyleSelectorItem(style_asset, uiDataObject, image);
    }
    private void StyleSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is null) return;
        var selected = (StyleSelectorItem)listBox.SelectedItem;
        //AppVM.MainVM.CurrentAsset = selected;
    }
    
    private void DrawBackground(SKCanvas canvas, int size)
    {
        SKShader BackgroundShader(params SKColor[] colors)
        {;
            return SKShader.CreateRadialGradient(new SKPoint(size / 2f, size / 2f), size / 5 * 4, colors,
                SKShaderTileMode.Clamp);
        }

        canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
        {
            Shader = BackgroundShader(SKColor.Parse("#50C8FF"), SKColor.Parse("#1B7BCF"))
        });
    }


}