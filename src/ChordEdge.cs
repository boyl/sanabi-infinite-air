namespace SanabiInfiniteAir;

internal sealed class ChordEdge
{
    private bool wasHeld;

    internal bool Update(bool held)
    {
        bool pressed = held && !wasHeld;
        wasHeld = held;
        return pressed;
    }
}
