namespace Kobb.Revkeep.Core;

public sealed class SettingsValidationException(string message, string field) : ArgumentException(message)
{
    public string Field { get; } = field;
}
