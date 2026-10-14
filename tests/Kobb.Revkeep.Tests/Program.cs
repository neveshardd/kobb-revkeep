using Kobb.Revkeep.Core;

var sandbox = Path.Combine(Path.GetTempPath(), "kobb-revkeep-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(sandbox);
try
{
    var project = Path.Combine(sandbox, "Projeto.rvt");
    File.WriteAllText(project, "estado anterior");
    var settings = new ProjectSettings(project, "Residência", Path.Combine(sandbox, "Backups"), Path.Combine(sandbox, "Famílias"));
    var store = new SettingsStore(Path.Combine(sandbox, "Configurações"));
    store.Save(settings);
    Assert(store.Load(project) == settings, "Configuração persistida por projeto");
    Assert(store.Load(Path.Combine(sandbox, "Outro.rvt")) is null, "Projetos independentes");
    var files = new BackupFiles();
    var sharedSettings = settings with { FamilyRoot = settings.BackupRoot };
    var layout = BackupLayout.From(sharedSettings);
    layout.EnsureExists();
    Assert(layout.Backups == Path.Combine(settings.BackupRoot, "Backup-Residência", "backups"), "Pasta de backups organizada por projeto");
    Assert(layout.Families == Path.Combine(settings.BackupRoot, "Backup-Residência", "familias"), "Pasta de famílias na mesma raiz");
    Assert(Directory.Exists(layout.Backups) && Directory.Exists(layout.Families), "As duas pastas são criadas na configuração");
    var before = files.Capture(settings, "antes");
    File.WriteAllText(project, "estado novo");
    var after = files.Capture(settings, "depois");
    Assert(File.ReadAllText(Path.Combine(before.ProjectFolder, "Projeto.rvt")) == "estado anterior", "Preservação antes de salvar");
    Assert(File.ReadAllText(Path.Combine(after.ProjectFolder, "Projeto.rvt")) == "estado novo", "Preservação depois de salvar");
    Assert(files.Capture(settings, "depois").Id != after.Id, "Versões não sobrescrevem backups");
    var numbered = Path.Combine(sandbox, "Projeto.0001.rvt");
    var unrelated = Path.Combine(sandbox, "Outro.0001.rvt");
    File.WriteAllText(numbered, "backup nativo");
    File.WriteAllText(unrelated, "não mover");
    Assert(files.RelocateNumberedBackups(settings, after) == 1, "Backup numerado identificado");
    Assert(!File.Exists(numbered) && File.ReadAllText(Path.Combine(after.ProjectFolder, "backups-revit", "Projeto.0001.rvt")) == "backup nativo", "Transferência íntegra");
    Assert(File.Exists(unrelated) && File.Exists(project), "Origem principal e outros projetos preservados");
    File.WriteAllText(numbered, "nova versão numerada");
    try { files.RelocateNumberedBackups(settings, after); throw new InvalidOperationException("Colisão deveria falhar."); }
    catch (IOException) { Assert(File.Exists(numbered), "Colisão preserva origem"); }
    using (var locked = new FileStream(numbered, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        try { files.RelocateNumberedBackups(settings, files.Capture(settings, "depois")); throw new InvalidOperationException("Arquivo bloqueado deveria falhar."); }
        catch (IOException) { Assert(File.Exists(numbered), "Arquivo bloqueado preservado"); }
    }
    try { (settings with { ProjectName = "../outro" }).Validate(); throw new InvalidOperationException("Nome inválido aceito."); }
    catch (ArgumentException) { }
    Console.WriteLine("PASS: preservação, configuração, estrutura de pastas e transferência de backups.");
}
finally
{
    var resolved = Path.GetFullPath(sandbox);
    if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("kobb-revkeep-tests-", StringComparison.Ordinal)) throw new InvalidOperationException("Sandbox inesperada.");
    Directory.Delete(resolved, true);
}

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
}
