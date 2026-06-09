using System.Windows;
using QuickInput.Core;

namespace QuickInput.Services;

public static class WindowPlacementService
{
    private const double MinimumVisibleSize = 96;

    public static void Restore(Window window, OverlayPlacement placement)
    {
        var currentVirtual = GetVirtualBounds();
        var width = Clamp(placement.Width <= 0 ? 560 : placement.Width, 280, currentVirtual.Width);
        var height = Clamp(placement.Height <= 0 ? 220 : placement.Height, 140, currentVirtual.Height);

        var left = placement.Left;
        var top = placement.Top;

        if (placement.VirtualWidth > 0 && placement.VirtualHeight > 0)
        {
            var xRatio = (placement.Left - placement.VirtualLeft) / placement.VirtualWidth;
            var yRatio = (placement.Top - placement.VirtualTop) / placement.VirtualHeight;

            var oldWidthRatio = placement.Width / placement.VirtualWidth;
            var oldHeightRatio = placement.Height / placement.VirtualHeight;

            left = currentVirtual.Left + currentVirtual.Width * xRatio;
            top = currentVirtual.Top + currentVirtual.Height * yRatio;
            width = Clamp(currentVirtual.Width * oldWidthRatio, 280, currentVirtual.Width);
            height = Clamp(currentVirtual.Height * oldHeightRatio, 140, currentVirtual.Height);
        }
        else
        {
            left = currentVirtual.Left + (currentVirtual.Width - width) / 2;
            top = currentVirtual.Top + (currentVirtual.Height - height) / 3;
        }

        var safe = EnsureVisible(new Rect(left, top, width, height), currentVirtual);
        window.Left = safe.Left;
        window.Top = safe.Top;
        window.Width = safe.Width;
        window.Height = safe.Height;
    }

    public static OverlayPlacement Capture(Window window)
    {
        var virtualBounds = GetVirtualBounds();
        return new OverlayPlacement
        {
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            VirtualLeft = virtualBounds.Left,
            VirtualTop = virtualBounds.Top,
            VirtualWidth = virtualBounds.Width,
            VirtualHeight = virtualBounds.Height
        };
    }

    public static OverlayPlacement CenterDefault()
    {
        var virtualBounds = GetVirtualBounds();
        var width = Math.Min(560, Math.Max(320, virtualBounds.Width * 0.36));
        var height = Math.Min(220, Math.Max(160, virtualBounds.Height * 0.22));
        return new OverlayPlacement
        {
            Left = virtualBounds.Left + (virtualBounds.Width - width) / 2,
            Top = virtualBounds.Top + (virtualBounds.Height - height) / 3,
            Width = width,
            Height = height,
            VirtualLeft = virtualBounds.Left,
            VirtualTop = virtualBounds.Top,
            VirtualWidth = virtualBounds.Width,
            VirtualHeight = virtualBounds.Height
        };
    }

    private static Rect EnsureVisible(Rect rect, Rect bounds)
    {
        var width = Math.Min(rect.Width, bounds.Width);
        var height = Math.Min(rect.Height, bounds.Height);

        var left = rect.Left;
        var top = rect.Top;

        if (left + MinimumVisibleSize > bounds.Right)
        {
            left = bounds.Right - width;
        }

        if (top + MinimumVisibleSize > bounds.Bottom)
        {
            top = bounds.Bottom - height;
        }

        if (left + width - MinimumVisibleSize < bounds.Left)
        {
            left = bounds.Left;
        }

        if (top + height - MinimumVisibleSize < bounds.Top)
        {
            top = bounds.Top;
        }

        return new Rect(left, top, width, height);
    }

    private static Rect GetVirtualBounds()
    {
        return new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            Math.Max(320, SystemParameters.VirtualScreenWidth),
            Math.Max(240, SystemParameters.VirtualScreenHeight));
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Min(max, Math.Max(min, value));
    }
}
