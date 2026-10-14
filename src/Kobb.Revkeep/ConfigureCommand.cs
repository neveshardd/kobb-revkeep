using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Kobb.Revkeep.Core;

namespace Kobb.Revkeep;

[Transaction(TransactionMode.Manual)]
public sealed class ConfigureCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
    {
        var document = data.Application.ActiveUIDocument?.Document;
        if (document is null || !ProjectConfiguration.Supports(document))
        {
            TaskDialog.Show("KOBB Revkeep", "Esta primeira versão atende projetos RVT locais, sem compartilhamento. Salve o projeto antes de configurar os backups.");
            return Result.Cancelled;
        }
        try
        {
            return new ProjectConfiguration(SettingsStore.ForCurrentUser()).Show(data.Application, document) ? Result.Succeeded : Result.Cancelled;
        }
        catch (Exception exception)
        {
            message = exception.Message;
            return Result.Failed;
        }
    }
}
