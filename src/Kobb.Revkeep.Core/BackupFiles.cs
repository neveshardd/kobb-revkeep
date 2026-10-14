using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Kobb.Revkeep.Core;

public sealed record BackupSnapshot(string Id, string ProjectFolder, string FamilyFolder, string Phase);

public sealed class BackupFiles
{
    public BackupSnapshot Capture(ProjectSettings settings, string phase)
    {
        var layout = BackupLayout.From(settings);
        layout.EnsureExists();
        var id = $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fff}_{phase}_{Guid.NewGuid():N}";
        var projectFolder = Path.Combine(layout.Backups, id);
        var familyFolder = Path.Combine(layout.Families, id);
        Directory.CreateDirectory(projectFolder);
        if (File.Exists(settings.ProjectPath))
            CopyVerified(settings.ProjectPath, Path.Combine(projectFolder, Path.GetFileName(settings.ProjectPath)));
        return new BackupSnapshot(id, projectFolder, familyFolder, phase);
    }

    public int RelocateNumberedBackups(ProjectSettings settings, BackupSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(settings.ProjectPath)!;
        var stem = Path.GetFileNameWithoutExtension(settings.ProjectPath);
        var pattern = new Regex("^" + Regex.Escape(stem) + @"\.\d{4,}\.rvt$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var moved = 0;
        foreach (var source in Directory.EnumerateFiles(directory).Where(p => pattern.IsMatch(Path.GetFileName(p))))
        {
            var targetDirectory = Path.Combine(snapshot.ProjectFolder, "backups-revit");
            Directory.CreateDirectory(targetDirectory);
            var target = Path.Combine(targetDirectory, Path.GetFileName(source));
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Delete))
            {
                WriteVerified(input, target);
                File.Delete(source);
            }
            moved++;
        }
        return moved;
    }

    private static void CopyVerified(string source, string target)
    {
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        WriteVerified(input, target);
    }

    private static void WriteVerified(FileStream input, string target)
    {
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                input.CopyTo(output);
                output.Flush(true);
            }
            input.Position = 0;
            var expected = SHA256.HashData(input);
            using (var output = File.OpenRead(temporary))
                if (!CryptographicOperations.FixedTimeEquals(expected, SHA256.HashData(output)))
                    throw new IOException("A transferência do backup não passou pela verificação de integridade.");
            File.Move(temporary, target, false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
