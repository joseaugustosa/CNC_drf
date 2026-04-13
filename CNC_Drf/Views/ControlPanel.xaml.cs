namespace CNC_Drf.Views;

public partial class ControlPanel : UserControl
{
    public ControlPanel() => InitializeComponent();

    private void BtnRefreshPorts_Click(object sender, RoutedEventArgs e)
        => RefreshPorts();

    private void CmbPort_DropDownOpened(object sender, EventArgs e)
        => RefreshPorts();

    private void RefreshPorts()
    {
        if (DataContext is not ViewModels.MainViewModel vm) return;
        var ports   = vm.GetAvailablePorts();
        var current = CmbPort.SelectedItem as string ?? vm.PortName;
        CmbPort.ItemsSource = ports;
        if (ports.Contains(current))
            CmbPort.SelectedItem = current;
        else if (ports.Length > 0)
            CmbPort.SelectedIndex = 0;
    }
}
