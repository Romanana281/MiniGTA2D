using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Project5
{
    public partial class Player : UserControl
    {
        private ComponentResourceManager resources = new ComponentResourceManager(typeof(Player));
        private Timer updateTimer = new Timer();

        // Move
        public bool KeysUp = false;
        public bool KeysDown = false;
        public bool KeysRight = false;
        public bool KeysLeft = false;
        public float directionAngle = 0;
        private float baseRotation = 0;

        // Info
        public int speed = 5;
        public double health = 5;
        public int money = 0;

        public int searchLevel = 0;
        public int wantedPoints = 0;
        public int searchTimer = 0;
        private int[] starThresholds = new int[] { 0, 20, 150, 300, 525, 700 };
        private int[] starTimer = new int[] { 0, 30, 60, 120, 150, 210 };

        public List<WeaponInfo> weapons = new List<WeaponInfo>();
        public WeaponInfo tekWeapon;
        public Point WorldPosition { get; set; } = new Point(575, 458);
        public int rampageKillCount = 0;

        public Player()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            updateTimer.Interval = 17;
            updateTimer.Tick += Update_Tick;
            updateTimer.Start();
        }

        private List<Car> GetNearbyCars(Map map, int checkRadius)
        {
            var nearbyCars = new List<Car>();
            var allCars = map.cars.Concat(map.policeCars.Cast<Car>());

            foreach (var car in allCars)
            {
                int dx = Math.Abs(car.WorldPosition.X - WorldPosition.X);
                int dy = Math.Abs(car.WorldPosition.Y - WorldPosition.Y);

                if (dx <= checkRadius && dy <= checkRadius)
                {
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= checkRadius)
                    {
                        nearbyCars.Add(car);
                    }
                }
            }

            return nearbyCars;
        }

        private List<House> GetNearbyHouses(Map map, int checkRadius)
        {
            var nearbyHouses = new List<House>();

            foreach (var house in map.houses)
            {
                int houseCenterX = house.startX + house.Width / 2;
                int houseCenterY = house.startY + house.Height / 2;

                int dx = Math.Abs(houseCenterX - WorldPosition.X);
                int dy = Math.Abs(houseCenterY - WorldPosition.Y);

                if (dx <= checkRadius && dy <= checkRadius)
                {
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= checkRadius)
                    {
                        nearbyHouses.Add(house);
                    }
                }
            }

            return nearbyHouses;
        }

        private Dictionary<Point, Color> GetNearbyMapMask(Map map, int checkRadius)
        {
            var nearbyMask = new Dictionary<Point, Color>();

            int minX = Math.Max(0, WorldPosition.X - checkRadius);
            int maxX = Math.Min(4096, WorldPosition.X + checkRadius);
            int minY = Math.Max(0, WorldPosition.Y - checkRadius);
            int maxY = Math.Min(5120, WorldPosition.Y + checkRadius);

            foreach (var kvp in map.mask)
            {
                if (kvp.Key.X >= minX && kvp.Key.X <= maxX &&
                    kvp.Key.Y >= minY && kvp.Key.Y <= maxY)
                {
                    nearbyMask.Add(kvp.Key, kvp.Value);
                }
            }

            return nearbyMask;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string action;

            if (tekWeapon == null)
                action = (KeysUp || KeysRight || KeysLeft || KeysDown) ? "run" : "stand";
            else if (tekWeapon.tekRechargeTime > 0)
                action = "reload";
            else action = tekWeapon.type == 0 ? "gun" : "machine";

            Image playerImage = (Image)resources.GetObject($"player_{action}");

            float finalAngle = directionAngle + baseRotation;

            e.Graphics.TranslateTransform(this.Width / 2f, this.Height / 2f);
            e.Graphics.RotateTransform(finalAngle);
            e.Graphics.DrawImage(playerImage, -playerImage.Width / 2f, -playerImage.Height / 2f);

            float rad = finalAngle * (float)Math.PI / 180f;
            float cos = (float)Math.Abs(Math.Cos(rad));
            float sin = (float)Math.Abs(Math.Sin(rad));

            int newWidth = (int)(playerImage.Width * cos + playerImage.Height * sin);
            int newHeight = (int)(playerImage.Width * sin + playerImage.Height * cos);

            this.Size = new Size(newWidth, newHeight);
        }

        public void UpdatePosition(Map map, Form form)
        {
            Point newPosition = WorldPosition;

            string action;
            if (tekWeapon == null)
                action = (KeysUp || KeysRight || KeysLeft || KeysDown) ? "run" : "stand";
            else if (tekWeapon.tekRechargeTime > 0)
                action = "reload";
            else action = tekWeapon.type == 0 ? "gun" : "machine";

            Image playerImage = (Image)resources.GetObject($"player_{action}");
            if (playerImage == null) return;

            int imageWidth = playerImage.Width;
            int imageHeight = playerImage.Height;

            bool moved = false;
            int moveX = 0;
            int moveY = 0;

            if (KeysUp && KeysRight)
            {
                if (WorldPosition.Y - speed >= 0 && WorldPosition.X + speed + imageWidth <= 4096)
                {
                    moveY -= speed;
                    moveX += speed;
                    directionAngle = -45;
                    moved = true;
                }
            }
            else if (KeysUp && KeysLeft)
            {
                if (WorldPosition.Y - speed >= 0 && WorldPosition.X - speed >= 0)
                {
                    moveY -= speed;
                    moveX -= speed;
                    directionAngle = -135;
                    moved = true;
                }
            }
            else if (KeysDown && KeysRight)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120 && WorldPosition.X + speed + imageWidth <= 4096)
                {
                    moveY += speed;
                    moveX += speed;
                    directionAngle = 45;
                    moved = true;
                }
            }
            else if (KeysDown && KeysLeft)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120 && WorldPosition.X - speed >= 0)
                {
                    moveY += speed;
                    moveX -= speed;
                    directionAngle = 135;
                    moved = true;
                }
            }
            else if (KeysUp)
            {
                if (WorldPosition.Y - speed >= 0)
                {
                    moveY -= speed;
                    directionAngle = -90;
                    moved = true;
                }
            }
            else if (KeysDown)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120)
                {
                    moveY += speed;
                    directionAngle = 90;
                    moved = true;
                }
            }
            else if (KeysRight)
            {
                if (WorldPosition.X + speed + imageWidth <= 4096)
                {
                    moveX += speed;
                    directionAngle = 0;
                    moved = true;
                }
            }
            else if (KeysLeft)
            {
                if (WorldPosition.X - speed >= 0)
                {
                    moveX -= speed;
                    directionAngle = 180;
                    moved = true;
                }
            }

            if (moved)
            {
                if (moveX != 0 && moveY != 0)
                {
                    float diagonalSpeed = speed * 0.7071f;
                    moveX = (int)(Math.Sign(moveX) * diagonalSpeed);
                    moveY = (int)(Math.Sign(moveY) * diagonalSpeed);
                }

                newPosition.X += moveX;
                newPosition.Y += moveY;

                bool isMoved = true;
                if (map.tekHouse == null)
                {
                    var nearbyCars = GetNearbyCars(map, 100);
                    var nearbyMaps = GetNearbyMapMask(map, 100);
                    var nearbyHouses = GetNearbyHouses(map, 500);

                    for (int x = 0; x <= imageWidth; x += 1)
                    {
                        for (int y = 0; y <= imageHeight; y += 1)
                        {
                            if (nearbyMaps.ContainsKey(new Point(x + newPosition.X, y + newPosition.Y)))
                            {
                                if (nearbyMaps[new Point(x + newPosition.X, y + newPosition.Y)] != Color.FromArgb(0, 0, 0, 0))
                                {
                                    isMoved = false;
                                    break;
                                }
                            }
                        }
                        if (!isMoved) break;
                    }

                    foreach (var c in nearbyCars)
                    {
                        var mask = c.createMask();
                        for (int x = 0; x <= imageWidth; x += 1)
                        {
                            for (int y = 0; y <= imageHeight; y += 1)
                            {
                                if (mask.Contains(new Point(x + newPosition.X, y + newPosition.Y)))
                                {
                                    isMoved = false;
                                    break;
                                }
                            }
                            if (!isMoved) break;
                        }
                    }

                    foreach (var c in nearbyCars)
                    {
                        var door = c.createDoor();

                        int minX = int.MaxValue;
                        int minY = int.MaxValue;
                        int maxX = int.MinValue;
                        int maxY = int.MinValue;

                        foreach (var d in door)
                        {
                            if (d.X < minX) minX = d.X;
                            if (d.Y < minY) minY = d.Y;
                            if (d.X > maxX) maxX = d.X;
                            if (d.Y > maxY) maxY = d.Y;
                        }

                        for (int x = minX; x <= maxX; x += 1)
                        {
                            for (int y = minY; y <= maxY; y += 1)
                            {
                                double distance = int.MaxValue;

                                if (directionAngle == 90 || directionAngle == 0)
                                {
                                    distance = Math.Sqrt(
                                        Math.Pow(x - newPosition.X - playerImage.Width, 2) +
                                        Math.Pow(y - newPosition.Y - playerImage.Height, 2)
                                    );
                                }
                                else if (directionAngle == -90)
                                {
                                    distance = Math.Sqrt(
                                        Math.Pow(x - newPosition.X - playerImage.Width, 2) +
                                        Math.Pow(y - newPosition.Y, 2)
                                    );
                                }
                                else if (directionAngle == 180)
                                {
                                    distance = Math.Sqrt(
                                        Math.Pow(x - newPosition.X, 2) +
                                        Math.Pow(y - newPosition.Y - playerImage.Height, 2)
                                    );
                                }

                                if (distance <= 30)
                                {
                                    c.isActive = true;
                                    c.Focus();
                                }
                                else
                                {
                                    c.isActive = false;
                                }

                            }
                        }
                    }

                    foreach (var h in nearbyHouses)
                    {
                        for (int x = h.doorOut[0].X; x <= h.doorOut[1].X; x += 1)
                        {
                            for (int y = h.doorOut[0].Y; y <= h.doorOut[1].Y; y += 1)
                            {
                                double distance = int.MaxValue;

                                if (directionAngle == 0)
                                {
                                    distance = Math.Sqrt(
                                        Math.Pow(x - (newPosition.X + playerImage.Width), 2) +
                                        Math.Pow(y - newPosition.Y, 2)
                                    );
                                }
                                else
                                {
                                    distance = Math.Sqrt(
                                        Math.Pow(x - newPosition.X, 2) +
                                        Math.Pow(y - newPosition.Y, 2)
                                    );
                                }

                                if (distance <= 1)
                                {
                                    map.tekHouse = h;
                                    int hx = 0, hy = 0;

                                    if (h.baseRotation == 90)
                                    {
                                        hx += 22;
                                    }
                                    else
                                    {
                                        hy += 22;
                                    }

                                    h.WorldPositionPlayer = this.WorldPosition;

                                    this.WorldPosition = new Point(
                                        form.Width / 2 - h.Width / 2 + ((h.doorIn[0].X + h.doorIn[1].X) / 2) - imageWidth / 2 + hx,
                                        form.Height / 2 - h.Height / 2 + ((h.doorIn[0].Y + h.doorIn[1].Y) / 2) - imageHeight / 2 + hy
                                    );

                                    return;
                                }
                            }
                        }
                    }

                    CheckWeaponPickups(map);
                }
                else
                {
                    for (int x = 0; x <= imageWidth; x += 1)
                    {
                        for (int y = 0; y <= imageHeight; y += 1)
                        {
                            if (map.tekHouse.mask.ContainsKey(
                                new Point(
                                    newPosition.X - form.Width / 2 + map.tekHouse.Width / 2 + x,
                                    newPosition.Y - form.Height / 2 + map.tekHouse.Height / 2 + y)
                                ))
                            {
                                if (map.tekHouse.mask[
                                    new Point(
                                    newPosition.X - form.Width / 2 + map.tekHouse.Width / 2 + x,
                                    newPosition.Y - form.Height / 2 + map.tekHouse.Height / 2 + y)
                                    ] != Color.FromArgb(0, 0, 0, 0)
                                    )
                                {
                                    isMoved = false;
                                    break;
                                }
                            }
                        }
                        if (!isMoved) break;
                    }

                    for (int x = map.tekHouse.doorIn[0].X; x <= map.tekHouse.doorIn[1].X; x += 1)
                    {
                        for (int y = map.tekHouse.doorIn[0].Y; y <= map.tekHouse.doorIn[1].Y; y += 1)
                        {
                            double distance = Math.Sqrt(
                                    Math.Pow(x - (newPosition.X - form.Width / 2 + map.tekHouse.Width / 2), 2) +
                                    Math.Pow(y - (newPosition.Y - form.Height / 2 + map.tekHouse.Height / 2), 2)
                            );

                            if (distance <= 1)
                            {
                                this.WorldPosition = map.tekHouse.WorldPositionPlayer;
                                map.tekHouse = null;
                                return;
                            }
                        }
                    }
                }

                if (isMoved)
                {
                    WorldPosition = newPosition;

                    foreach (var humon in map.humons.ToArray())
                    {
                        if (humon == null || humon.IsDisposed) continue;

                        float playerCenterX = WorldPosition.X + this.Width / 2f;
                        float playerCenterY = WorldPosition.Y + this.Height / 2f;
                        float humonCenterX = humon.WorldPosition.X + humon.Width / 2f;
                        float humonCenterY = humon.WorldPosition.Y + humon.Height / 2f;

                        float dx = playerCenterX - humonCenterX;
                        float dy = playerCenterY - humonCenterY;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        float minDistance = (this.Width / 2f) + (humon.Width / 2f) + 10;

                        if (distance < minDistance && distance > 0)
                        {
                            float pushForce = (minDistance - distance) * 0.3f;

                            float pushX = (dx / distance) * pushForce;
                            float pushY = (dy / distance) * pushForce;

                            WorldPosition = new Point(
                                (int)(WorldPosition.X + pushX),
                                (int)(WorldPosition.Y + pushY)
                            );
                        }
                    }
                }
            }
        }

        private void CheckWeaponPickups(Map map)
        {
            foreach (var pickup in map.weapons.ToArray())
            {
                double distance = Math.Sqrt(
                    Math.Pow(pickup.WorldPosition.X - WorldPosition.X, 2) +
                    Math.Pow(pickup.WorldPosition.Y - WorldPosition.Y, 2)
                );

                if (distance < 75)
                {
                    if (weapons.Count < 2)
                    {
                        weapons.Add(pickup);
                        if (tekWeapon == null)
                            tekWeapon = pickup;
                    }
                    else
                    {
                        tekWeapon = pickup;
                    }

                    map.weapons.Remove(pickup);
                }
            }
        }

        private void Update_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        public void UpdateWantedLevel()
        {
            int newLevel = 0;
            for (int i = starThresholds.Length - 1; i >= 1; i--)
            {
                if (wantedPoints >= starThresholds[i])
                {
                    newLevel = i;
                    searchTimer = starTimer[i];
                    break;
                }
            }
            searchLevel = newLevel;
        }

        public void ResetControls()
        {
            KeysUp = false;
            KeysDown = false;
            KeysLeft = false;
            KeysRight = false;
        }
    }
}