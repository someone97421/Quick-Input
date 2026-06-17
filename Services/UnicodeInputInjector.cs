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

        foreach (var inputs in BuildInputChunks(text, DefaultChunkSize))
        {
            if (!SendInputs(inputs))
            {
                return false;
            }

            Thread.Sleep(DefaultChunkDelay);
        }

        return true;
    }

    private static bool SendInputs(NativeMethods.Input[] inputs)
    {
        var sent = NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            InputSize);

        return sent == inputs.Length;
    }

    private static IEnumerable<NativeMethods.Input[]> BuildInputChunks(string text, int maxChunkLength)
    {
        var inputs = new List<NativeMethods.Input>(maxChunkLength * 2);
        var unitsInChunk = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r')
            {
                if (unitsInChunk >= maxChunkLength)
                {
                    yield return inputs.ToArray();
                    inputs.Clear();
                    unitsInChunk = 0;
                }

                AddShiftEnter(inputs);
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                unitsInChunk++;
                continue;
            }

            if (text[i] == '\n')
            {
                if (unitsInChunk >= maxChunkLength)
                {
                    yield return inputs.ToArray();
                    inputs.Clear();
                    unitsInChunk = 0;
                }

                AddShiftEnter(inputs);
                unitsInChunk++;
                continue;
            }

            var requiredUnits = i + 1 < text.Length &&
                                char.IsHighSurrogate(text[i]) &&
                                char.IsLowSurrogate(text[i + 1])
                ? 2
                : 1;

            if (unitsInChunk > 0 && unitsInChunk + requiredUnits > maxChunkLength)
            {
                yield return inputs.ToArray();
                inputs.Clear();
                unitsInChunk = 0;
            }

            AddUnicodeChar(inputs, text[i]);
            if (requiredUnits == 2)
            {
                i++;
                AddUnicodeChar(inputs, text[i]);
            }

            unitsInChunk += requiredUnits;
        }

        if (inputs.Count > 0)
        {
            yield return inputs.ToArray();
        }
    }

    private static void AddUnicodeChar(List<NativeMethods.Input> inputs, char character)
    {
        var scan = (ushort)character;

        inputs.Add(new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = 0,
                    WScan = scan,
                    DwFlags = NativeMethods.KeyeventfUnicode
                }
            }
        });

        inputs.Add(new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = 0,
                    WScan = scan,
                    DwFlags = NativeMethods.KeyeventfUnicode | NativeMethods.KeyeventfKeyup
                }
            }
        });
    }

    private static void AddShiftEnter(List<NativeMethods.Input> inputs)
    {
        AddVirtualKeyDown(inputs, NativeMethods.VkShift);
        AddVirtualKeyDown(inputs, NativeMethods.VkReturn);
        AddVirtualKeyUp(inputs, NativeMethods.VkReturn);
        AddVirtualKeyUp(inputs, NativeMethods.VkShift);
    }

    private static void AddVirtualKeyDown(List<NativeMethods.Input> inputs, ushort virtualKey)
    {
        inputs.Add(new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = virtualKey,
                    WScan = 0
                }
            }
        });
    }

    private static void AddVirtualKeyUp(List<NativeMethods.Input> inputs, ushort virtualKey)
    {
        inputs.Add(new NativeMethods.Input
        {
            Type = NativeMethods.InputKeyboard,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeyboardInput
                {
                    WVk = virtualKey,
                    WScan = 0,
                    DwFlags = NativeMethods.KeyeventfKeyup
                }
            }
        });
    }
}
