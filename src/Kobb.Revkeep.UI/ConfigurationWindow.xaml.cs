using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using Kobb.Revkeep.Core;
using Microsoft.Win32;

namespace Kobb.Revkeep.UI;

public partial class ConfigurationWindow : Window
{
    private readonly string projectPath;
    private readonly Action<ProjectSettings> save;
    private bool hasError;

    public ConfigurationWindow(ProjectSettings settings, Action<ProjectSettings> save, string? notice = null)
    {
        InitializeComponent();
        WindowChromeBehavior.Configure(this);
        Icon = ProductBrand.Icon(32);
        projectPath = settings.ProjectPath;
        this.save = save;
        ProjectNameField.Text = settings.ProjectName;
        BackupRootField.Text = settings.BackupRoot;
        FamilyRootField.Text = settings.FamilyRoot;
        var area = SystemParameters.WorkArea;
        Width = Math.Min(668, area.Width * .85);
        Height = Math.Min(530, area.Height * .85);
        MinWidth = Math.Min(MinWidth, Width);
        MinHeight = Math.Min(MinHeight, Height);
        foreach (var button in new[] { MinimizeButton, MaximizeButton, CloseButton })
        {
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            button.MouseEnter += (_, _) =>
            {
                button.Background = button == CloseButton ? Brushes.Red : new SolidColorBrush(Color.FromRgb(225, 225, 225));
                button.Foreground = button == CloseButton ? Brushes.White : Brushes.Black;
            };
            button.MouseLeave += (_, _) => { button.Background = Brushes.Transparent; button.Foreground = Brushes.Black; };
        }
        if (notice is not null) SetStatus(notice, false);
        foreach (var field in new[] { ProjectNameField, BackupRootField, FamilyRootField })
            field.TextChanged += (_, _) => { if (hasError) SetStatus("Backups automáticos antes e depois de salvar no Revit.", false); };
        PreviewKeyDown += KeyboardShortcut;
        Loaded += (_, _) => ProjectNameField.Focus();
    }

    private void Confirm(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = new ProjectSettings(projectPath, ProjectNameField.Text.Trim(), BackupRootField.Text.Trim(), FamilyRootField.Text.Trim());
            settings.Validate();
            save(settings);
            DialogResult = true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            SetStatus(exception.Message);
            if (exception is SettingsValidationException argument)
            {
                var field = argument.Field switch
                {
                    nameof(ProjectSettings.ProjectName) => ProjectNameField,
                    nameof(ProjectSettings.BackupRoot) => BackupRootField,
                    nameof(ProjectSettings.FamilyRoot) => FamilyRootField,
                    _ => null
                };
                field?.Focus();
            }
        }
    }

    private void BrowseProjects(object sender, RoutedEventArgs e) => Browse(BackupRootField);
    private void BrowseFamilies(object sender, RoutedEventArgs e) => Browse(FamilyRootField);

    private void Browse(System.Windows.Controls.TextBox field)
    {
        var dialog = new OpenFolderDialog { Title = "Escolher pasta de backups", Multiselect = false };
        if (Directory.Exists(field.Text)) dialog.InitialDirectory = field.Text;
        if (dialog.ShowDialog(this) == true) field.Text = dialog.FolderName;
    }

    private void Cancel(object sender, RoutedEventArgs e) => Close();
    private void Minimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void TitleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource != TitleBar) return;
        if (e.ClickCount == 2) Maximize(sender, e);
        else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void OpenWebsite(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException) { SetStatus("Não foi possível abrir o site. Acesse kobbstudio.com.br no navegador."); }
        e.Handled = true;
    }

    private void KeyboardShortcut(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Alt) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        switch (key)
        {
            case Key.N: ProjectNameField.Focus(); break;
            case Key.P: BackupRootField.Focus(); break;
            case Key.F: FamilyRootField.Focus(); break;
            case Key.B: Browse(BackupRootField); break;
            case Key.M: Browse(FamilyRootField); break;
            default: return;
        }
        e.Handled = true;
    }

    private void SetStatus(string message, bool error = true)
    {
        StatusText.Text = message;
        hasError = error;
        StatusText.Foreground = error ? Brushes.DarkRed : Brushes.Black;
        if (AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
            (UIElementAutomationPeer.FromElement(StatusText) ?? UIElementAutomationPeer.CreatePeerForElement(StatusText))?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
