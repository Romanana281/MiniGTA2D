using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Project5
{
    public partial class Humon : UserControl
    {
        private ComponentResourceManager resources = new ComponentResourceManager(typeof(Humon));

        // Move
        public bool KeysUp = false;
        public bool KeysDown = false;
        public bool KeysRight = false;
        public bool KeysLeft = false;
        private float directionAngle = 0;
        private float baseRotation = 0;

        // Info
        protected int speed;
        public double health;
        private int type;
        public bool isDied = false;
        public DateTime deathTime;
        public bool deathProcessed = false;
        public Point WorldPosition;

        public Humon(int speed, double health, int type, Point WorldPosition)
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            this.speed = speed;
            this.health = health;
            this.type = type;
            this.WorldPosition = WorldPosition;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            string action;

            float finalAngle = directionAngle + baseRotation;

            if (isDied)
            {
                Image bloodImage = (Image)resources.GetObject("blood");

                e.Graphics.TranslateTransform(this.Width / 2f, this.Height / 2f);
                e.Graphics.RotateTransform(finalAngle);

                e.Graphics.DrawImage(bloodImage,
                    -bloodImage.Width / 2f,
                    -bloodImage.Height / 2f);

                e.Graphics.RotateTransform(-finalAngle);
                e.Graphics.TranslateTransform(-this.Width / 2f, -this.Height / 2f);
            }

            if (this.Tag.ToString().Contains("police"))
            {
                action = this.Tag.ToString().Split(';')[1];
            }
            else if ((KeysUp || KeysRight || KeysLeft || KeysDown) && !isDied) action = "run";
            else action = "stand";

            Image humonImage = (Image)resources.GetObject($"humon_{type}_{action}");

            e.Graphics.TranslateTransform(this.Width / 2f, this.Height / 2f);
            e.Graphics.RotateTransform(finalAngle);
            e.Graphics.DrawImage(humonImage, -humonImage.Width / 2f, -humonImage.Height / 2f);

            float rad = finalAngle * (float)Math.PI / 180f;
            float cos = (float)Math.Abs(Math.Cos(rad));
            float sin = (float)Math.Abs(Math.Sin(rad));

            int newWidth = (int)(humonImage.Width * cos + humonImage.Height * sin);
            int newHeight = (int)(humonImage.Width * sin + humonImage.Height * cos);

            this.Size = new Size(newWidth, newHeight);
        }

        private List<Car> GetNearbyCars(Map map, int checkRadius)
        {
            var nearbyCars = new List<Car>();

            foreach (var car in map.cars)
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

        private Dictionary<Point, Color> GetNearbyMapMask(Map map, int checkRadius)
        {
            var nearbyMask = new Dictionary<Point, Color>();

            int minX = Math.Max(-50, WorldPosition.X - checkRadius);
            int maxX = Math.Min(4096+50, WorldPosition.X + checkRadius);
            int minY = Math.Max(-50, WorldPosition.Y - checkRadius);
            int maxY = Math.Min(5120+50, WorldPosition.Y + checkRadius);

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

        public void UpdatePosition(Map map, Form form)
        {
            if (isDied)
            {
                this.Invalidate();
                return;
            }

            Point newPosition = WorldPosition;

            string action;
            if (KeysUp || KeysRight || KeysLeft || KeysDown) action = "run";
            else action = "stand";
            Image humonImage = (Image)resources.GetObject($"humon_{type}_{action}");

            int imageWidth = humonImage.Width;
            int imageHeight = humonImage.Height;

            bool moved = false;
            int moveX = 0;
            int moveY = 0;

            if (KeysUp && KeysRight)
            {
                if (WorldPosition.Y - speed >= -50 && WorldPosition.X + speed + imageWidth <= 4096+50)
                {
                    moveY -= speed;
                    moveX += speed;
                    directionAngle = -45;
                    moved = true;
                }
            }
            else if (KeysUp && KeysLeft)
            {
                if (WorldPosition.Y - speed >= -50 && WorldPosition.X - speed >= -50)
                {
                    moveY -= speed;
                    moveX -= speed;
                    directionAngle = -135;
                    moved = true;
                }
            }
            else if (KeysDown && KeysRight)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120+50 && WorldPosition.X + speed + imageWidth <= 4096+50)
                {
                    moveY += speed;
                    moveX += speed;
                    directionAngle = 45;
                    moved = true;
                }
            }
            else if (KeysDown && KeysLeft)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120+ 50 && WorldPosition.X - speed >= -50)
                {
                    moveY += speed;
                    moveX -= speed;
                    directionAngle = 135;
                    moved = true;
                }
            }
            else if (KeysUp)
            {
                if (WorldPosition.Y - speed >= -50)
                {
                    moveY -= speed;
                    directionAngle = -90;
                    moved = true;
                }
            }
            else if (KeysDown)
            {
                if (WorldPosition.Y + speed + imageHeight <= 5120 + 50)
                {
                    moveY += speed;
                    directionAngle = 90;
                    moved = true;
                }
            }
            else if (KeysRight)
            {
                if (WorldPosition.X + speed + imageWidth <= 4096 + 50)
                {
                    moveX += speed;
                    directionAngle = 0;
                    moved = true;
                }
            }
            else if (KeysLeft)
            {
                if (WorldPosition.X - speed >= -50)
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

                var nearbyCars = GetNearbyCars(map, 100);
                var nearbyMask = GetNearbyMapMask(map, 100);

                for (int x = 0; x <= imageWidth; x += 1)
                {
                    for (int y = 0; y <= imageHeight; y += 1)
                    {
                        if (nearbyMask.ContainsKey(new Point(x + newPosition.X, y + newPosition.Y)))
                        {
                            if (nearbyMask[new Point(x + newPosition.X, y + newPosition.Y)] != Color.FromArgb(0, 0, 0, 0))
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

                if (isMoved)
                {
                    WorldPosition = newPosition;

                    Map parentMap = this.Parent as Map;
                    if (parentMap != null && parentMap.player != null)
                    {
                        Player player = parentMap.player;

                        float playerCenterX = player.WorldPosition.X + player.Width / 2f;
                        float playerCenterY = player.WorldPosition.Y + player.Height / 2f;
                        float humonCenterX = WorldPosition.X + this.Width / 2f;
                        float humonCenterY = WorldPosition.Y + this.Height / 2f;

                        float dx = humonCenterX - playerCenterX;
                        float dy = humonCenterY - playerCenterY;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                        float minDistance = (player.Width / 2f) + (this.Width / 2f) + 10;

                        if (distance < minDistance && distance > 0)
                        {
                            float pushForce = (minDistance - distance) * 0.5f;

                            float pushX = (dx / distance) * pushForce;
                            float pushY = (dy / distance) * pushForce;

                            WorldPosition = new Point(
                                (int)(WorldPosition.X + pushX),
                                (int)(WorldPosition.Y + pushY)
                            );
                        }
                    }
                    else if (parentMap != null)
                    {
                        float humonCenterX = WorldPosition.X + this.Width / 2f;
                        float humonCenterY = WorldPosition.Y + this.Height / 2f;
                        float minHumonDistance = (this.Width / 2f) + 5;

                        foreach (var otherHumon in parentMap.humons)
                        {
                            if (otherHumon == this || otherHumon.isDied || otherHumon.IsDisposed)
                                continue;

                            float otherCenterX = otherHumon.WorldPosition.X + otherHumon.Width / 2f;
                            float otherCenterY = otherHumon.WorldPosition.Y + otherHumon.Height / 2f;

                            float dx = humonCenterX - otherCenterX;
                            float dy = humonCenterY - otherCenterY;
                            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                            if (distance < minHumonDistance && distance > 0)
                            {
                                float pushForce = (minHumonDistance - distance) * 0.8f;
                                float pushX = (dx / distance) * pushForce;
                                float pushY = (dy / distance) * pushForce;

                                WorldPosition = new Point(
                                    (int)(WorldPosition.X + pushX),
                                    (int)(WorldPosition.Y + pushY)
                                );

                                otherHumon.WorldPosition = new Point(
                                    (int)(otherHumon.WorldPosition.X - pushX),
                                    (int)(otherHumon.WorldPosition.Y - pushY)
                                );
                            }
                        }
                    }
                }
                else
                {
                    KeysUp = KeysDown = KeysLeft = KeysRight = false;
                }
            }
            this.Invalidate();
        }
    }
}