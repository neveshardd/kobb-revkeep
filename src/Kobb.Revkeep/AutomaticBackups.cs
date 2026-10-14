using System.IO;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Kobb.Revkeep.Core;

namespace Kobb.Revkeep;

public sealed class AutomaticBackups : IDisposable
{
    private readonly UIControlledApplication application;
    private readonly SettingsStore store = SettingsStore.ForCurrentUser();
    private readonly BackupFiles files = new();
    private readonly Queue<PendingBackup> pending = new();

    public AutomaticBackups(UIControlledApplication application)
    {
        this.application = application;
        application.ControlledApplication.DocumentSaving += BeforeSave;
        application.ControlledApplication.DocumentSaved += AfterSave;
        application.ControlledApplication.DocumentSavedAs += AfterSaveAs;
        application.Idling += OnIdle;
    }

    private void BeforeSave(object? sender, DocumentSavingEventArgs e) => Capture(e.Document, "antes", false);
    private void AfterSave(object? sender, DocumentSavedEventArgs e)
    {
        if (e.Status == RevitAPIEventStatus.Succeeded) Capture(e.Document, "depois", true);
    }
    private void AfterSaveAs(object? sender, DocumentSavedAsEventArgs e)
    {
        if (e.Status == RevitAPIEventStatus.Succeeded) Capture(e.Document, "depois", true);
    }

    private void Capture(Document document, string phase, bool relocate)
    {
        if (!document.IsValidObject || document.IsFamilyDocument || document.IsLinked || document.IsWorkshared || document.IsModelInCloud || !File.Exists(document.PathName)) return;
        try
        {
            var settings = store.Load(document.PathName);
            if (settings is null) return;
            var snapshot = files.Capture(settings, phase);
            if (relocate) pending.Enqueue(new PendingBackup(document, settings, snapshot));
            WriteReport(snapshot, "Projeto preservado", [], 0);
        }
        catch (Exception exception) { WriteError(document.PathName, exception); }
    }

    private void OnIdle(object? sender, IdlingEventArgs e)
    {
        if (pending.Count == 0) return;
        var item = pending.Peek();
        if (!item.Document.IsValidObject) { pending.Dequeue(); WriteReport(item.Snapshot, "Projeto preservado; famílias pendentes porque o projeto foi fechado", [], 0); return; }
        if (item.Document.IsModifiable || item.Document.IsReadOnly) return;
        pending.Dequeue();
        try
        {
            var moved = files.RelocateNumberedBackups(item.Settings, item.Snapshot);
            var results = ExportFamilies(item.Document, item.Snapshot);
            WriteReport(item.Snapshot, results.Any(r => r.Status == "Falha") ? "Backup concluído com falhas na exportação de famílias" : "Backup concluído", results, moved);
        }
        catch (Exception exception) { WriteError(item.Settings.ProjectPath, exception); WriteReport(item.Snapshot, exception.Message, [], 0); }
    }

    private static List<FamilyResult> ExportFamilies(Document document, BackupSnapshot snapshot)
    {
        var results = new List<FamilyResult>();
        Directory.CreateDirectory(snapshot.FamilyFolder);
        foreach (var family in new FilteredElementCollector(document).OfClass(typeof(Family)).Cast<Family>().ToList())
        {
            if (!family.IsEditable || family.IsInPlace) { results.Add(new(family.Name, "Não exportável", null)); continue; }
            Document? familyDocument = null;
            try
            {
                familyDocument = document.EditFamily(family);
                var safeName = string.Concat(family.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).TrimEnd('.', ' ');
                var path = Path.Combine(snapshot.FamilyFolder, $"familia_{family.Id.Value}_{safeName}.rfa");
                using var options = new SaveAsOptions { OverwriteExistingFile = false, MaximumBackups = 1 };
                familyDocument.SaveAs(path, options);
                results.Add(new(family.Name, "Exportada", path));
            }
            catch (Exception exception) { results.Add(new(family.Name, "Falha", exception.Message)); }
            finally
            {
                if (familyDocument?.IsValidObject == true) familyDocument.Close(false);
            }
        }
        return results;
    }

    private static void WriteReport(BackupSnapshot snapshot, string status, IReadOnlyList<FamilyResult> families, int moved) => File.WriteAllText(Path.Combine(snapshot.ProjectFolder, "backup.json"), JsonSerializer.Serialize(new { snapshot.Id, snapshot.Phase, Status = status, Families = families, NumberedBackupsMoved = moved }, new JsonSerializerOptions { WriteIndented = true }));

    private static void WriteError(string project, Exception exception) => PluginDiagnostics.Record(project, exception);

    public void Dispose()
    {
        application.ControlledApplication.DocumentSaving -= BeforeSave;
        application.ControlledApplication.DocumentSaved -= AfterSave;
        application.ControlledApplication.DocumentSavedAs -= AfterSaveAs;
        application.Idling -= OnIdle;
    }

    private sealed record PendingBackup(Document Document, ProjectSettings Settings, BackupSnapshot Snapshot);
    private sealed record FamilyResult(string Name, string Status, string? Detail);
}
