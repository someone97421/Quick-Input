using System.Windows;
using QuickInput.Core;
using QuickInput.Views;

namespace QuickInput.Services;

public sealed class OverlayController
{
    private readonly SettingsStore _settingsStore;
    private OverlayWindow? _window;
    private TargetTextSession? _session;

    public OverlayController(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public bool IsOpen => _window?.IsVisible == true;

    public void Toggle()
    {
        if (IsOpen)
        {
            Close(commit: true);
            return;
        }

        Open();
    }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        _session = TargetTextSession.Capture();
        _window = new OverlayWindow(_session.InitialValue, _session.StatusText);
        _window.CloseRequested += (_, commit) => Close(commit);
        _window.LocationOrSizeChanged += (_, _) => SavePlacement();
        _window.TextChangedByUser += (_, text) => SyncTextToTarget(text);

        var settings = _settingsStore.Load();
        WindowPlacementService.Restore(_window, settings.OverlayPlacement);
        _window.Show();
        _window.FocusInput();
    }

    public void Close(bool commit)
    {
        if (_window is null)
        {
            return;
        }

        SavePlacement();

        var currentWindow = _window;
        var currentSession = _session;
        _window = null;
        _session = null;

        currentWindow.ForceClose();

        currentSession?.RestoreFocus();
    }

    public void ResetPlacement()
    {
        var settings = _settingsStore.Load();
        settings.OverlayPlacement = WindowPlacementService.CenterDefault();
        _settingsStore.Save(settings);

        if (_window is not null)
        {
            WindowPlacementService.Restore(_window, settings.OverlayPlacement);
        }
    }

    private void SavePlacement()
    {
        if (_window is null || !_window.IsLoaded)
        {
            return;
        }

        var settings = _settingsStore.Load();
        settings.OverlayPlacement = WindowPlacementService.Capture(_window);
        _settingsStore.Save(settings);
    }

    private void SyncTextToTarget(string text)
    {
        if (_window is null || _session is null)
        {
            return;
        }

        if (!_session.CanWriteText)
        {
            _window.SetStatus(_session.StatusText);
            return;
        }

        _window.SetStatus(_session.TryWriteText(text)
            ? _session.StatusText
            : "悬浮输入 · 写入目标失败");
        _window.EnsureInputFocus();
    }
}
