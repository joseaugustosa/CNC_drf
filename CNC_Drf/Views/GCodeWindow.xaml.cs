using CNC_Drf.ViewModels;
using System.ComponentModel;

namespace CNC_Drf.Views;

public partial class GCodeWindow : Window
{
    private MainViewModel? _vm;
    private int            _lastLine = -1;

    public GCodeWindow()
    {
        InitializeComponent();
    }

    // ── Liga ao ViewModel definindo o DataContext ────────────────────────
    public void Attach(MainViewModel vm)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmChanged;

        _vm        = vm;
        DataContext = vm;   // ← binding directo igual ao GCodePanel
        _vm.PropertyChanged += OnVmChanged;

        // Estado inicial
        Dispatcher.Invoke(FullRefresh);
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.FileName):
                case nameof(MainViewModel.TotalLines):
                    UpdateHeader();
                    // Reatribui a lista sempre que o ficheiro muda
                    GcodeList.ItemsSource = _vm!.Parser.Lines;
                    _lastLine = -1;
                    break;

                case nameof(MainViewModel.IsRunning):
                    UpdateRunningPanel();
                    break;

                case nameof(MainViewModel.CurrentLine):
                case nameof(MainViewModel.CurrentLineText):
                    UpdateCurrentLine();
                    ScrollTo(_vm!.CurrentLine);
                    break;

                case nameof(MainViewModel.Progress):
                    PbProgress.Value = _vm!.Progress;
                    TbPercent.Text   = $"{_vm.Progress:F1}%";
                    break;

                case nameof(MainViewModel.TaskTime):
                    TbTaskTime.Text = $"⏱ {_vm!.TaskTime}";
                    break;

                case nameof(MainViewModel.TimeToComp):
                    TbEta.Text = (_vm!.TimeToComp != "--:--") ? $"  ETA {_vm.TimeToComp}" : "";
                    break;

                case nameof(MainViewModel.StatusText):
                    TbStatus.Text        = _vm!.StatusText;
                    TbControlStatus.Text = _vm!.StatusText;
                    break;
            }
        });
    }

    private void FullRefresh()
    {
        if (_vm is null) return;
        UpdateHeader();
        GcodeList.ItemsSource = _vm.Parser.Lines;
        UpdateRunningPanel();
    }

    private void UpdateHeader()
    {
        if (_vm is null) return;
        bool has = _vm.TotalLines > 0;
        TbFileName.Text  = has ? _vm.FileName : "Sem ficheiro";
        TbLineCount.Text = has ? $"  ({_vm.TotalLines} linhas)" : "";
        EmptyOverlay.Visibility = has ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateRunningPanel()
    {
        if (_vm is null) return;
        RunningPanel.Visibility = _vm.IsRunning ? Visibility.Visible : Visibility.Collapsed;
        TbTotalLines.Text = _vm.TotalLines.ToString();
        if (_vm.IsRunning) UpdateCurrentLine();
    }

    private void UpdateCurrentLine()
    {
        if (_vm is null) return;
        TbCurrentLine.Text = (_vm.CurrentLine + 1).ToString();
        TbCurrentCode.Text = _vm.CurrentLineText;
        PbProgress.Value   = _vm.Progress;
        TbPercent.Text     = $"{_vm.Progress:F1}%";
    }

    private void ScrollTo(int idx)
    {
        if (idx == _lastLine || idx < 0 || idx >= GcodeList.Items.Count) return;
        _lastLine = idx;
        GcodeList.SelectedIndex = idx;
        GcodeList.ScrollIntoView(GcodeList.Items[idx]);
    }

    // ── Botões ───────────────────────────────────────────────────────────
    private void BtnStart_Click  (object s, RoutedEventArgs e) => _vm?.StartCycleCommand .Execute(null);
    private void BtnPause_Click  (object s, RoutedEventArgs e) => _vm?.FeedHoldCommand   .Execute(null);
    private void BtnResume_Click (object s, RoutedEventArgs e) => _vm?.CycleResumeCommand.Execute(null);
    private void BtnStop_Click   (object s, RoutedEventArgs e) => _vm?.SoftResetCommand  .Execute(null);

    private void BtnOnTop_Checked  (object s, RoutedEventArgs e) => Topmost = true;
    private void BtnOnTop_Unchecked(object s, RoutedEventArgs e) => Topmost = false;

    // ── Janela esconde em vez de fechar ──────────────────────────────────
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
