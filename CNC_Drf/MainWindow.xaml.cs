using CNC_Drf.Dialogs;
using CNC_Drf.ViewModels;
using CNC_Drf.Views;
using Microsoft.Win32;

namespace CNC_Drf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    // ── File ─────────────────────────────────────────────────────────────
    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap;*.plt;*.txt|All|*.*" };
        if (dlg.ShowDialog() == true) _vm.LoadFile(dlg.FileName);
    }

    private void BtnSaveFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_vm.FilePath)) return;
        var dlg = new SaveFileDialog { Filter = "G-code|*.nc;*.gcode;*.ngc;*.tap|All|*.*", FileName = _vm.FileName };
        if (dlg.ShowDialog() == true) File.WriteAllText(dlg.FileName, _vm.GetGCodeText());
    }

    private void BtnEditFile_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm.FilePath))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _vm.FilePath, UseShellExecute = true });
    }

    // ── Run ──────────────────────────────────────────────────────────────
    private void BtnStart_Click(object sender, RoutedEventArgs e)
        => _vm.StartCycleCommand.Execute(null);
    private void BtnHold_Click(object sender, RoutedEventArgs e)       => _vm.FeedHoldCommand.Execute(null);
    private void BtnSmoothStop_Click(object sender, RoutedEventArgs e) => _vm.SmoothStopCommand.Execute(null);
    private void BtnStop_Click(object sender, RoutedEventArgs e)       => _vm.SoftResetCommand.Execute(null);

    // ── Speed ─────────────────────────────────────────────────────────────
    private void BtnSpeedInc_Click(object sender, RoutedEventArgs e) => _vm.SpeedIncreaseCommand.Execute(null);
    private void BtnSpeedDec_Click(object sender, RoutedEventArgs e) => _vm.SpeedDecreaseCommand.Execute(null);

    // ── View ─────────────────────────────────────────────────────────────
    private void BtnFitView_Click(object sender, RoutedEventArgs e)  => VisPanel.FitView();

    // ── Dialogs ──────────────────────────────────────────────────────────
    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog { Owner = this };

        // Wire GRBL read: send $$ to board; responses reach vm.GrblSettings automatically
        dlg.RequestGrblRead = () => _vm.ReadGrblSettingsCommand.Execute(null);

        // Wire GRBL write: send each $N=V line to board
        dlg.RequestGrblWrite = cmds => _vm.WriteGrblCommands(cmds);

        // Wire controller type change
        dlg.SelectController(_vm.Controller.Type);
        dlg.ControllerTypeChanged = type => _vm.SetControllerType(type);

        // Forward any $N=V lines that arrive while dialog is open
        void OnGrblLine(string line) => dlg.ApplyGrblLine(line);
        _vm.GrblSettingLineReceived += OnGrblLine;

        dlg.ShowDialog();

        _vm.GrblSettingLineReceived -= OnGrblLine;
    }

    private void BtnAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "CNC_Drf — CNC Controller Software\n\n" +
            "Based on DrufelCNC v1.20 features.\n\n" +
            "Supports: Milling · Laser · Plasma · 3D Printing\n" +
            "Controllers: GRBL, Mach3, NVUM, STB5100, USBCNC\n\n" +
            "© 2026 — Open Source WPF Implementation",
            "About CNC_Drf",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        => _vm.Dispose();
}
