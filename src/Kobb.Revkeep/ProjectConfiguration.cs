using System.IO;
using System.Text.Json;
using System.Windows.Interop;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Kobb.Revkeep.Core;
using Kobb.Revkeep.UI;

namespace Kobb.Revkeep;

public sealed class ProjectConfiguration(SettingsStore store)
{
    public static bool Supports(Document document) => document.IsValidObject && !document.IsFamilyDocument && !document.IsLinked
        && !document.IsModelInCloud && !document.IsWorkshared && File.Exists(document.PathName);

    public bool IsConfigured(Document document) => Load(document) is not null;

    public bool Show(UIApplication application, Document document, bool automatic = false)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KOBB", "Revkeep");
        var settings = Load(document) ?? new ProjectSettings(document.PathName, Path.GetFileNameWithoutExtension(document.PathName), root, root);
        var notice = automatic ? "Escolha as pastas para ativar os backups automáticos deste projeto." : null;
        var window = new ConfigurationWindow(settings, Save, notice);
        new WindowInteropHelper(window).Owner = application.MainWindowHandle;
        return window.ShowDialog() == true;
    }

    private void Save(ProjectSettings settings)
    {
        BackupLayout.From(settings).EnsureExists();
        store.Save(settings);
    }

    private ProjectSettings? Load(Document document)
    {
        try { return store.Load(document.PathName); }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
