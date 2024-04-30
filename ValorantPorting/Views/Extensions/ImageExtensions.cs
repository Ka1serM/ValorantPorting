using System.Windows.Media.Imaging;
using SkiaSharp;

namespace ValorantPorting.Views.Extensions;

public static class ImageExtensions
{
    public static BitmapSource ToBitmapSource(this SKBitmap bitmap)
    {
        var source = new BitmapImage { CacheOption = BitmapCacheOption.OnDemand };
        source.BeginInit();
        source.StreamSource = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        source.EndInit();
        return source;
    }
}