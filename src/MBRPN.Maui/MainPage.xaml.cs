using System.Collections.ObjectModel;
using System.Diagnostics;
using MBRPN.Engine;
using MBRPN.Engine.History;
using MBRPN.Engine.Keys;
using MBRPN.Engine.State;
using MBRPN.Maui.Graphics;
using MBRPN.Maui.Services;
using MBRPN.Maui.Views;

namespace MBRPN.Maui;

public partial class MainPage : ContentPage
{
    private const double DesignW = 1010, DesignH = 650;

    // Narrower on the phone, where every horizontal pixel comes out of the keypad.
    private static readonly double HistoryPanelWidth =
        DeviceInfo.Platform == DevicePlatform.Android ? 200 : 264;

    private readonly CalculatorEngine _engine = new();
    private readonly StateStore _store = new();
    private readonly KeySound _keySound = new();
    private readonly LcdDrawable _lcd = new();
    private readonly ObservableCollection<HistoryEntry> _history = [];
    private IDispatcherTimer? _saveTimer;
    private bool _dirty;
    private bool _historyOpen;
    private double _lastWidth = -1, _lastHeight = -1;

    public MainPage()
    {
        InitializeComponent();

        try
        {
            if (_store.Load() is { } json && StateSerializer.FromJson(json) is { } state)
                _engine.Restore(state);
        }
        catch
        {
            // corrupted or unavailable storage: start fresh
        }

        LcdView.Drawable = _lcd;
        HistoryList.ItemsSource = _history;
        HistoryPanel.WidthRequest = HistoryPanelWidth;
        BuildKeypad();

        _engine.Changed += OnEngineChanged;
        _engine.History.Changed += RebuildHistory;
        Refresh();
        RebuildHistory();

        _saveTimer = Dispatcher.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(500);
        _saveTimer.Tick += (_, _) => FlushSave();
        _saveTimer.Start();

        Loaded += (_, _) => HookKeyboard();
        Unloaded += (_, _) =>
        {
            UnhookKeyboard();
            _keySound.Dispose();
        };
    }

    // Implemented in MainPage.Windows.cs; no-ops elsewhere.
    partial void HookKeyboard();
    partial void UnhookKeyboard();

    private void BuildKeypad()
    {
        // Star-sized cells: the keypad block stretches to fill the whole face,
        // so the keys get every pixel the header and footer leave free.
        for (int col = 0; col < 10; col++)
            KeypadGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int row = 0; row < 4; row++)
            KeypadGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        foreach (var def in KeyboardMap.Keys.OrderBy(k => k.Row).ThenBy(k => k.Col))
        {
            var key = new CalcKeyView(def);
            key.Pressed += id => _ = PressAsync(id);
            Grid.SetColumn(key, def.Col - 1);
            Grid.SetRow(key, def.Row - 1);
            if (def.Id == KeyId.Enter)
                Grid.SetRowSpan(key, 2);
            KeypadGrid.Children.Add(key);
        }

        // CLEAR group rule over the SST…ENTER f-legends (row 3, columns 2–6).
        var bracket = new GraphicsView
        {
            Drawable = new ClearBracketDrawable(),
            HeightRequest = 26,
            TranslationY = -19,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true,
        };
        Grid.SetRow(bracket, 2);
        Grid.SetColumn(bracket, 1);
        Grid.SetColumnSpan(bracket, 5);
        KeypadGrid.Children.Add(bracket);
    }

    private void OnEngineChanged()
    {
        Debug.Assert(MainThread.IsMainThread, "engine must only change on the UI thread");
        _dirty = true;
        Refresh();
    }

    private void Refresh()
    {
        _lcd.State = _engine.Display;
        LcdView.Invalidate();

        bool rowsEnabled = !(_engine.ProgramMode || _engine.IsRunning || _engine.IsOff);
        HistoryList.IsEnabled = rowsEnabled;
        HistoryList.Opacity = rowsEnabled ? 1.0 : 0.45;
    }

    private void RebuildHistory()
    {
        _history.Clear();
        foreach (var entry in _engine.History.Entries.Reverse())
            _history.Add(entry);
    }

    private async Task PressAsync(KeyId key)
    {
        _keySound.Play();
        bool wasRunning = _engine.IsRunning;
        _engine.Press(key);
        if (!wasRunning && _engine.IsRunning)
            await RunLoopAsync();
    }

    private async Task ReplayAsync(HistoryEntry entry)
    {
        _engine.Replay(entry);
        if (_engine.IsRunning)              // a replayed R/S starts the program
            await RunLoopAsync();
        Refresh();
    }

    /// <summary>Drives program execution without blocking the UI, honoring PSE pauses.</summary>
    private async Task RunLoopAsync()
    {
        int steps = 0;
        while (_engine.IsRunning)
        {
            var status = _engine.StepProgram();
            if (status == ProgramStepResult.Paused)
            {
                Refresh();
                await Task.Delay(1100);
                continue;
            }
            if (status is ProgramStepResult.Halted or ProgramStepResult.Errored) break;
            if (++steps % 500 == 0)
            {
                Refresh();
                await Task.Yield(); // keep the UI responsive during long programs
            }
        }
        Refresh();
    }

    public void FlushSave()
    {
        if (!_dirty) return;
        _dirty = false;
        _store.Save(StateSerializer.ToJson(_engine.Snapshot()));
    }

    private void OnHistoryClearClicked(object? sender, EventArgs e) =>
        _engine.History.Clear();

    private async void OnHistoryRowTapped(object? sender, TappedEventArgs e)
    {
        if (_engine.ProgramMode || _engine.IsRunning || _engine.IsOff) return;
        if ((sender as BindableObject)?.BindingContext is HistoryEntry entry)
            await ReplayAsync(entry);
    }

    private async void OnHistoryToggleTapped(object? sender, TappedEventArgs e)
    {
        _historyOpen = !_historyOpen;
        HistoryToggleLabel.TextColor = _historyOpen ? Theme.Palette.Teal : Theme.Palette.Muted;
        HistoryToggle.Stroke = _historyOpen ? Theme.Palette.TealDim : Theme.Palette.PanelBorder;

        HistoryPanel.IsVisible = _historyOpen;
        UpdateLayout();
        if (_historyOpen)
        {
            HistoryPanel.Opacity = 0;
            await HistoryPanel.FadeToAsync(1, 150);
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;
        if (Math.Abs(width - _lastWidth) < 0.5 && Math.Abs(height - _lastHeight) < 0.5) return;
        _lastWidth = width;
        _lastHeight = height;
        UpdateLayout();
    }

    /// <summary>Scales the fixed design to fill whatever the history panel leaves free.</summary>
    private void UpdateLayout()
    {
        // +2 covers the panel border's stroke so the calculator never overflows its column.
        double availW = _lastWidth - RootGrid.Padding.HorizontalThickness
            - (_historyOpen ? HistoryPanelWidth + RootGrid.ColumnSpacing + 2 : 0);
        double availH = _lastHeight - RootGrid.Padding.VerticalThickness;
        double scale = Math.Min(availW / DesignW, availH / DesignH);
        if (scale <= 0) return;

        Faceplate.Scale = scale;
        ScaleBox.WidthRequest = DesignW * scale;
        ScaleBox.HeightRequest = DesignH * scale;
    }
}
