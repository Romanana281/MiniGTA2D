using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Project5
{
    public class LaneAStar
    {
        private WayMask way;
        private const int STEP = 8;
        public LaneAStar(WayMask way)
        {
            this.way = way;
        }
        public List<Point> FindPath(Point start, Point goal, int lane)
        {
            var open = new List<AStarNode>();
            var closed = new HashSet<Point>();

            Point realStart = FindNearestRoad(start, lane);

            open.Add(new AStarNode
            {
                Pos = realStart,
                G = 0,
                H = Heuristic(realStart, goal)
            });

            while (open.Count > 0)
            {
                var current = open.OrderBy(n => n.F).First();

                if (Distance(current.Pos, goal) < STEP * 2)
                    return BuildPath(current);

                open.Remove(current);
                closed.Add(current.Pos);

                foreach (var next in Neighbors(current.Pos))
                {
                    if (closed.Contains(next))
                        continue;

                    if (!way.mask.TryGetValue(next, out var info))
                        continue;

                    if (!info.IsRoad)
                        continue;

                    float g = current.G + STEP;

                    if (info.Lane != 0 && info.Lane != lane)
                        g += 60;

                    g += DirectionPenalty(current.Pos, next, info.PreferredDirection);

                    var exist = open.FirstOrDefault(n => n.Pos == next);
                    if (exist == null)
                    {
                        open.Add(new AStarNode
                        {
                            Pos = next,
                            G = g,
                            H = Heuristic(next, goal),
                            Parent = current
                        });
                    }
                    else if (g < exist.G)
                    {
                        exist.G = g;
                        exist.Parent = current;
                    }
                }
            }

            return null;
        }
        private IEnumerable<Point> Neighbors(Point p)
        {
            yield return new Point(p.X + STEP, p.Y);
            yield return new Point(p.X - STEP, p.Y);
            yield return new Point(p.X, p.Y + STEP);
            yield return new Point(p.X, p.Y - STEP);
        }
        private float DirectionPenalty(Point from, Point to, float preferred)
        {
            Vector2 v = new Vector2(
                to.X - from.X,
                to.Y - from.Y);

            float moveAngle =
                (float)(Math.Atan2(v.X, -v.Y) * 180 / Math.PI);
            if (moveAngle < 0) moveAngle += 360;

            float diff = Math.Abs(moveAngle - preferred);
            diff = Math.Min(diff, 360 - diff);

            if (diff > 100)
                return 200;

            return diff * 0.5f;
        }
        private float Heuristic(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
        private float Distance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
        private Point FindNearestRoad(Point p, int lane)
        {
            return way.mask
                .Where(m => m.Value.IsRoad &&
                       (m.Value.Lane == lane || m.Value.Lane == 0))
                .OrderBy(m => Distance(m.Key, p))
                .First().Key;
        }
        private List<Point> BuildPath(AStarNode n)
        {
            var path = new List<Point>();
            while (n != null)
            {
                path.Add(n.Pos);
                n = n.Parent;
            }
            path.Reverse();
            return path;
        }
    }
}
