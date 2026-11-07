using System.Windows.Automation;
namespace AutoCopy;
internal static class SelectionReader
{
    internal static SelectionResult Read(Native.Point point, nint window)
    {
        try
        {
            var focused = AutomationElement.FocusedElement;
            var underPointer = AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y));
            if (focused == null || underPointer == null) return new(SelectionKind.Unavailable, null);
            if (IsProtected(focused) || IsProtected(underPointer)) return new(SelectionKind.Protected, null);
            foreach (var start in new[] { underPointer })
            {
                var current = start;
                for (var i = 0; current != null && i < 16; i++, current = TreeWalker.ControlViewWalker.GetParent(current))
                {
                    if (current.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
                    {
                        var ranges = ((TextPattern)pattern).GetSelection();
                        var text = string.Concat(ranges.Select(range => range.GetText(-1)));
                        if (text.Length > 0) return new(SelectionKind.Text, text);
                    }
                }
            }
            return new(SelectionKind.Empty, null);
        }
        catch { return new(SelectionKind.Unavailable, null); }
    }
    private static bool IsProtected(AutomationElement element)
    {
        for (var current = element; current != null; current = TreeWalker.ControlViewWalker.GetParent(current))
            if (current.Current.IsPassword) return true;
        return false;
    }
}

