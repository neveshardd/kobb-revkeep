using System.Windows;
using Kobb.Revkeep.Core;
using Kobb.Revkeep.UI;

namespace Kobb.Revkeep.Preview;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var root = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KOBB", "Revkeep");
        var settings = new ProjectSettings(System.IO.Path.Combine(root, "Projeto.rvt"), "Projeto residencial", root, root);
        var application = new Application();
        var window = new ConfigurationWindow(settings, _ => MessageBox.Show("Configuração validada. Esta prévia não executa backups.", "KOBB Revkeep"));
        application.MainWindow = window;
        window.ShowDialog();
    }
}
