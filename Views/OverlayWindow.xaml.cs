using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using QuickInput.Interop;

namespace QuickInput.Views;

public partial class OverlayWindow : Window
{
    private bool _forceClose;
    private bool _updatingTextFromTarget;

    public event EventHandler<bool>? CloseRequested;
    public event EventHandler? LocationOrSizeChanged;
    public event EventHandler<string>? TextChangedByUser;

    public string CurrentText => InputBox.Text;

    public OverlayWindow(string initialText, string statusText)
    {
        InitializeComponent();

        _updatingTextFromTarget = true;
        InputBox.Text = initialText;
        _updatingTextFromTarget = false;
        StatusText.Text = statusText;

        SourceInitialized += OverlayWindow_OnSourceInitialized;
        LocationChanged += (_, _) => LocationOrSizeChanged?.Invoke(this, EventArgs.Empty);
        SizeChanged += (_, _) => LocationOrSizeChanged?.Invoke(this, EventArgs.Empty);
        Closing += OverlayWindow_OnClosing;
    }

    public void FocusInput()
    {
        Dispatcher.BeginInvoke(() =>
        {
            Activate();
            InputBox.Focus();
            Keyboard.Focus(InputBox);
            InputBox.CaretIndex = InputBox.Text.Length;
            InputBox.ScrollToEnd();
        }, DispatcherPriority.ApplicationIdle);
    }

    public void EnsureInputFocus()
    {
        var selectionStart = InputBox.SelectionStart;
        var selectionLength = InputBox.SelectionLength;
        var verticalOffset = InputBox.VerticalOffset;

        Activate();
        InputBox.Focus();
        Keyboard.Focus(InputBox);

        InputBox.SelectionStart = Math.Min(selectionStart, InputBox.Text.Length);
        InputBox.SelectionLength = Math.Min(selectionLength, InputBox.Text.Length - InputBox.SelectionStart);
        InputBox.ScrollToVerticalOffset(verticalOffset);
    }

    public void SetTargetText(string text)
    {
        if (InputBox.Text == text)
        {
            return;
        }

        var verticalOffset = InputBox.VerticalOffset;
        var caretIndex = Math.Min(InputBox.CaretIndex, text.Length);
        _updatingTextFromTarget = true;
        InputBox.Text = text;
        _updatingTextFromTarget = false;
        InputBox.CaretIndex = caretIndex;
        InputBox.ScrollToVerticalOffset(verticalOffset);
    }

    public void SetStatus(string statusText)
    {
        StatusText.Text = statusText;
    }

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void InputBox_OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseRequested?.Invoke(this, false);
        }
    }

    private void InputBox_OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_updatingTextFromTarget)
        {
            return;
        }

        TextChangedByUser?.Invoke(this, InputBox.Text);
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, false);
    }

    private void OverlayWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose)
        {
            return;
        }

        e.Cancel = true;
        CloseRequested?.Invoke(this, true);
    }

    private void OverlayWindow_OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        style |= NativeMethods.WsExToolWindow;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(style));
    }
}
