namespace AutoCopy;

public enum SelectionKind { Text, Empty, Unavailable, Protected, Error }
public record SelectionResult(SelectionKind Kind, string? Text);
public static class CopyRules
{
    public static bool Copy(SelectionResult selection, Action<string> write)
    {
        if (selection.Kind != SelectionKind.Text || string.IsNullOrEmpty(selection.Text)) return false;
        write(selection.Text);
        return true;
    }
}
