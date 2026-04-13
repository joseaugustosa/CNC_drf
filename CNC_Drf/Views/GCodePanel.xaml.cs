using CNC_Drf.ViewModels;
using Microsoft.Win32;

namespace CNC_Drf.Views;

public partial class GCodePanel : UserControl
{
    public GCodePanel() => InitializeComponent();

    private MainViewModel? VM => DataContext as MainViewModel;

    private void BtnOpen_Click(object sender, RoutedEventArgs e) => OpenFile();
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(VM?.FilePath)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = VM.FilePath, UseShellExecute = true
        });
    }
    private void BtnLast_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(VM?.FilePath)) VM.LoadFile(VM.FilePath);
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)  => VM?.CycleResumeCommand.Execute(null);
    private void BtnPause_Click(object sender, RoutedEventArgs e)   => VM?.FeedHoldCommand.Execute(null);
    private void BtnStop_Click(object sender, RoutedEventArgs e)    => VM?.SoftResetCommand.Execute(null);

    private void GcodeList_DragOver(object sender, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;

    private void GcodeList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files?.Length > 0) VM?.LoadFile(files[0]);
    }

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Abrir G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap;*.plt;*.txt|Todos|*.*"
        };
        if (dlg.ShowDialog() == true) VM?.LoadFile(dlg.FileName);
    }
}
