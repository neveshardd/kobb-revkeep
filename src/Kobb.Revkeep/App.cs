using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Kobb.Revkeep.UI;
using Kobb.Revkeep.Core;

namespace Kobb.Revkeep;

public sealed class App : IExternalApplication
{
    private AutomaticBackups? backups;
    private AutomaticConfiguration? configuration;
    public Result OnStartup(UIControlledApplication application)
    {
        var panel = application.CreateRibbonPanel("KOBB Studio");
        var command = new PushButtonData("KobbRevkeep", "Revkeep", typeof(App).Assembly.Location, typeof(ConfigureCommand).FullName!);
        var button = (PushButton)panel.AddItem(command);
        button.ToolTip = "Configurar os backups automáticos do projeto e das famílias.";
        button.Image = ProductBrand.Icon(16);
        button.LargeImage = ProductBrand.Icon(32);
        button.AvailabilityClassName = typeof(ProjectAvailability).FullName;
        backups = new AutomaticBackups(application);
        configuration = new AutomaticConfiguration(application, new ProjectConfiguration(SettingsStore.ForCurrentUser()));
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        backups?.Dispose();
        configuration?.Dispose();
        return Result.Succeeded;
    }
}

public sealed class ProjectAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication application, CategorySet categories) => application.ActiveUIDocument?.Document is { IsFamilyDocument: false, IsLinked: false };
}
