using System.Windows;
using PowerShellUI.App.ViewModels;

namespace PowerShellUI.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
