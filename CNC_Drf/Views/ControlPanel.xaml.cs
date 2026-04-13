namespace CNC_Drf.Views;

public partial class ControlPanel : UserControl
{
    public ControlPanel() => InitializeComponent();

    private void BtnRefreshPorts_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            CmbPort.ItemsSource  = vm.GetAvailablePorts();
            CmbPort.SelectedIndex = 0;
        }
    }
}
