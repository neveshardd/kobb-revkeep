using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace Kobb.Revkeep;

public sealed class AutomaticConfiguration : IDisposable
{
    private readonly UIControlledApplication application;
    private readonly ProjectConfiguration configuration;
    private readonly Dictionary<Document, string> visited = [];
    private bool showing;

    public AutomaticConfiguration(UIControlledApplication application, ProjectConfiguration configuration)
    {
        this.application = application;
        this.configuration = configuration;
        application.Idling += OnIdle;
    }

    private void OnIdle(object? sender, IdlingEventArgs e)
    {
        if (showing || sender is not UIApplication ui) return;
        foreach (var closed in visited.Keys.Where(document => !document.IsValidObject).ToArray()) visited.Remove(closed);
        var document = ui.ActiveUIDocument?.Document;
        if (document is null || !ProjectConfiguration.Supports(document) || document.IsModifiable || document.IsReadOnly) return;
        if (visited.TryGetValue(document, out var path) && string.Equals(path, document.PathName, StringComparison.OrdinalIgnoreCase)) return;
        visited[document] = document.PathName;
        if (configuration.IsConfigured(document)) return;
        showing = true;
        try { configuration.Show(ui, document, true); }
        catch (Exception exception) { PluginDiagnostics.Record(document.PathName, exception); }
        finally { showing = false; }
    }

    public void Dispose()
    {
        application.Idling -= OnIdle;
        visited.Clear();
    }
}
