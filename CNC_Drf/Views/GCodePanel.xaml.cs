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

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (VM is null) return;
        var dlg = new SaveFileDialog
        {
            Title  = "Save G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap|All|*.*",
            FileName = VM.FileName
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, VM.GetGCodeText());
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        if (VM is null) return;
        Clipboard.SetText(VM.GetGCodeText());
    }

    private void BtnStepByStep_Click(object sender, RoutedEventArgs e)
        => VM?.ToggleStepByStepCommand.Execute(null);

    private void BtnStart_Click(object sender, RoutedEventArgs e)   => VM?.StartCycleCommand.Execute(null);
    private void BtnPause_Click(object sender, RoutedEventArgs e)   => VM?.FeedHoldCommand.Execute(null);
    private void BtnSmoothStop_Click(object sender, RoutedEventArgs e) => VM?.SmoothStopCommand.Execute(null);
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

    private void GcodeList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GcodeList.SelectedIndex >= 0)
            VM?.SetCurrentLine(GcodeList.SelectedIndex);
    }

    private void GcodeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GcodeList.SelectedIndex >= 0 && VM is not null)
            VM.StartFromLine = GcodeList.SelectedIndex;
    }

    private void CtxStartFromHere_Click(object sender, RoutedEventArgs e)
    {
        if (VM is null || GcodeList.SelectedIndex < 0) return;
        VM.StartFromLine = GcodeList.SelectedIndex;
        VM.StartCycleCommand.Execute(null);
    }

    private void CtxCopyLine_Click(object sender, RoutedEventArgs e)
    {
        if (GcodeList.SelectedItem is Core.GCodeLine line)
            Clipboard.SetText(line.Raw);
    }

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Open G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap;*.plt;*.txt|All|*.*"
        };
        if (dlg.ShowDialog() == true) VM?.LoadFile(dlg.FileName);
    }
}
