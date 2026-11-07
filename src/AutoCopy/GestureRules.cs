namespace AutoCopy;
public static class GestureRules
{
    public static bool ShouldRead(int clicks, bool dragged, bool sameWindow) => sameWindow && (dragged || clicks >= 2);
}
