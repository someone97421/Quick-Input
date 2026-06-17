using System.Runtime.InteropServices;
using QuickInput.Interop;

namespace QuickInput.Services;

internal static class UnicodeInputInjector
{
    private const int DefaultChunkSize = 32;
    private static readonly int InputSize = Marshal.SizeOf<NativeMethods.Input>();
    private static readonly TimeSpan DefaultChunkDelay = TimeSpan.FromMilliseconds(5);

    public static bool SendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        if (InputSize < 40)
        {
            return false;
        }

        foreach (var chunk in EnumerateChunks(text, DefaultChunkSize))
        {
            if (!SendChunk(chunk))
            {
                return false;
            }

            Thread.Sleep(DefaultChunkDelay);
        }

        return true;
    }

    private static bool SendChunk(string chunk)
    {
        var inputs = new NativeMethods.Input[chunk.Length * 2];

        for (var i = 0; i < chunk.Length; i++)
        {
            var scan = (ushort)chunk[i];
            var inputIndex = i * 2;

            inputs[inputIndex].Type = NativeMethods.InputKeyboard;
            inputs[inputIndex].U.Ki.WVk = 0;
            inputs[inputIndex].U.Ki.WScan = scan;
            inputs[inputIndex].U.Ki.DwFlags = NativeMethods.KeyeventfUnicode;

            inputs[inputIndex + 1].Type = NativeMethods.InputKeyboard;
            inputs[inputIndex + 1].U.Ki.WVk = 0;
            inputs[inputIndex + 1].U.Ki.WScan = scan;
            inputs[inputIndex + 1].U.Ki.DwFlags = NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup;
        }

        var sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            InputSize);

        return sent == inputs.Length;
    }

    private static IEnumerable<string> EnumerateChunks(string text, int maxChunkLength)
    {
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(maxChunkLength, text.Length - start);
            if (length < text.Length - start && char.IsHighSurrogate(text[start + length - 1]))
            {
                length--;
            }

            if (length <= 0)
            {
                length = Math.Min(2, text.Length - start);
            }

            yield return text.Substring(start, length);
            start += length;
        }
    }
}
