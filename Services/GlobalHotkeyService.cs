using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using QuickInput.Core;
using QuickInput.Interop;

namespace QuickInput.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 9001;
    private HwndSource? _source;
    private bool _registered;
    private HotkeyGesture? _current;

    public event EventHandler? HotkeyPressed;

    public HotkeyGesture? Current => _current?.Clone();

    public void Register(HotkeyGesture hotkey)
    {
        EnsureMessageWindow();
        Unregister();

        if (!hotkey.IsValid)
        {
            throw new InvalidOperationException("快捷键无效。");
        }

        var modifiers = hotkey.Modifiers | HotkeyGesture.ModNoRepeat;
        if (!NativeMethods.RegisterHotKey(_source!.Handle, HotkeyId, modifiers, (uint)hotkey.VirtualKey))
        {
            var error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"快捷键 {hotkey.DisplayText} 注册失败，可能已被其他程序占用。");
        }

        _current = hotkey.Clone();
        _registered = true;
    }

    public void Unregister()
    {
        if (_registered && _source is not null)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }
    }

    private void EnsureMessageWindow()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("QuickInputHotkeySink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
