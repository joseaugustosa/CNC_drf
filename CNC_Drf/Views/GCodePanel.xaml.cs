using CNC_Drf.ViewModels;
using Microsoft.Win32;

namespace CNC_Drf.Views;

public partial class GCodePanel : UserControl
{
    private MainViewModel? _vm;

    public GCodePanel()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (_vm is not null) _vm.PropertyChanged -= OnVmChanged;
            _vm = e.NewValue as MainViewModel;
            if (_vm is not null) _vm.PropertyChanged += OnVmChanged;
        };
    }

    private void OnVmChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                // Ficheiro novo → reatribuir a lista (List<> não notifica sozinha)
                case nameof(MainViewModel.TotalLines):
                case nameof(MainViewModel.FileName):
                    GcodeList.ItemsSource = _vm?.Parser.Lines;
                    break;

                case nameof(MainViewModel.CurrentLine):
                    ScrollToCurrent();
                    break;
            }
        });
    }

    private void ScrollToCurrent()
    {
        if (_vm is null) return;
        var idx = _vm.CurrentLine;
        if (idx < 0 || idx >= GcodeList.Items.Count) return;
        GcodeList.ScrollIntoView(GcodeList.Items[idx]);
    }

    // ── Toolbar ficheiro ────────────────────────────────────────────────
    private void BtnOpen_Click(object s, RoutedEventArgs e) => OpenFile();
    private void BtnEdit_Click(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm?.FilePath)) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = _vm.FilePath, UseShellExecute = true });
    }
    private void BtnLast_Click(object s, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm?.FilePath)) _vm.LoadFile(_vm.FilePath);
    }
    private void BtnSave_Click(object s, RoutedEventArgs e)
    {
        if (_vm is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Guardar G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap|Todos|*.*",
            FileName = _vm.FileName
        };
        if (dlg.ShowDialog() == true) File.WriteAllText(dlg.FileName, _vm.GetGCodeText());
    }
    private void BtnCopy_Click(object s, RoutedEventArgs e)
    {
        if (_vm is not null) Clipboard.SetText(_vm.GetGCodeText());
    }

    // ── Controlos de execução ───────────────────────────────────────────
    private void BtnStart_Click  (object s, RoutedEventArgs e) => _vm?.StartCycleCommand .Execute(null);
    private void BtnPause_Click  (object s, RoutedEventArgs e) => _vm?.FeedHoldCommand   .Execute(null);
    private void BtnResume_Click (object s, RoutedEventArgs e) => _vm?.CycleResumeCommand.Execute(null);
    private void BtnStop_Click   (object s, RoutedEventArgs e) => _vm?.SoftResetCommand  .Execute(null);

    // ── Lista ────────────────────────────────────────────────────────────
    private void GcodeList_DragOver(object s, DragEventArgs e)
        => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;

    private void GcodeList_Drop(object s, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files?.Length > 0) _vm?.LoadFile(files[0]);
    }

    private void GcodeList_DoubleClick(object s, MouseButtonEventArgs e)
    {
        if (GcodeList.SelectedIndex >= 0) _vm?.SetCurrentLine(GcodeList.SelectedIndex);
    }

    private void GcodeList_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GcodeList.SelectedIndex >= 0 && _vm is not null)
            _vm.StartFromLine = GcodeList.SelectedIndex;
    }

    private void CtxStartFromHere_Click(object s, RoutedEventArgs e)
    {
        if (_vm is null || GcodeList.SelectedIndex < 0) return;
        _vm.StartFromLine = GcodeList.SelectedIndex;
        _vm.StartCycleCommand.Execute(null);
    }

    private void CtxCopyLine_Click(object s, RoutedEventArgs e)
    {
        if (GcodeList.SelectedItem is Core.GCodeLine line)
            Clipboard.SetText(line.Raw);
    }

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Abrir G-code",
            Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap;*.plt;*.txt|Todos|*.*"
        };
        if (dlg.ShowDialog() == true) _vm?.LoadFile(dlg.FileName);
    }
}
