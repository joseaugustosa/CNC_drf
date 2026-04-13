using CNC_Drf.Dialogs;
using CNC_Drf.ViewModels;

namespace CNC_Drf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog { Owner = this };
        dlg.ShowDialog();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        => _vm.Dispose();
}
