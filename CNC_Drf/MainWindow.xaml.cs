using CNC_Drf.ViewModels;

namespace CNC_Drf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        RefreshPorts();
        Closed += (_, _) => _vm.Dispose();
    }

    private void RefreshPorts()
    {
        var ports = _vm.GetAvailablePorts();
        CmbPort.ItemsSource = ports;
        if (ports.Length > 0) CmbPort.SelectedIndex = 0;
    }

    private void BtnRefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void TbMdi_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _vm.SendCommandCommand.Execute(TbMdi.Text);
        TbMdi.Clear();
    }

    private void BtnMdiSend_Click(object sender, RoutedEventArgs e)
    {
        _vm.SendCommandCommand.Execute(TbMdi.Text);
        TbMdi.Clear();
    }
}
