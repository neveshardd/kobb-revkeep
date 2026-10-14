using System.Text.RegularExpressions;

namespace Kobb.Revkeep.Core;

public sealed record ProjectSettings(string ProjectPath, string ProjectName, string BackupRoot, string FamilyRoot)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectName) || ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || ProjectName.EndsWith('.') || ProjectName.EndsWith(' ') || Regex.IsMatch(ProjectName, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase))
            throw new SettingsValidationException("Informe um nome de projeto válido.", nameof(ProjectName));
        if (!Path.IsPathFullyQualified(ProjectPath) || !ProjectPath.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
            throw new SettingsValidationException("Salve o projeto em um arquivo RVT local antes de configurar os backups.", nameof(ProjectPath));
        foreach (var (name, path) in new[] { (nameof(BackupRoot), BackupRoot), (nameof(FamilyRoot), FamilyRoot) })
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
                throw new SettingsValidationException("Escolha caminhos completos para as pastas de backup.", name);
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath)) throw new SettingsValidationException("O caminho escolhido corresponde a um arquivo.", name);
        }
    }
}
