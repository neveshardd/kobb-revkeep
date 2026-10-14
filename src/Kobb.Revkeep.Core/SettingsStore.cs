using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kobb.Revkeep.Core;

public sealed class SettingsStore(string root)
{
    public static SettingsStore ForCurrentUser() => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KOBB", "Revkeep", "projects"));

    public ProjectSettings? Load(string projectPath)
    {
        var path = GetPath(projectPath);
        if (!File.Exists(path)) return null;
        var settings = JsonSerializer.Deserialize<ProjectSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("A configuração do Revkeep está vazia.");
        settings.Validate();
        return settings;
    }

    public void Save(ProjectSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(root);
        var path = GetPath(settings.ProjectPath);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private string GetPath(string path) => Path.Combine(root, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToUpperInvariant()))) + ".json");
}
