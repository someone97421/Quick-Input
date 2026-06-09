using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using QuickInput.Interop;

namespace QuickInput.Services;

public sealed class TargetTextSession
{
    private const int MaxMirrorLength = 12000;

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

    public bool CanWriteText => CanWriteWithValuePattern() || _canUseWin32Text;

    public string InitialValue => ReadCurrentText();

    public string StatusText => CanWriteText
        ? "悬浮输入 · 正在同步到目标"
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
        try
        {
            if (CanWriteWithValuePattern())
            {
                _valuePattern!.SetValue(text);
                return true;
            }
        }
        catch
        {
            // Fall through to a Win32 edit-control write when that is available.
        }

        if (!_canUseWin32Text)
        {
            return false;
        }

        try
        {
            return NativeMethods.SendMessage(_focusedHandle, NativeMethods.WmSetText, IntPtr.Zero, text) != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
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

    private bool CanWriteWithValuePattern()
    {
        if (_valuePattern is null)
        {
            return false;
        }

        try
        {
            return !_valuePattern.Current.IsReadOnly;
        }
        catch
        {
            return false;
        }
    }

    private string ReadWin32Text()
    {
        if (_focusedHandle == IntPtr.Zero)
        {
            return string.Empty;
        }

        var length = NativeMethods.SendMessage(
            _focusedHandle,
            NativeMethods.WmGetTextLength,
            IntPtr.Zero,
            IntPtr.Zero).ToInt32();

        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(length, MaxMirrorLength) + 1);
        NativeMethods.SendMessage(
            _focusedHandle,
            NativeMethods.WmGetText,
            new IntPtr(builder.Capacity),
            builder);

        return builder.ToString();
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
