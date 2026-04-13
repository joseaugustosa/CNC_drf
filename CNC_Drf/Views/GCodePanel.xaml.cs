using CNC_Drf.ViewModels;
using Microsoft.Win32;

namespace CNC_Drf.Views;

public partial class GCodePanel : UserControl
{
    public GCodePanel() => InitializeComponent();

    private MainViewModel? VM => DataContext as MainViewModel;

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Abrir ficheiro G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.txt|Todos|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        VM?.LoadFile(dlg.FileName);
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)   => VM?.SendCommandCommand.Execute("~");
    private void BtnHold_Click(object sender, RoutedEventArgs e)    => VM?.SendCommandCommand.Execute("!");
    private void BtnAbort_Click(object sender, RoutedEventArgs e)   => VM?.SoftResetCommand.Execute(null);
}
