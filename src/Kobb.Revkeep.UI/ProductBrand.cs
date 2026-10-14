using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kobb.Revkeep.UI;

public static class ProductBrand
{
    public static ImageSource Icon(int size)
    {
        var source = new BitmapImage(new Uri("pack://application:,,,/Kobb.Revkeep.UI;component/Assets/product.png"));
        source.Freeze();
        var drawing = new DrawingGroup
        {
            OpacityMask = new ImageBrush(source) { Stretch = Stretch.Uniform }
        };
        drawing.Children.Add(new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(0, 0, size, size))));
        RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.HighQuality);
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
