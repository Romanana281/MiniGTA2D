using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Project5
{
    public partial class Car : UserControl
    {
        protected ComponentResourceManager resources = new ComponentResourceManager(typeof(Car));

        private Timer updateTimer = new Timer();
        private Timer animationTimer = new Timer();

        protected Map map;

        // Move
        public bool KeysUp { get; set; } = false;
        public bool KeysDown { get; set; } = false;
        public bool KeysLeft { get; set; } = false;
        public bool KeysRight { get; set; } = false;
        public bool KeysSpace { get; set; } = false;

        public float Rotation;
        private bool isSteel = false;

        //Info
        public int maxSpeed;
        public float speed;
        private float acceleration;
        private float deceleration;
        public float health;
        public int type;

        public bool isDied = false;
        public DateTime deathTime;
        public bool deathProcessed = false;

        public bool isActive = false;
        public bool inCar = false;
        public Point WorldPosition;

        //Animation
        protected Image fire;
        protected int tekAction = -1;
        private bool doorsOpen = false;
        protected string[] action = new string[]
        {
            "closed",
            "startOpen",
            "open"
        };

        // AI
        public List<Point> Path;
        protected int pathIndex = 0;
        private LaneAStar pathfinder;
        public Point TargetPoint;
        public int Lane = 1;
        private bool decisionMade = false;
        private bool isPathfindingInProgress = false;
        private readonly object pathfindingLock = new object();
        protected float aiSpeed = 0f;
        protected const float aiMaxSpeed = 7f;
        protected const float aiAcceleration = 0.3f;
        protected const float aiDeceleration = 0.4f;
        protected const float aiBrakePower = 0.8f;
        protected const float aiTurnSpeedReduction = 0.6f;

        private List<Point> cachedMask = null;
        private float cachedRotation = -999f;
        private Point cachedPosition = new Point(-999, -999);

        public Car(Map map, Point WorldPosition, int type)
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            updateTimer.Interval = 17;
            updateTimer.Tick += Update_Tick;
            updateTimer.Start();

            animationTimer.Interval = 100;
            animationTimer.Tick += AnimationTimer_Tick;

            this.map = map;
            this.WorldPosition = WorldPosition;
            this.type = type;
            CreateInfo();

            fire = (Image)resources.GetObject("fire");
            ImageAnimator.Animate(fire, (s, e) => this.Invalidate());

            pathfinder = new LaneAStar(map.wayMask);
        }

        private void CreateInfo()
        {

            switch (type)
            {
                case 1:
                    maxSpeed = 18;
                    acceleration = 0.25f;
                    deceleration = 0.3f;
                    health = 13f;
                    break;
                case 2:
                    maxSpeed = 22;
                    acceleration = 0.3f;
                    deceleration = 0.25f;
                    health = 12f;
                    break;
                case 3:
                    maxSpeed = 20;
                    acceleration = 0.22f;
                    deceleration = 0.28f;
                    health = 15f;
                    break;
                case 4:
                    maxSpeed = 23;
                    acceleration = 0.4f;
                    deceleration = 0.35f;
                    health = 11f;
                    break;
                case 5:
                    maxSpeed = 12;
                    acceleration = 0.12f;
                    deceleration = 0.15f;
                    health = 25f;
                    break;
                case 6:
                    maxSpeed = 24;
                    acceleration = 0.4f;
                    deceleration = 0.35f;
                    health = 20f;
                    break;
                case 7:
                    maxSpeed = 18;
                    acceleration = 0.22f;
                    deceleration = 0.28f;
                    health = 30f;
                    break;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!inCar)
            {
                if (e.KeyCode == Keys.E && isActive)
                    StartAnimation();

                return;
            }

            if (e.KeyCode == Keys.W) KeysUp = true;
            if (e.KeyCode == Keys.A) KeysLeft = true;
            if (e.KeyCode == Keys.S) KeysDown = true;
            if (e.KeyCode == Keys.D) KeysRight = true;
            if (e.KeyCode == Keys.Space) KeysSpace = true;
            if (e.KeyCode == Keys.E && isActive && !isDied) StartAnimation();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (!inCar) return;

            if (e.KeyCode == Keys.W) KeysUp = false;
            if (e.KeyCode == Keys.A) KeysLeft = false;
            if (e.KeyCode == Keys.S) KeysDown = false;
            if (e.KeyCode == Keys.D) KeysRight = false;
            if (e.KeyCode == Keys.Space) KeysSpace = false;
        }

        private void ResetControls()
        {
            KeysUp = false;
            KeysDown = false;
            KeysLeft = false;
            KeysRight = false;
            KeysSpace = false;
            speed = 0;
        }

        public virtual void Draw(Graphics g, Point cameraPosition)
        {
            Image carImage = (Image)resources.GetObject($"car_{type}_doors_{action[tekAction == -1 ? 0 : tekAction]}");

            int Width = (int)(carImage.Width * 1.3);
            int Height = (int)(carImage.Height * 1.3);

            GraphicsState state = g.Save();

            g.TranslateTransform(WorldPosition.X - cameraPosition.X, WorldPosition.Y - cameraPosition.Y);
            g.RotateTransform(Rotation);

            g.DrawImage(carImage, -Width / 2, -Height / 2, Width, Height);

            g.Restore(state);

            if (isDied)
            {
                ImageAnimator.UpdateFrames(fire);

                float screenX = WorldPosition.X - cameraPosition.X - fire.Width / 2;
                float screenY = WorldPosition.Y - cameraPosition.Y - fire.Height / 2;

                g.DrawImage(fire, screenX, screenY);
            }

            float rad = Rotation * (float)Math.PI / 180f;
            float cos = (float)Math.Abs(Math.Cos(rad));
            float sin = (float)Math.Abs(Math.Sin(rad));

            int newWidth = (int)(Width * cos + Height * sin);
            int newHeight = (int)(Width * sin + Height * cos);

            this.Size = new Size(newWidth, newHeight);
        }

        private Dictionary<Point, Color> GetNearbyMapMask(int checkRadius)
        {
            var nearbyMask = new Dictionary<Point, Color>();

            int minX = Math.Max(-50, WorldPosition.X - checkRadius);
            int maxX = Math.Min(4096 + 50, WorldPosition.X + checkRadius);
            int minY = Math.Max(-50, WorldPosition.Y - checkRadius);
            int maxY = Math.Min(5120 + 50, WorldPosition.Y + checkRadius);

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

        protected void UpdatePosition()
        {
            if (!inCar && this.Tag != "police_car") return;

            if (KeysLeft)
            {
                float newRotation = Rotation - 4;
                if (newRotation < 0) newRotation += 360;

                if (CanRotate(newRotation))
                {
                    Rotation = newRotation;
                }
            }

            if (KeysRight)
            {
                float newRotation = Rotation + 4;
                if (newRotation >= 360) newRotation -= 360;

                if (CanRotate(newRotation))
                {
                    Rotation = newRotation;
                }
            }

            if (KeysSpace)
            {
                if (speed > 0)
                {
                    speed -= deceleration * 3f;
                    if (speed < 0) speed = 0;
                }
                else if (speed < 0)
                {
                    speed += deceleration * 3f;
                    if (speed > 0) speed = 0;
                }
            }

            if (KeysUp)
            {
                speed += acceleration;
                if (speed > maxSpeed) speed = maxSpeed;
            }
            else if (KeysDown)
            {
                if (speed > 0)
                {
                    speed -= deceleration * 2;
                    if (speed < 0) speed = 0;
                }
                else
                {
                    speed -= acceleration;
                    if (speed < -maxSpeed) speed = -maxSpeed;
                }
            }
            else if (!KeysSpace)
            {
                if (speed > 0)
                {
                    speed -= deceleration;
                    if (speed < 0) speed = 0;
                }
                else if (speed < 0)
                {
                    speed += deceleration;
                    if (speed > 0) speed = 0;
                }
            }

            float angle = Rotation * (float)Math.PI / 180f;
            float moveX = (float)Math.Sin(angle) * speed;
            float moveY = -(float)Math.Cos(angle) * speed;

            bool isMoved = true;

            Bitmap carBitmap = null;
            if (type < 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_doors_closed"));
            else if (type == 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_no"));
            else carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}"));

            var nearbyMaps = GetNearbyMapMask(100);

            foreach (var m in createMask())
            {
                if (nearbyMaps.ContainsKey(new Point(m.X + (int)moveX, m.Y + (int)moveY)))
                {
                    if (nearbyMaps[new Point(m.X + (int)moveX, m.Y + (int)moveY)] != Color.FromArgb(0, 0, 0, 0))
                    {
                        isMoved = false;
                        break;
                    }
                }

                if (m.X + (int)moveX < -50 || m.Y + (int)moveY < -50 || m.X + (int)moveX > map.Width + 50 || m.Y + (int)moveY > map.Height + 50)
                {
                    isMoved = false;
                }

                if (!isMoved) break;
            }


            var allCars = map.cars.Concat(map.policeCars.Cast<Car>());

            foreach (var otherCar in allCars)
            {
                if (otherCar == this || otherCar.isDied) continue;

                float dx = otherCar.WorldPosition.X - (WorldPosition.X + (int)moveX);
                float dy = otherCar.WorldPosition.Y - (WorldPosition.Y + (int)moveY);
                float distSquared = dx * dx + dy * dy;

                if (distSquared < 80 * 80)
                {
                    isMoved = false;
                    break;
                }
            }

            if (this.Tag == "police_car")
            {
                if (map.tekCar != null)
                {
                    Car playerCar = map.tekCar;

                    float playerCarCenterX = playerCar.WorldPosition.X + playerCar.Width / 2f;
                    float playerCarCenterY = playerCar.WorldPosition.Y + playerCar.Height / 2f;

                    float policeFutureX = WorldPosition.X + (int)moveX + Width / 2f;
                    float policeFutureY = WorldPosition.Y + (int)moveY + Height / 2f;

                    float dx = policeFutureX - playerCarCenterX;
                    float dy = policeFutureY - playerCarCenterY;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (dist < 40f)
                    {
                        playerCar.health -= 2;

                        if (playerCar.health <= 0 && !playerCar.isDied)
                        {
                            playerCar.isDied = true;
                            playerCar.deathTime = DateTime.Now;
                            playerCar.deathProcessed = true;
                            playerCar.speed = 0;
                        }

                        speed *= 0.8f;
                    }
                }
                else
                {
                    float playerCenterX = map.player.WorldPosition.X + map.player.Width / 2f;
                    float playerCenterY = map.player.WorldPosition.Y + map.player.Height / 2f;

                    float policeFutureX = WorldPosition.X + (int)moveX + Width / 2f;
                    float policeFutureY = WorldPosition.Y + (int)moveY + Height / 2f;

                    float dx = policeFutureX - playerCenterX;
                    float dy = policeFutureY - playerCenterY;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (dist < 50f)
                    {
                        map.player.health -= 1;

                        if (map.player.health <= 0)
                        {
                            map.EndGame();
                            return;
                        }

                        float knockback = 20f + Math.Abs(speed) / 2f;
                        float knockAngle = (float)Math.Atan2(dy, dx);
                        map.player.WorldPosition = new Point(
                            map.player.WorldPosition.X + (int)(Math.Cos(knockAngle) * knockback),
                            map.player.WorldPosition.Y + (int)(Math.Sin(knockAngle) * knockback)
                        );

                        speed *= 0.8f;
                    }
                }
            }

            var allPedestrians = map.humons.Cast<Humon>().Concat(map.policeUnits.Cast<Humon>());

            foreach (var ped in allPedestrians.ToList())
            {
                if (ped == null || ped.IsDisposed || ped.isDied) continue;

                float pedCenterX = ped.WorldPosition.X + ped.Width / 2f;
                float pedCenterY = ped.WorldPosition.Y + ped.Height / 2f;

                float carFutureX = WorldPosition.X + (int)moveX + Width / 2f;
                float carFutureY = WorldPosition.Y + (int)moveY + Height / 2f;

                float dx = carFutureX - pedCenterX;
                float dy = carFutureY - pedCenterY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                if (dist < 50f)
                {
                    ped.health -= Math.Abs(speed) / 2f;

                    if (ped.health <= 0 && !ped.isDied)
                    {
                        ped.isDied = true;
                        ped.deathTime = DateTime.Now;
                        ped.deathProcessed = true;
                        ped.KeysUp = ped.KeysDown = ped.KeysLeft = ped.KeysRight = false;

                        if (this == map.tekCar)

                        {
                            if (map.policeUnits.Contains(ped))
                            {
                                map.player.wantedPoints += 100;
                                map.player.money += 200;
                                map.player.UpdateWantedLevel();
                            }
                            else
                            {
                                map.player.wantedPoints += 50;
                                map.player.money += 100;
                                map.player.UpdateWantedLevel();
                            }
                        }
                    }

                    float knockback = 30f + Math.Abs(speed);
                    float knockAngle = (float)Math.Atan2(dx, -dy);
                    ped.WorldPosition = new Point(
                        ped.WorldPosition.X + (int)(Math.Cos(knockAngle) * knockback),
                        ped.WorldPosition.Y + (int)(Math.Sin(knockAngle) * knockback)
                    );

                    speed *= 0.7f;
                }
            }

            if (isMoved)
            {
                WorldPosition = new Point(
                   WorldPosition.X + (int)moveX,
                   WorldPosition.Y + (int)moveY
                );
                cachedMask = null;
            }
            else
            {
                speed = -speed * 0.3f;

                float bounceBackDistance = 5f;
                float bounceAngle = angle + (float)Math.PI;

                float bounceX = (float)Math.Sin(bounceAngle) * bounceBackDistance;
                float bounceY = -(float)Math.Cos(bounceAngle) * bounceBackDistance;

                bool canBounceBack = true;
                foreach (var m in createMask())
                {
                    Point bouncePoint = new Point(m.X + (int)bounceX, m.Y + (int)bounceY);

                    if (nearbyMaps.ContainsKey(bouncePoint))
                    {
                        if (nearbyMaps[bouncePoint] != Color.FromArgb(0, 0, 0, 0))
                        {
                            canBounceBack = false;
                            break;
                        }
                    }

                    if (bouncePoint.X < -50 || bouncePoint.Y < -50 || bouncePoint.X > map.Width + 50 || bouncePoint.Y > map.Height + 50)
                    {
                        canBounceBack = false;
                        break;
                    }
                }

                if (canBounceBack && Math.Abs(speed) > 0.5f)
                {
                    WorldPosition = new Point(
                        WorldPosition.X + (int)bounceX,
                        WorldPosition.Y + (int)bounceY
                    );
                    cachedMask = null;
                }
                else
                {
                    speed = 0;
                }

            }

            Invalidate();
        }

        public void DrawDebugPath(Graphics g)
        {
            if (Path == null || Path.Count < 2)
                return;

            Color pathColor = Lane == 1
                ? Color.Gold
                : Color.HotPink;

            using (Pen pen = new Pen(pathColor, 2))
            {
                for (int i = 1; i < Path.Count; i++)
                {
                    g.DrawLine(pen, Path[i - 1].X - map.cameraPosition.X, Path[i - 1].Y - map.cameraPosition.Y, Path[i].X - map.cameraPosition.X, Path[i].Y - map.cameraPosition.Y);
                }
            }

            // текущая цель
            Point p = Path[Math.Min(pathIndex, Path.Count - 1)];
            g.FillEllipse(Brushes.Red, p.X - 4 - map.cameraPosition.X, p.Y - 4 - map.cameraPosition.Y, 8, 8);
        }

        protected virtual void UpdateAIPosition()
        {
            if (Path == null || Path.Count < 2 || pathIndex >= Path.Count)
            {
                aiSpeed = Math.Max(0, aiSpeed - aiDeceleration);
                if (aiSpeed < 0.1f) aiSpeed = 0;
                return;
            }

            bool onIntersection = IsOnIntersection();

            if (onIntersection && !decisionMade)
            {
                DecideOnIntersection();
                RecalculatePathAsync();
            }

            if (!onIntersection)
            {
                decisionMade = false;
            }

            Point currentTarget = Path[pathIndex];
            float dx = currentTarget.X - WorldPosition.X;
            float dy = currentTarget.Y - WorldPosition.Y;
            float distToTarget = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distToTarget < 50f)
            {
                pathIndex++;
                if (pathIndex >= Path.Count)
                {
                    aiSpeed = Math.Max(0, aiSpeed - aiDeceleration * 2);
                    if (aiSpeed < 1f) aiSpeed = 0;
                    return;
                }
                currentTarget = Path[pathIndex];
                dx = currentTarget.X - WorldPosition.X;
                dy = currentTarget.Y - WorldPosition.Y;
                distToTarget = (float)Math.Sqrt(dx * dx + dy * dy);
            }

            PointF lookAhead = currentTarget;
            if (pathIndex + 1 < Path.Count)
                lookAhead = Path[pathIndex + 1];

            float lookDx = lookAhead.X - WorldPosition.X;
            float lookDy = lookAhead.Y - WorldPosition.Y;
            float desiredAngle = (float)(Math.Atan2(lookDx, -lookDy) * 180f / Math.PI);
            if (desiredAngle < 0) desiredAngle += 360f;

            float angleDiff = desiredAngle - Rotation;
            while (angleDiff > 180) angleDiff -= 360;
            while (angleDiff < -180) angleDiff += 360;

            float turnPower = 0f;
            if (Math.Abs(angleDiff) > 5f)
            {
                float intensity = Math.Min(Math.Abs(angleDiff) / 90f, 1.8f);
                turnPower = Math.Sign(angleDiff) * (8f + intensity * 8f);
            }
            Rotation += turnPower;
            if (Rotation >= 360) Rotation -= 360;
            if (Rotation < 0) Rotation += 360;

            float targetSpeed = aiMaxSpeed;

            if (Math.Abs(angleDiff) > 30f)
            {
                targetSpeed *= aiTurnSpeedReduction;
            }

            if (distToTarget < 100f)
            {
                targetSpeed *= (distToTarget / 100f);
                targetSpeed = Math.Max(targetSpeed, 2f);
            }

            if (aiSpeed < targetSpeed)
            {
                aiSpeed += aiAcceleration;
                if (aiSpeed > targetSpeed) aiSpeed = targetSpeed;
            }
            else if (aiSpeed > targetSpeed)
            {
                aiSpeed -= aiBrakePower;
                if (aiSpeed < targetSpeed) aiSpeed = targetSpeed;
            }

            if (aiSpeed > 0)
            {
                aiSpeed -= aiDeceleration * 0.3f;
                if (aiSpeed < 0) aiSpeed = 0;
            }

            if (aiSpeed > 0.1f)
            {
                float rad = Rotation * (float)Math.PI / 180f;
                int moveX = (int)(Math.Sin(rad) * aiSpeed);
                int moveY = (int)(-Math.Cos(rad) * aiSpeed);

                Point newPos = new Point(WorldPosition.X + moveX, WorldPosition.Y + moveY);
                WorldPosition = newPos;
            }
        }

        protected bool IsOnIntersection()
        {
            const int checkRadius = 20;

            int centerX = WorldPosition.X;
            int centerY = WorldPosition.Y;

            for (int dx = -checkRadius; dx <= checkRadius; dx += 2)
            {
                for (int dy = -checkRadius; dy <= checkRadius; dy += 2)
                {
                    Point gridPoint = new Point(
                        ((centerX + dx) / 4) * 4,
                        ((centerY + dy) / 4) * 4
                    );

                    if (map.wayMask.mask.TryGetValue(gridPoint, out var info) && info.IsIntersection)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected void RecalculatePathAsync()
        {
            lock (pathfindingLock)
            {
                if (isPathfindingInProgress) return;
                isPathfindingInProgress = true;
            }

            Task.Run(() =>
            {
                List<Point> newPath = null;
                try
                {
                    var astar = new LaneAStar(map.wayMask);
                    newPath = astar.FindPath(
                        new Point((int)WorldPosition.X, (int)WorldPosition.Y),
                        TargetPoint,
                        Lane);
                }
                catch
                {
                    newPath = null;
                }
                finally
                {
                    if (map.IsHandleCreated && !map.IsDisposed)
                    {
                        map.BeginInvoke(new Action(() =>
                        {
                            Path = newPath ?? new List<Point>();
                            pathIndex = 0;

                            lock (pathfindingLock)
                            {
                                isPathfindingInProgress = false;
                            }
                        }));
                    }
                    else
                    {
                        lock (pathfindingLock)
                        {
                            isPathfindingInProgress = false;
                        }
                    }
                }
            });
        }

        private void DecideOnIntersection()
        {
            float normalizedRotation = (Rotation % 360 + 360) % 360;

            int directionIndex = (int)(Math.Round(normalizedRotation / 90f) % 4);
            float mainDirection = directionIndex * 90f;

            bool isComingFromTopOrRight = (directionIndex == 0 || directionIndex == 1);
            bool isComingFromBottomOrLeft = (directionIndex == 2 || directionIndex == 3);

            if (isComingFromTopOrRight)
            {
                Rotation = (Rotation + 90) % 360;
                Lane = 1;
            }
            else if (isComingFromBottomOrLeft)
            {
                Rotation = (Rotation + 270) % 360;
                Lane = 2;
            }

            decisionMade = true;
        }

        protected virtual void Update_Tick(object sender, EventArgs e)
        {
            if (isDied) return;

            if (inCar || this.Tag == "police_car")
            {
                UpdatePosition();
            }
            else
            {
                if (isSteel) return;
                UpdateAIPosition();
            }

            if (cachedMask != null && (cachedPosition != WorldPosition || cachedRotation != Rotation))
            {
                cachedMask = null;
            }
        }

        public Size GetSize()
        {
            Image carImage = (Image)resources.GetObject($"car_{type}_doors_{action[tekAction == -1 ? 0 : tekAction]}");

            int width = (int)(carImage.Width * 1.3);
            int height = (int)(carImage.Height * 1.3);

            double angleRad = Rotation * Math.PI / 180.0;

            double cos = Math.Abs(Math.Cos(angleRad));
            double sin = Math.Abs(Math.Sin(angleRad));

            int rotatedWidth = (int)(width * cos + height * sin);
            int rotatedHeight = (int)(width * sin + height * cos);

            return new Size(rotatedWidth, rotatedHeight);
        }

        public List<Point> createMask()
        {
            if (cachedMask != null &&
                cachedRotation == Rotation &&
                cachedPosition == WorldPosition)
            {
                return cachedMask;
            }

            List<Point> mask = new List<Point>();
            Bitmap carBitmap = null;
            if (type < 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_doors_closed"));
            else if (type == 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_no"));
            else carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}"));

            float angle = Rotation * (float)Math.PI / 180f;
            float sin = (float)Math.Sin(angle);
            float cos = (float)Math.Cos(angle);

            int W = (int)(carBitmap.Width * 1.3f);
            int H = (int)(carBitmap.Height * 1.3f);
            float scaleX = W / (float)carBitmap.Width;
            float scaleY = H / (float)carBitmap.Height;

            for (int x = 0; x < carBitmap.Width; x += 1)
            {
                for (int y = 0; y < carBitmap.Height; y += 1)
                {
                    Color c = carBitmap.GetPixel(x, y);
                    if (c.A < 50) continue;

                    float lx = (x * scaleX) - (W / 2f);
                    float ly = (y * scaleY) - (H / 2f);

                    float rx = lx * cos - ly * sin;
                    float ry = lx * sin + ly * cos;

                    int worldX = (int)(WorldPosition.X + rx);
                    int worldY = (int)(WorldPosition.Y + ry);

                    mask.Add(new Point(worldX, worldY));
                }
            }

            cachedMask = mask;
            cachedRotation = Rotation;
            cachedPosition = WorldPosition;

            return mask;
        }

        public List<Point> createDoor()
        {
            List<Point> door = new List<Point>();

            Bitmap carBitmap = null;
            if (type < 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_doors_closed"));
            else if (type == 6) carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}_no"));
            else carBitmap = new Bitmap((Image)resources.GetObject($"car_{type}"));

            float ang = Rotation * (float)Math.PI / 180f;
            float sin = (float)Math.Sin(ang);
            float cos = (float)Math.Cos(ang);

            PointF[] local = new PointF[]
            {
                new PointF(-carBitmap.Width/2*1.3f, 0),
                new PointF(-carBitmap.Width/2*1.3f, -carBitmap.Height/2*1.3f),
            };

            foreach (var p in local)
            {
                float rx = p.X * cos - p.Y * sin;
                float ry = p.X * sin + p.Y * cos;

                int wx = (int)(WorldPosition.X + rx);
                int wy = (int)(WorldPosition.Y + ry);

                door.Add(new Point(wx, wy));
            }

            return door;
        }

        private void StartAnimation()
        {
            if (!inCar)
            {
                inCar = true;
                doorsOpen = true;
                animationTimer.Start();
                map.tekCar = this;
                isSteel = true;
            }
            else if (inCar && speed == 0)
            {
                Point? exitPosition = null;

                int exitDistance = 30;
                float localX = -exitDistance;
                float localY = 0;

                float angleRad = Rotation * (float)Math.PI / 180f;
                float cos = (float)Math.Cos(angleRad);
                float sin = (float)Math.Sin(angleRad);

                float worldX = localX * cos - localY * sin;
                float worldY = localX * sin + localY * cos;

                Point newPosition = new Point(
                    WorldPosition.X + (int)worldX,
                    WorldPosition.Y + (int)worldY);

                if (IsPositionFree(newPosition))
                {
                    exitPosition = newPosition;
                }

                if (exitPosition.HasValue)
                {
                    map.player.WorldPosition = exitPosition.Value;

                    inCar = false;
                    animationTimer.Stop();
                    map.tekCar = null;
                    ResetControls();
                }
                else
                {
                    int[] distances = { 35, 40, 45 };

                    foreach (int d in distances)
                    {
                        Point? pos = TryExitFromDoor(d);
                        if (pos.HasValue)
                        {
                            map.player.WorldPosition = pos.Value;

                            inCar = false;
                            animationTimer.Stop();
                            map.tekCar = null;
                            ResetControls();

                            return;
                        }
                    }

                    float ang = Rotation * (float)Math.PI / 180f;

                    Point fallback = new Point(
                        newPosition.X + (int)(Math.Sin(ang) * 40),
                        newPosition.Y - (int)(Math.Cos(ang) * 40)
                    );

                    if (IsPositionFree(fallback))
                    {
                        map.player.WorldPosition = fallback;

                        inCar = false;
                        animationTimer.Stop();
                        map.tekCar = null;
                        ResetControls();

                        return;
                    }
                }
            }
        }

        private Point? TryExitFromDoor(int distance)
        {
            var door = createDoor();

            int minX = door.Min(p => p.X);
            int maxX = door.Max(p => p.X);
            int minY = door.Min(p => p.Y);
            int maxY = door.Max(p => p.Y);

            Point doorCenter = new Point(
                (minX + maxX) / 2,
                (minY + maxY) / 2
            );

            float ang = Rotation * (float)Math.PI / 180f;
            float outX = -(float)Math.Cos(ang);
            float outY = -(float)Math.Sin(ang);

            Point candidate = new Point(
                doorCenter.X + (int)(outX * distance),
                doorCenter.Y + (int)(outY * distance)
            );

            if (IsPositionFree(candidate))
                return candidate;

            return null;
        }

        private bool IsPositionFree(Point position)
        {
            for (int x = 0; x <= map.player.Width; x += 1)
            {
                for (int y = 0; y <= map.player.Height; y += 1)
                {
                    if (map.mask.ContainsKey(new Point(x + position.X, y + position.Y)))
                    {
                        if (map.mask[new Point(x + position.X, y + position.Y)] != Color.FromArgb(0, 0, 0, 0))
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (var car in map.cars)
            {
                var mask = car.createMask();
                for (int x = 0; x <= map.player.Width; x += 1)
                {
                    for (int y = 0; y <= map.player.Height; y += 1)
                    {
                        if (mask.Contains(new Point(x + position.X, y + position.Y)))
                        {
                            return false;
                        }
                    }
                }
            }

            if (position.X < -50 || position.Y < -50 || position.X > map.Width + 50 || position.Y > map.Height + 50)
            {
                return false;
            }

            return true;
        }

        private bool CanRotate(float newRotation)
        {
            if (!inCar) return true;

            float originalRotation = Rotation;
            Rotation = newRotation;

            bool canRotate = true;
            var mask = createMask();

            foreach (var m in mask)
            {
                if (map.mask.ContainsKey(m))
                {
                    if (map.mask[m] != Color.FromArgb(0, 0, 0, 0))
                    {
                        canRotate = false;
                        break;
                    }
                }

                if (m.X < -50 || m.Y < -50 || m.X > map.Width + 50 || m.Y > map.Height + 50)
                {
                    canRotate = false;
                    break;
                }
            }

            Rotation = originalRotation;
            return canRotate;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (doorsOpen)
            {
                if (tekAction < action.Length - 1)
                {
                    tekAction++;
                }
                else { doorsOpen = false; }
            }
            else
            {
                if (tekAction > 0)
                {
                    tekAction--;
                }
                else
                {
                    animationTimer.Stop();
                }
            }
            Invalidate();
        }
    }
}
