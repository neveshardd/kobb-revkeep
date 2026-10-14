using System.IO;

namespace Kobb.Revkeep;

public static class PluginDiagnostics
{
    public static void Record(string project, Exception exception)
    {
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KOBB", "Revkeep");
            Directory.CreateDirectory(root);
            File.AppendAllText(Path.Combine(root, "errors.log"), $"{DateTimeOffset.Now:O} | {project} | {exception.Message}{Environment.NewLine}");
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(logException.Message); }
    }
}
