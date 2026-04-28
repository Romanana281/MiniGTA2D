using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;


namespace Project5
{
    public class WayMask
    {
        private Map map;

        public Dictionary<Point, RoadInfo> mask = new Dictionary<Point, RoadInfo>();
        public List<SpawnCar> spawn = new List<SpawnCar>();
        private List<Point> interimSpawn = new List<Point>();

        private int tilesX = 4;
        private int tilesY = 5;
        private int tileSize = 1024;
        public WayMask(Map map)
        {
            this.map = map;
        }

        public event Action<string, int> ProgressChanged;
        protected virtual void OnProgressChanged(string message, int progress)
        {
            ProgressChanged?.Invoke(message, progress);
        }
        public void CreateMask()
        {
            Image image;
            Bitmap img;
            List<Point> allSpawnPoints = new List<Point>();

            int totalTiles = tilesX * tilesY;
            int processedTiles = 0;
            for (int i = 1; i <= tilesX; i++)
            {
                for (int j = 1; j <= tilesY; j++)
                {
                    processedTiles++;
                    int progress = (processedTiles * 100) / totalTiles;
                    OnProgressChanged($"Обработка дорог: tile {i}x{j}", progress);
                    image = (Image)Properties.Resources.ResourceManager.GetObject($"way_tile_{i}_{j}");
                    img = new Bitmap(image);
                    for (int x = 0; x < tileSize; x += 4)
                    {
                        for (int y = 0; y < tileSize; y += 4)
                        {
                            Color pixelColor = img.GetPixel(x, y);
                            int worldX = x + (i - 1) * tileSize;
                            int worldY = y + (j - 1) * tileSize;
                            Point worldPoint = new Point(worldX, worldY);

                            if (pixelColor.R > 200 && pixelColor.G < 100 && pixelColor.B < 100)
                            {
                                continue;
                            }

                            var info = new RoadInfo { IsRoad = true };

                            if (pixelColor == Color.FromArgb(0, 162, 232))
                            {
                                allSpawnPoints.Add(worldPoint);
                            }
                            else if (pixelColor == Color.FromArgb(255, 242, 0))
                            {
                                info.Lane = 1;
                            }
                            else if (pixelColor == Color.FromArgb(255, 174, 201))
                            {
                                info.Lane = 2;
                            }
                            else if (pixelColor == Color.FromArgb(34, 177, 76))
                            {
                                info.IsIntersection = true;
                                info.Lane = 0;
                            }
                            else 
                            {
                                info.Lane = 0;
                            }

                            float direction = GetDirectionFromContext(pixelColor, img, x, y);
                            info.PreferredDirection = direction;


                            mask[worldPoint] = info;
                        }
                    }

                    interimSpawn = MergeSpawnPoints(allSpawnPoints);
                    CreateSpawn();
                    OnProgressChanged("Маска дорог создана", 100);
                }
            }
        }
        private int GetDirectionFromContext(Color c, Bitmap img, int x, int y)
        {
            bool isRightLane = (c == Color.FromArgb(255, 242, 0));
            bool isPinkLane = (c == Color.FromArgb(255, 174, 201));

            int checkDist = 4;
            bool hasLeftRight = false;
            bool hasUpDown = false;

            bool leftSame = false, rightSame = false;
            for (int d = 0; d <= checkDist; d += 4)
            {
                if (x - d >= 0 && img.GetPixel(x - d, y) == c) leftSame = true;
                if (x + d < img.Width && img.GetPixel(x + d, y) == c) rightSame = true;
            }
            hasLeftRight = leftSame && rightSame;

            bool upSame = false, downSame = false;
            for (int d = 0; d <= checkDist; d += 4)
            {
                if (y - d >= 0 && img.GetPixel(x, y - d) == c) upSame = true;
                if (y + d < img.Height && img.GetPixel(x, y + d) == c) downSame = true;
            }
            hasUpDown = upSame && downSame;

            if (hasLeftRight && !hasUpDown)
            {
                if (isRightLane)
                {
                    return 0;
                }
                else if (isPinkLane)
                {
                    return 180;
                }
            }
            else if (hasUpDown && !hasLeftRight)
            {
                if (isRightLane)
                {
                    return 90;
                }
                else if (isPinkLane)
                {
                    return 270;
                }
            }


            return 0;
        }
        private void CreateSpawn()
        {
            for (int i = 0; i < interimSpawn.Count; i += 2)
            {
                Point start = interimSpawn[i];
                Point end = interimSpawn[i + 1];

                int dx = end.X - start.X;
                int dy = end.Y - start.Y;

                int centerX = start.X + dx / 2;
                int centerY = start.Y + dy / 2;

                SpawnCar entrance = new SpawnCar();
                SpawnCar exit = new SpawnCar();

                bool isVertical = dy > dx;

                if (start.X <= map.Width / 2 && start.Y <= map.Height / 2)
                {
                    if (isVertical)
                    {
                        entrance.start = start;
                        entrance.end = new Point(end.X, centerY);
                        entrance.angle = 270;
                        entrance.type = "entrance";

                        exit.start = new Point(start.X, centerY);
                        exit.end = end;
                        exit.angle = 90;
                        exit.type = "exit";
                    }
                    else
                    {
                        entrance.start = new Point(centerX, start.Y);
                        entrance.end = end;
                        entrance.angle = 0;
                        entrance.type = "entrance";

                        exit.start = start;
                        exit.end = new Point(centerX, end.Y);
                        exit.angle = 180;
                        exit.type = "exit";
                    }
                }
                else if (start.X <= map.Width / 2 && start.Y > map.Height / 2)
                {
                    if (isVertical)
                    {
                        entrance.start = new Point(start.X, centerY);
                        entrance.end = end;
                        entrance.angle = 90;
                        entrance.type = "entrance";

                        exit.start = start;
                        exit.end = new Point(end.X, centerY);
                        exit.angle = 270;
                        exit.type = "exit";
                    }
                    else
                    {
                        entrance.start = start;
                        entrance.end = new Point(centerX, end.Y);
                        entrance.angle = 180;
                        entrance.type = "entrance";

                        exit.start = new Point(centerX, start.Y);
                        exit.end = end;
                        exit.angle = 0;
                        exit.type = "exit";
                    }
                }
                else if (start.X > map.Width / 2 && start.Y <= map.Height / 2)
                {
                    if (isVertical)
                    {
                        entrance.start = new Point(start.X, centerY);
                        entrance.end = end;
                        entrance.angle = 90;
                        entrance.type = "entrance";

                        exit.start = start;
                        exit.end = new Point(end.X, centerY);
                        exit.angle = 270;
                        exit.type = "exit";
                    }
                    else
                    {
                        entrance.start = new Point(centerX, start.Y);
                        entrance.end = end;
                        entrance.angle = 0;
                        entrance.type = "entrance";

                        exit.start = start;
                        exit.end = new Point(centerX, end.Y);
                        exit.angle = 180;
                        exit.type = "exit";
                    }
                }
                else if (start.X > map.Width / 2 && start.Y > map.Height / 2)
                {
                    if (isVertical)
                    {
                        entrance.start = new Point(start.X, centerY);
                        entrance.end = end;
                        entrance.angle = 90;
                        entrance.type = "entrance";

                        exit.start = start;
                        exit.end = new Point(end.X, centerY);
                        exit.angle = 270;
                        exit.type = "exit";
                    }
                    else
                    {
                        entrance.start = start;
                        entrance.end = new Point(centerX, end.Y);
                        entrance.angle = 180;
                        entrance.type = "entrance";

                        exit.start = new Point(centerX, start.Y);
                        exit.end = end;
                        exit.angle = 0;
                        exit.type = "exit";
                    }
                }

                spawn.Add(entrance);
                spawn.Add(exit);
            }
        }
        private List<Point> MergeSpawnPoints(List<Point> points)
        {
            if (points.Count == 0)
                return new List<Point>();

            List<List<Point>> clusters = new List<List<Point>>();

            foreach (var point in points)
            {
                bool added = false;

                foreach (var cluster in clusters)
                {
                    if (cluster.Any(p => IsNearby(p, point)))
                    {
                        cluster.Add(point);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    clusters.Add(new List<Point> { point });
                }
            }

            List<Point> result = new List<Point>();

            foreach (var cluster in clusters)
            {
                int minX = cluster.Min(p => p.X);
                int maxX = cluster.Max(p => p.X);
                int minY = cluster.Min(p => p.Y);
                int maxY = cluster.Max(p => p.Y);

                result.Add(new Point(minX, minY));
                result.Add(new Point(maxX, maxY));
            }

            return result;
        }
        private bool IsNearby(Point p1, Point p2)
        {
            int distance = 5;
            return Math.Abs(p1.X - p2.X) < distance && Math.Abs(p1.Y - p2.Y) < distance;
        }

    }
}

