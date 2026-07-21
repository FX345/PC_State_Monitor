namespace PcGuardianLite.Core;

public static class DragPositionCalculator
{
    public static WindowPosition Calculate(
        double windowLeft,
        double windowTop,
        double startScreenX,
        double startScreenY,
        double currentScreenX,
        double currentScreenY)
    {
        return new WindowPosition(
            windowLeft + currentScreenX - startScreenX,
            windowTop + currentScreenY - startScreenY);
    }
}
