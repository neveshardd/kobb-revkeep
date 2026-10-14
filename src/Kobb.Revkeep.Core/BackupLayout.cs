namespace Kobb.Revkeep.Core;

public sealed record BackupLayout(string Backups, string Families)
{
    public static BackupLayout From(ProjectSettings settings)
    {
        settings.Validate();
        var folder = $"Backup-{settings.ProjectName}";
        return new BackupLayout(Path.Combine(settings.BackupRoot, folder, "backups"), Path.Combine(settings.FamilyRoot, folder, "familias"));
    }

    public void EnsureExists()
    {
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Families);
    }
}
