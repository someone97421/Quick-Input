using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using QuickInput.Interop;

namespace QuickInput.Services;

public sealed class TargetTextSession
{
    private const int MaxMirrorLength = 12000;
    private const uint TextMessageTimeoutMs = 250;

    private readonly IntPtr _foregroundWindow;
    private readonly IntPtr _focusedHandle;
    private readonly AutomationElement? _targetElement;
    private readonly ValuePattern? _valuePattern;
    private readonly TextPattern? _textPattern;
    private readonly bool _canUseWin32Text;

    private TargetTextSession(
        IntPtr foregroundWindow,
        IntPtr focusedHandle,
        AutomationElement? targetElement,
        ValuePattern? valuePattern,
        TextPattern? textPattern,
        bool canUseWin32Text)
    {
        _foregroundWindow = foregroundWindow;
        _focusedHandle = focusedHandle;
        _targetElement = targetElement;
        _valuePattern = valuePattern;
        _textPattern = textPattern;
        _canUseWin32Text = canUseWin32Text;
    }

    public bool CanReadText => _valuePattern is not null || _textPattern is not null || _canUseWin32Text;

    public bool CanReplaceText => _canUseWin32Text;

    public bool CanInjectText => _foregroundWindow != IntPtr.Zero;

    public bool CanWriteText => CanReplaceText || CanInjectText;

    public bool HasPotentialWriteTarget => CanReplaceText || CanInjectText;

    public string InitialValue => CanReplaceText ? ReadCurrentText() : string.Empty;

    public string StatusText => CanReplaceText
        ? "悬浮输入 · 正在同步到目标"
        : CanInjectText
            ? "悬浮输入 · 关闭后输入到目标"
            : CanReadText
                ? "悬浮输入 · 当前目标只读，无法写回"
                : "悬浮输入 · 当前目标不支持同步";

    public static TargetTextSession Capture()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var focusHandle = GetFocusedHandle(foreground);
        AutomationElement? element = null;
        ValuePattern? valuePattern = null;
        TextPattern? textPattern = null;
        var canUseWin32Text = IsWin32TextControl(focusHandle);

        try
        {
            element = AutomationElement.FocusedElement;
            if (element is null && focusHandle != IntPtr.Zero)
            {
                element = AutomationElement.FromHandle(focusHandle);
            }

            if (element is not null &&
                element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObject) &&
                valuePatternObject is ValuePattern vp)
            {
                valuePattern = vp;
            }
            else if (element is not null &&
                     element.TryGetCurrentPattern(TextPattern.Pattern, out var textPatternObject) &&
                     textPatternObject is TextPattern tp)
            {
                textPattern = tp;
            }
        }
        catch
        {
            element = null;
            valuePattern = null;
            textPattern = null;
        }

        return new TargetTextSession(foreground, focusHandle, element, valuePattern, textPattern, canUseWin32Text);
    }

    public string ReadCurrentText()
    {
        try
        {
            if (_valuePattern is not null)
            {
                return _valuePattern.Current.Value ?? string.Empty;
            }

            if (_textPattern is not null)
            {
                return _textPattern.DocumentRange.GetText(MaxMirrorLength) ?? string.Empty;
            }

            if (_canUseWin32Text)
            {
                return ReadWin32Text();
            }
        }
        catch
        {
            // The target may have closed, changed privilege level, or stopped exposing UIA data.
        }

        return string.Empty;
    }

    public bool TryWriteText(string text)
    {
        if (!_canUseWin32Text)
        {
            return false;
        }

        try
        {
            return SendTextMessage(
                _focusedHandle,
                NativeMethods.WmSetText,
                IntPtr.Zero,
                text,
                out var result) && result != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    public bool TryInjectText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            RestoreFocus();
            return true;
        }

        WaitForModifierRelease(TimeSpan.FromMilliseconds(500));

        if (!RestoreFocusAndWait(TimeSpan.FromMilliseconds(200)))
        {
            return false;
        }

        Thread.Sleep(10);
        return UnicodeInputInjector.SendText(text);
    }

    public void RestoreFocus()
    {
        if (_foregroundWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(_foregroundWindow);
        }

        try
        {
            _targetElement?.SetFocus();
        }
        catch
        {
            // Best effort only.
        }
    }

    private bool RestoreFocusAndWait(TimeSpan timeout)
    {
        RestoreFocus();

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (NativeMethods.GetForegroundWindow() == _foregroundWindow)
            {
                return true;
            }

            Thread.Sleep(1);
        }

        return NativeMethods.GetForegroundWindow() == _foregroundWindow;
    }

    private static void WaitForModifierRelease(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout && AnyModifierKeyPressed())
        {
            Thread.Sleep(10);
        }
    }

    private static bool AnyModifierKeyPressed()
    {
        return IsKeyPressed(NativeMethods.VkShift) ||
               IsKeyPressed(NativeMethods.VkControl) ||
               IsKeyPressed(NativeMethods.VkMenu) ||
               IsKeyPressed(NativeMethods.VkLwin) ||
               IsKeyPressed(NativeMethods.VkRwin);
    }

    private static bool IsKeyPressed(int virtualKey)
    {
        return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static IntPtr GetFocusedHandle(IntPtr foreground)
    {
        if (foreground == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var threadId = NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var info = new NativeMethods.GuiThreadInfo
        {
            CbSize = Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
        };

        return NativeMethods.GetGUIThreadInfo(threadId, ref info) ? info.HwndFocus : IntPtr.Zero;
    }

    private string ReadWin32Text()
    {
        if (_focusedHandle == IntPtr.Zero)
        {
            return string.Empty;
        }

        if (!SendTextMessage(_focusedHandle, NativeMethods.WmGetTextLength, IntPtr.Zero, IntPtr.Zero, out var lengthResult))
        {
            return string.Empty;
        }

        var length = lengthResult.ToInt32();

        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(length, MaxMirrorLength) + 1);
        if (!SendTextMessage(
            _focusedHandle,
            NativeMethods.WmGetText,
            new IntPtr(builder.Capacity),
            builder,
            out _))
        {
            return string.Empty;
        }

        return builder.ToString();
    }

    private static bool SendTextMessage(
        IntPtr handle,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        out IntPtr result)
    {
        return NativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            lParam,
            NativeMethods.SmtoAbortIfHung,
            TextMessageTimeoutMs,
            out result) != IntPtr.Zero;
    }

    private static bool SendTextMessage(
        IntPtr handle,
        int message,
        IntPtr wParam,
        string lParam,
        out IntPtr result)
    {
        return NativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            lParam,
            NativeMethods.SmtoAbortIfHung,
            TextMessageTimeoutMs,
            out result) != IntPtr.Zero;
    }

    private static bool SendTextMessage(
        IntPtr handle,
        int message,
        IntPtr wParam,
        StringBuilder lParam,
        out IntPtr result)
    {
        return NativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            lParam,
            NativeMethods.SmtoAbortIfHung,
            TextMessageTimeoutMs,
            out result) != IntPtr.Zero;
    }

    private static bool IsWin32TextControl(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var builder = new StringBuilder(256);
        if (NativeMethods.GetClassName(handle, builder, builder.Capacity) <= 0)
        {
            return false;
        }

        var className = builder.ToString();
        return className.Contains("Edit", StringComparison.OrdinalIgnoreCase);
    }
}
