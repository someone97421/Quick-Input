using System.Threading;
using System.Windows;
using QuickInput.Core;
using QuickInput.Services;
using MessageBox = System.Windows.MessageBox;

namespace QuickInput;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private SettingsStore? _settingsStore;
    private OverlayController? _overlayController;
    private TrayIconService? _trayIconService;
    private GlobalHotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "QuickInput.SingleInstance", out var created);
        if (!created)
        {
            Current.Shutdown();
            return;
        }

        _settingsStore = new SettingsStore();
        var settings = _settingsStore.Load();
        ThemeService.Apply(settings.Theme);

        _overlayController = new OverlayController(_settingsStore);
        _hotkeyService = new GlobalHotkeyService();
        _hotkeyService.HotkeyPressed += (_, _) => _overlayController.Toggle();
        try
        {
            _hotkeyService.Register(settings.Hotkey);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{ex.Message}\n\n应用会继续运行，请在托盘菜单里重新设置快捷键。",
                "QuickInput",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _trayIconService = new TrayIconService(
            settings,
            _settingsStore,
            _hotkeyService,
            _overlayController);
        _trayIconService.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _hotkeyService?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
