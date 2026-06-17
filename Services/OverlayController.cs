using System.Windows;
using System.Windows.Threading;
using QuickInput.Core;
using QuickInput.Views;

namespace QuickInput.Services;

public sealed class OverlayController
{
    private static readonly TimeSpan SyncDelay = TimeSpan.FromMilliseconds(350);
    private const string PendingSyncStatus = "悬浮输入 · 输入中，稍后同步";
    private const string InjectionPendingStatus = "悬浮输入 · 关闭后输入到目标";
    private const string SyncedStatus = "悬浮输入 · 已同步到目标";

    private readonly SettingsStore _settingsStore;
    private readonly DispatcherTimer _syncTimer;
    private OverlayWindow? _window;
    private TargetTextSession? _session;
    private string? _pendingText;
    private bool _syncInProgress;

    public OverlayController(SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _syncTimer = new DispatcherTimer
        {
            Interval = SyncDelay
        };
        _syncTimer.Tick += (_, _) => FlushPendingTextSync();
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
        _window.TextChangedByUser += (_, text) => QueueTextSync(text);

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

        var currentWindow = _window;
        var currentSession = _session;
        var textToCommit = currentWindow.CurrentText;
        var shouldInjectText = commit &&
                               currentSession is not null &&
                               !currentSession.CanReplaceText &&
                               currentSession.CanInjectText;

        if (commit && currentSession?.CanReplaceText == true)
        {
            _pendingText = textToCommit;
            FlushPendingTextSync(restoreFocus: false, runSynchronously: true);
        }
        else
        {
            _syncTimer.Stop();
            _pendingText = null;
        }

        SavePlacement();

        _window = null;
        _session = null;

        currentWindow.ForceClose();

        if (shouldInjectText)
        {
            currentSession!.TryInjectText(textToCommit);
        }
        else
        {
            currentSession?.RestoreFocus();
        }
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

    private void QueueTextSync(string text)
    {
        if (_window is null || _session is null)
        {
            return;
        }

        if (_session.CanReplaceText)
        {
            _pendingText = text;
            _window.SetStatus(PendingSyncStatus);
            _syncTimer.Stop();
            _syncTimer.Start();
            return;
        }

        if (_session.CanInjectText)
        {
            _window.SetStatus(InjectionPendingStatus);
            return;
        }

        if (!_session.HasPotentialWriteTarget)
        {
            _window.SetStatus(_session.StatusText);
            return;
        }
    }

    private void FlushPendingTextSync(bool restoreFocus = true, bool runSynchronously = false)
    {
        _syncTimer.Stop();

        if (_pendingText is null)
        {
            return;
        }

        if (_syncInProgress)
        {
            return;
        }

        var text = _pendingText;
        _pendingText = null;

        if (runSynchronously)
        {
            SyncTextToTarget(text, restoreFocus);
            return;
        }

        _ = SyncTextToTargetAsync(text, restoreFocus);
    }

    private void SyncTextToTarget(string text, bool restoreFocus)
    {
        var currentWindow = _window;
        var currentSession = _session;
        if (currentWindow is null || currentSession is null)
        {
            return;
        }

        if (!currentSession.CanReplaceText)
        {
            currentWindow.SetStatus(currentSession.StatusText);
            return;
        }

        _syncInProgress = true;
        try
        {
            var success = currentSession.TryWriteText(text);
            currentWindow.SetStatus(success ? SyncedStatus : "悬浮输入 · 写入目标失败");
            if (restoreFocus)
            {
                currentWindow.EnsureInputFocus();
            }
        }
        finally
        {
            _syncInProgress = false;
        }
    }

    private async Task SyncTextToTargetAsync(string text, bool restoreFocus)
    {
        var currentWindow = _window;
        var currentSession = _session;
        if (currentWindow is null || currentSession is null)
        {
            return;
        }

        if (!currentSession.CanReplaceText)
        {
            currentWindow.SetStatus(currentSession.StatusText);
            return;
        }

        _syncInProgress = true;
        try
        {
            var success = await Task.Run(() => currentSession.TryWriteText(text));
            if (!ReferenceEquals(currentWindow, _window) || !ReferenceEquals(currentSession, _session))
            {
                return;
            }

            currentWindow.SetStatus(_pendingText is not null
                ? PendingSyncStatus
                : success
                    ? SyncedStatus
                    : "悬浮输入 · 写入目标失败");
            if (restoreFocus)
            {
                currentWindow.EnsureInputFocus();
            }
        }
        finally
        {
            _syncInProgress = false;
            if (_pendingText is not null && _window is not null)
            {
                _syncTimer.Stop();
                _syncTimer.Start();
            }
        }
    }
}
