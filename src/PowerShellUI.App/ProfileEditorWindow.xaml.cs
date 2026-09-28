using System.Windows;
using PowerShellUI.App.ViewModels;
using PowerShellUI.Core.Models;

namespace PowerShellUI.App;

/// <summary>
/// Modaler Dialog zum Anlegen/Bearbeiten eines <see cref="TenantProfile"/>. Bei Bestätigung
/// liefert <see cref="Result"/> das fertige Profil, sonst <c>null</c>.
/// </summary>
public partial class ProfileEditorWindow : Window
{
    private readonly ProfileEditorViewModel _viewModel;

    public ProfileEditorWindow(TenantProfile? existing)
    {
        InitializeComponent();
        _viewModel = new ProfileEditorViewModel(existing);
        DataContext = _viewModel;
    }

    public TenantProfile? Result { get; private set; }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.Name))
        {
            MessageBox.Show(this, "Bitte einen Namen für das Profil angeben.", "Profil",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = _viewModel.ToProfile();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
