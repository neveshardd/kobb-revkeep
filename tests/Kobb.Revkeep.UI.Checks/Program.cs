using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Shell;
using Kobb.Revkeep.Core;
using Kobb.Revkeep.UI;

namespace Kobb.Revkeep.UI.Checks;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var application = new Application();
        foreach (var size in new[] { 16, 32, 64 })
        {
            var image = ProductBrand.Icon(size);
            if (image is not DrawingImage { Drawing: DrawingGroup drawing } || !image.IsFrozen || image.Width != size || image.Height != size)
                throw new InvalidOperationException("O ícone perdeu a dimensão lógica ou foi rasterizado antes da exibição.");
            if (drawing.OpacityMask is not ImageBrush { ImageSource: BitmapSource source } || source.PixelWidth < 900)
                throw new InvalidOperationException("O asset original perdeu resolução.");
        }
        var root = Path.Combine(Path.GetTempPath(), "kobb-ui-checks");
        var settings = new ProjectSettings(Path.Combine(root, "Projeto.rvt"), "Projeto residencial", Path.Combine(root, "Projetos"), Path.Combine(root, "Famílias"));
        var saved = false;
        var window = new ConfigurationWindow(settings, _ => saved = true);
        var surface = (FrameworkElement)window.Content;
        Layout(surface);
        var names = new[] { "ProjectNameField", "BackupRootField", "FamilyRootField" };
        foreach (var name in names)
        {
            var field = (TextBox)window.FindName(name);
            var caret = field.GetRectFromCharacterIndex(0, true);
            if (caret.IsEmpty || caret.Height < 10 || caret.Top < 0 || caret.Bottom > field.ActualHeight)
                throw new InvalidOperationException("O texto ou o cursor de edição está cortado.");
            var size = field.RenderSize;
            field.SelectAll();
            Layout(surface);
            if (field.RenderSize != size || field.Cursor != Cursors.IBeam || field.FocusVisualStyle is null)
                throw new InvalidOperationException("O campo perdeu a geometria ou a indicação de edição.");
            var peer = new TextBoxAutomationPeer(field);
            if (string.IsNullOrWhiteSpace(peer.GetName()) || peer.GetPattern(PatternInterface.Value) is null)
                throw new InvalidOperationException("O campo não está disponível para tecnologias assistivas.");
            field.Select(0, 0);
        }
        var bar = (Grid)window.FindName("TitleBar");
        if (bar.TransformToAncestor(surface).Transform(new Point()).Y != 0 || WindowChrome.GetWindowChrome(window)?.CaptionHeight != 0)
            throw new InvalidOperationException("A barra não está colada ao topo.");
        var confirm = (Button)window.FindName("ConfirmButton");
        var projectName = (TextBox)window.FindName("ProjectNameField");
        projectName.Text = "..";
        confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if (saved || !((TextBlock)window.FindName("StatusText")).Text.Contains("nome de projeto válido"))
            throw new InvalidOperationException("A configuração inválida não foi bloqueada e informada.");
        projectName.Text = settings.ProjectName;
        Layout(surface);
        if (args.Length >= 1)
        {
            var drawing = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(drawing, BitmapScalingMode.HighQuality);
            using (var context = drawing.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, 240, 100));
                context.DrawImage(ProductBrand.Icon(16), new Rect(20, 42, 16, 16));
                context.DrawImage(ProductBrand.Icon(32), new Rect(62, 34, 32, 32));
                context.DrawImage(ProductBrand.Icon(32), new Rect(126, 18, 64, 64));
            }
            var bitmap = new RenderTargetBitmap(240, 100, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawing);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(args[0]);
            encoder.Save(stream);
        }
        if (args.Length >= 2)
        {
            var bitmap = new RenderTargetBitmap(668, 530, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(surface);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(args[1]);
            encoder.Save(stream);
        }
        Console.WriteLine("PASS: ícones, texto e cursor visíveis, campos estáveis, barra no topo, automação e validação.");
        application.Shutdown();
    }

    private static void Layout(FrameworkElement surface)
    {
        surface.Measure(new Size(668, 530));
        surface.Arrange(new Rect(0, 0, 668, 530));
        surface.UpdateLayout();
        surface.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        surface.UpdateLayout();
    }
}
