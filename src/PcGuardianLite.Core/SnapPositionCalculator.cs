namespace PcGuardianLite.Core;

public static class SnapPositionCalculator
{
    public static WindowPosition Snap(
        double left,
        double top,
        double width,
        double height,
        double workAreaLeft,
        double workAreaTop,
        double workAreaRight,
        double workAreaBottom,
        double threshold)
    {
        var snappedLeft = left;
        var snappedTop = top;

        if (Math.Abs(left - workAreaLeft) <= threshold)
        {
            snappedLeft = workAreaLeft;
        }
        else if (Math.Abs((left + width) - workAreaRight) <= threshold)
        {
            snappedLeft = workAreaRight - width;
        }

        if (Math.Abs(top - workAreaTop) <= threshold)
        {
            snappedTop = workAreaTop;
        }
        else if (Math.Abs((top + height) - workAreaBottom) <= threshold)
        {
            snappedTop = workAreaBottom - height;
        }

        return new WindowPosition(snappedLeft, snappedTop);
    }
}
