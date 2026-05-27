using ISMA.Domain.Models;

namespace ISMA.Domain.Conversion;

/// <summary>
/// Line simplification algorithms for simulation result data.
/// Implements Radial-Distance and Douglas-Peucker algorithms.
/// </summary>
public static class ResultSimplifier
{
    /// <summary>
    /// Simplifies a sequence of simulation points using the Douglas-Peucker algorithm.
    /// </summary>
    /// <param name="points">The points to simplify.</param>
    /// <param name="tolerance">The maximum allowed distance from the simplified line.</param>
    /// <returns>The simplified sequence of points.</returns>
    public static IEnumerable<SimulationPoint> DouglasPeucker(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        var pointList = points.ToList();
        if (pointList.Count <= 2) return pointList;

        var keep = new bool[pointList.Count];
        keep[0] = true;
        keep[pointList.Count - 1] = true;

        DouglasPeuckerRecursive(pointList, 0, pointList.Count - 1, tolerance, keep);

        return pointList.Where((_, i) => keep[i]);
    }

    private static void DouglasPeuckerRecursive(
        List<SimulationPoint> points, int first, int last, double tolerance, bool[] keep)
    {
        double maxDist = 0;
        int maxIndex = 0;

        for (int i = first + 1; i < last; i++)
        {
            double dist = DistanceToSegment(points[first], points[last], points[i]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIndex = i;
            }
        }

        if (maxDist > tolerance)
        {
            keep[maxIndex] = true;
            DouglasPeuckerRecursive(points, first, maxIndex, tolerance, keep);
            DouglasPeuckerRecursive(points, maxIndex, last, tolerance, keep);
        }
    }

    private static double DistanceToSegment(SimulationPoint a, SimulationPoint b, SimulationPoint p)
    {
        if (a.YForDe.Length == 0 || b.YForDe.Length == 0)
            return Math.Sqrt((p.X - a.X) * (p.X - a.X));

        double dx = b.X - a.X;
        double dy = b.YForDe[0] - a.YForDe[0];
        double lenSq = dx * dx + dy * dy;

        if (lenSq == 0)
            return Math.Sqrt(
                (p.X - a.X) * (p.X - a.X) +
                (p.YForDe[0] - a.YForDe[0]) * (p.YForDe[0] - a.YForDe[0]));

        double t = ((p.X - a.X) * dx + (p.YForDe[0] - a.YForDe[0]) * dy) / lenSq;
        t = Math.Clamp(t, 0, 1);

        double projX = a.X + t * dx;
        double projY = a.YForDe[0] + t * dy;

        return Math.Sqrt(
            (p.X - projX) * (p.X - projX) +
            (p.YForDe[0] - projY) * (p.YForDe[0] - projY));
    }

    /// <summary>
    /// Simplifies a sequence of simulation points using the Radial-Distance algorithm.
    /// </summary>
    /// <param name="points">The points to simplify.</param>
    /// <param name="tolerance">The minimum distance between kept points.</param>
    /// <returns>The simplified sequence of points.</returns>
    public static IEnumerable<SimulationPoint> RadialDistance(
        IEnumerable<SimulationPoint> points, double tolerance)
    {
        var result = new List<SimulationPoint>();
        var pointList = points.ToList();
        if (pointList.Count == 0) return result;

        result.Add(pointList[0]);
        SimulationPoint lastKept = pointList[0];

        for (int i = 1; i < pointList.Count; i++)
        {
            double dist = Math.Sqrt(
                Math.Pow(pointList[i].X - lastKept.X, 2) +
                (pointList[i].YForDe.Length > 0 && lastKept.YForDe.Length > 0
                    ? Math.Pow(pointList[i].YForDe[0] - lastKept.YForDe[0], 2)
                    : 0));

            if (dist >= tolerance)
            {
                result.Add(pointList[i]);
                lastKept = pointList[i];
            }
        }

        if (!result.Contains(pointList[^1]))
            result.Add(pointList[^1]);

        return result;
    }
}
