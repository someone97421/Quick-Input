using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using QuickInput.Core;
using QuickInput.Views;
using Forms = System.Windows.Forms;

namespace QuickInput.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly OverlayController _overlayController;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _startupItem;
    private readonly Icon _icon;
    private AppSettings _settings;
    private SettingsWindow? _settingsWindow;

    public TrayIconService(
        AppSettings settings,
        SettingsStore settingsStore,
        GlobalHotkeyService hotkeyService,
        OverlayController overlayController)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _hotkeyService = hotkeyService;
        _overlayController = overlayController;

        _startupItem = new Forms.ToolStripMenuItem("开机自启动")
        {
            Checked = _settings.StartWithWindows,
            CheckOnClick = true
        };
        _startupItem.CheckedChanged += StartupItem_OnCheckedChanged;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示/隐藏", null, (_, _) => Dispatch(_overlayController.Toggle));
        menu.Items.Add("设置快捷键", null, (_, _) => Dispatch(ShowSettingsWindow));
        menu.Items.Add(_startupItem);
        menu.Items.Add("复位位置", null, (_, _) => Dispatch(_overlayController.ResetPlacement));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("重启", null, (_, _) => Dispatch(Restart));
        menu.Items.Add("退出", null, (_, _) => Dispatch(Quit));

        _icon = LoadApplicationIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _icon,
            Text = $"QuickInput - {_settings.Hotkey.DisplayText}",
            ContextMenuStrip = menu,
            Visible = false
        };
        _notifyIcon.MouseClick += NotifyIcon_OnMouseClick;
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
    }

    private void NotifyIcon_OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            Dispatch(_overlayController.Toggle);
        }
    }

    private void StartupItem_OnCheckedChanged(object? sender, EventArgs e)
    {
        Dispatch(() =>
        {
            _settings.StartWithWindows = _startupItem.Checked;
            StartupService.SetEnabled(_settings.StartWithWindows);
            _settingsStore.Save(_settings);
        });
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow(_settings)
        {
            Topmost = true
        };
        _settingsWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
            {
                _settingsWindow = null;
            }
        };

        if (window.ShowDialog() == true)
        {
            var previous = _settings.Hotkey.Clone();
            var selectedSettings = window.Settings;
            var selectedHotkey = selectedSettings.Hotkey;
            try
            {
                _hotkeyService.Register(selectedHotkey);
                _settings = selectedSettings;
                _startupItem.CheckedChanged -= StartupItem_OnCheckedChanged;
                _startupItem.Checked = _settings.StartWithWindows;
                _startupItem.CheckedChanged += StartupItem_OnCheckedChanged;
                StartupService.SetEnabled(_settings.StartWithWindows);
                ThemeService.Apply(_settings.Theme);
                _settingsStore.Save(_settings);
                _notifyIcon.Text = $"QuickInput - {_settings.Hotkey.DisplayText}";
            }
            catch (Win32Exception ex)
            {
                _hotkeyService.Register(previous);
                Forms.MessageBox.Show(
                    ex.Message,
                    "QuickInput",
                    Forms.MessageBoxButtons.OK,
                    Forms.MessageBoxIcon.Warning);
            }
        }
    }

    private void Restart()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            Process.Start(new ProcessStartInfo(exe)
            {
                UseShellExecute = true
            });
        }

        Quit();
    }

    private void Quit()
    {
        _overlayController.Close(commit: false);
        _notifyIcon.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            RunSafely(action);
            return;
        }

        dispatcher.Invoke(() => RunSafely(action));
    }

    private static void RunSafely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Forms.MessageBox.Show(
                ex.Message,
                "QuickInput",
                Forms.MessageBoxButtons.OK,
                Forms.MessageBoxIcon.Error);
        }
    }

    private static Icon LoadApplicationIcon()
    {
        var exeIcon = Environment.ProcessPath is { Length: > 0 } path
            ? Icon.ExtractAssociatedIcon(path)
            : null;

        if (exeIcon is not null)
        {
            return exeIcon;
        }

        using var stream = typeof(TrayIconService).Assembly.GetManifestResourceStream("QuickInput.Assets.quick-input.ico");
        if (stream is not null)
        {
            return new Icon(stream);
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon.Dispose();
    }
}
