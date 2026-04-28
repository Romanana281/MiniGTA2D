using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Project5
{
    public partial class Map : UserControl
    {
        private Form form;
        public Player player;
        public WayMask wayMask;
        private Random rand = new Random();
        private Hud hud;
        private static int frameCounter = 0;
        private bool isGameOver = false;

        // img
        private ComponentResourceManager resources = new ComponentResourceManager(typeof(Map));
        private ComponentResourceManager resource = new ComponentResourceManager(typeof(Player));

        private int screenWidth = 1200;
        private int screenHeight = 720;

        // tile
        public Dictionary<string, Image> tileCache = new Dictionary<string, Image>();
        private List<Point> visibleTiles = new List<Point>();
        private int tilesX = 4;
        private int tilesY = 5;
        private int tileSize = 1024;
        public Point cameraPosition = new Point(500, 400);

        // colision
        public Dictionary<Point, Color> mask = new Dictionary<Point, Color>();
        private HashSet<Point> occupiedPointsHouses = new HashSet<Point>();
        private HashSet<Point> occupiedPointsPlants = new HashSet<Point>();

        // object
        public House tekHouse;
        public Car tekCar;
        public List<House> houses = new List<House>();
        private List<Plant> plants = new List<Plant>();
        public List<Car> cars = new List<Car>();
        public List<Humon> humons = new List<Humon>();
        public List<WeaponInfo> weapons = new List<WeaponInfo>();
        public List<Bullet> bullets = new List<Bullet>();
        public List<PoliceUnit> policeUnits = new List<PoliceUnit>();
        public List<PoliceCar> policeCars = new List<PoliceCar>();

        // humons
        public Timer humonUpdate = new Timer();
        private int maxHumonCount = 6;

        // cars
        public Timer carUpdate = new Timer();
        private int maxCarCount = 2;

        // bullet
        private Timer shootTimer = new Timer();

        // Police
        private Timer policeSpawnTimer = new Timer();
        private int currentWantedLevel = 0;

        // Mission
        public List<MissionPickup> missionPickups = new List<MissionPickup>();
        public Mission activeMission = null;
        public HashSet<Keys> pressedKeys = new HashSet<Keys>();

        public Map(Form form, Player player)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.CreateMask();
            this.form = form;
            this.player = player;
            this.MouseClick += Map_MouseClick;

            SpawnWeaponsOnMap();
            shootTimer.Interval = 17;
            shootTimer.Tick += ShootTimer_Tick;
            shootTimer.Start();

            hud = new Hud(this, player, form);
            hud.Dock = DockStyle.Fill;
            this.Controls.Add(hud);

            policeSpawnTimer.Interval = 3000;
            policeSpawnTimer.Tick += PoliceSpawnTimer_Tick;
            policeSpawnTimer.Start();

            InitializeMissions();
        }

        private void InitializeMissions()
        {
            Mission stealMission = CreateStealCarMission();
            Mission rampageMission = CreateRampageMission();
            Mission timeRaceMission = CreateTimeRaceMission();

            SpawnMissionPickup(new Point(1000, 2000), stealMission);
            SpawnMissionPickup(new Point(3000, 1000), rampageMission);
            SpawnMissionPickup(new Point(2500, 3500), timeRaceMission);
        }

        private void SpawnMissionPickup(Point worldPos, Mission mission)
        {
            MissionPickup pickup = new MissionPickup(this, worldPos, mission);
            missionPickups.Add(pickup);
        }

        public void CreateMask()
        {
            for (int X = 1; X <= tilesX; X++)
            {
                for (int Y = 1; Y <= tilesY; Y++)
                {
                    Image image = (Image)resources.GetObject($"mask_tile_{X}_{Y}");
                    if (image == null) continue;
                    Bitmap img = new Bitmap(image);

                    for (int x = 0; x < tileSize; x += 4)
                    {
                        for (int y = 0; y < tileSize; y += 4)
                        {

                            Color pixelColor = img.GetPixel(x, y);
                            mask[new Point(x, y)] = pixelColor;

                            if (!occupiedPointsHouses.Contains(new Point(x, y)))
                            {
                                House house = new House(pixelColor, x, y, form);

                                if (house.colorHouse.Contains(pixelColor))
                                {
                                    int minX = int.MaxValue;
                                    int minY = int.MaxValue;
                                    int maxX = int.MinValue;
                                    int maxY = int.MinValue;

                                    for (int hx = house.startX; hx <= house.endX + 4; hx++)
                                    {
                                        for (int hy = house.startY; hy <= house.endY + 4; hy++)
                                        {
                                            if (hx < 0 || hx >= tileSize || hy < 0 || hy >= tileSize)
                                                continue;
                                            pixelColor = img.GetPixel(hx, hy);
                                            if (pixelColor == Color.FromArgb(255, 0, 0, 0))
                                            {

                                                if (hx < minX) minX = hx;
                                                if (hy < minY) minY = hy;
                                                if (hx > maxX) maxX = hx;
                                                if (hy > maxY) maxY = hy;
                                            }

                                            occupiedPointsHouses.Add(new Point(hx, hy));
                                        }
                                    }

                                    house.doorOut.AddRange(
                                        new List<Point>()
                                        {
                                            new Point(minX, minY),
                                            new Point(maxX, maxY)
                                        }
                                    );
                                    houses.Add(house);
                                }
                            }

                            if (!occupiedPointsPlants.Contains(new Point(x, y)))
                            {
                                Plant plant = new Plant(pixelColor, x, y);

                                if (plant.colorPlant.Contains(pixelColor))
                                {
                                    for (int hx = plant.startX; hx <= plant.endX + 4; hx++)
                                    {
                                        for (int hy = plant.startY; hy <= plant.endY + 4; hy++)
                                        {
                                            occupiedPointsPlants.Add(new Point(hx, hy));
                                        }
                                    }
                                    plants.Add(plant);
                                }
                            }

                        }
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(Color.Black);

            if (tekHouse != null)
            {
                tekHouse.DrawIn(e.Graphics, form);
                return;
            }

            // Tiles
            foreach (Point tilePos in visibleTiles)
            {
                DrawTile(e.Graphics, tilePos.X, tilePos.Y);
            }

            // Plants
            foreach (var p in plants)
            {
                p.Draw(e.Graphics, cameraPosition);
            }

            // Houses
            foreach (var h in houses)
            {
                if (h.endX - cameraPosition.X > 0 && h.endY - cameraPosition.Y > 0)
                {
                    h.DrawOut(e.Graphics, cameraPosition);
                }
            }

            // Cars
            var allCars = cars.Cast<Car>().Concat(policeCars.Cast<Car>());
            foreach (var c in allCars)
            {
                c.Draw(e.Graphics, cameraPosition);

                // Mission
                if (c.Tag == "mission_steal_target" && c != tekCar)
                {
                    float screenX = c.WorldPosition.X - cameraPosition.X;
                    float screenY = c.WorldPosition.Y - cameraPosition.Y - 80;

                    float pulse = (float)(Math.Sin(DateTime.Now.Millisecond / 200.0) * 10);
                    e.Graphics.DrawLine(Pens.Black, screenX, screenY + pulse / 2, screenX, screenY + pulse / 2 + 30);
                    e.Graphics.FillPolygon(Brushes.Yellow, new Point[]
                    {
                        new Point((int)screenX - 10, (int)(screenY + pulse / 2 + 30)),
                        new Point((int)screenX + 10, (int)(screenY + pulse / 2 + 30)),
                        new Point((int)screenX, (int)(screenY + pulse / 2 + 50))
                    });
                }
            }

            // Car door
            Font font = new Font("Times New Roman", 24, FontStyle.Bold);
            Brush brush = new SolidBrush(Color.White);
            foreach (var c in allCars)
            {
                if (c.isActive && !c.inCar && !c.isDied)
                {
                    string Text = "[E] чтобы сесть в машину";
                    SizeF textSize = e.Graphics.MeasureString(Text, font);
                    e.Graphics.DrawString(Text, font, brush, form.Width - textSize.Width - 10, form.Height - textSize.Height - 50);
                    break;
                }
            }

            // Weapons
            if (weapons.Count > 0)
            {
                foreach (var w in weapons)
                {
                    Image currentWeaponImg = (Image)resource.GetObject(w.type == 0 ? "gun" : "machine");
                    e.Graphics.DrawImage(currentWeaponImg, w.WorldPosition.X - cameraPosition.X, w.WorldPosition.Y - cameraPosition.Y);
                }
            }

            // Bullet
            foreach (var bullet in bullets)
            {
                if (bullet.Active)
                {
                    float screenX = bullet.Position.X - cameraPosition.X;
                    float screenY = bullet.Position.Y - cameraPosition.Y;
                    e.Graphics.FillEllipse(Brushes.Red, screenX - 4, screenY - 4, 8, 8);
                }
            }

            // Mission
            foreach (var pickup in missionPickups)
            {
                pickup.DrawOnMap(e.Graphics, cameraPosition);
            }

            if (activeMission != null && activeMission.Name == "Гонка на время" && activeMission.MarkerPosition.HasValue)
            {
                Point cp = activeMission.MarkerPosition.Value;
                float screenX = cp.X - cameraPosition.X;
                float screenY = cp.Y - cameraPosition.Y;

                float pulse = (float)(Math.Sin(DateTime.Now.Millisecond / 200.0) * 20 + 80);
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(100, 255, 255, 0)), screenX - pulse / 2, screenY - pulse / 2, pulse, pulse);
                e.Graphics.DrawEllipse(new Pen(Color.Yellow, 4), screenX - pulse / 2, screenY - pulse / 2, pulse, pulse);

                e.Graphics.FillEllipse(Brushes.Yellow, screenX - 20, screenY - 20, 40, 40);
                e.Graphics.DrawEllipse(Pens.Black, screenX - 20, screenY - 20, 40, 40);

                if (activeMission.Description.Contains("Чекпоинт"))
                {
                    string[] parts = activeMission.Description.Split(' ');
                    if (parts.Length >= 2)
                    {
                        string numStr = parts[1].Split('/')[0];
                        e.Graphics.DrawString(numStr, new Font("Arial", 24, FontStyle.Bold), Brushes.Black, screenX - 12, screenY - 18);
                    }
                }
            }
        }

        private void DrawTile(Graphics g, int tileX, int tileY)
        {
            int worldX = (tileX - 1) * tileSize;
            int worldY = (tileY - 1) * tileSize;
            int screenX = worldX - cameraPosition.X;
            int screenY = worldY - cameraPosition.Y;

            string tileKey = $"{tileX}_{tileY}";
            Image tileImage;

            if (!tileCache.TryGetValue(tileKey, out tileImage))
            {
                tileImage = (Image)resources.GetObject($"tile_{tileX}_{tileY}");
                if (tileImage != null)
                {

                    tileCache[tileKey] = tileImage;
                }
            }

            if (tileImage != null)
            {
                g.DrawImage(tileImage, screenX, screenY);
            }
        }

        private void UpdateVisibleTiles()
        {
            visibleTiles.Clear();

            int startTileX = cameraPosition.X / tileSize;
            int startTileY = cameraPosition.Y / tileSize;
            int endTileX = (cameraPosition.X + this.Parent.Width) / tileSize;
            int endTileY = (cameraPosition.Y + this.Parent.Height) / tileSize;

            startTileX = Math.Max(1, startTileX);
            startTileY = Math.Max(1, startTileY);
            endTileX = Math.Min(tilesX, endTileX + 1);
            endTileY = Math.Min(tilesY, endTileY + 1);

            for (int y = startTileY; y <= endTileY; y++)
            {
                for (int x = startTileX; x <= endTileX; x++)
                {
                    visibleTiles.Add(new Point(x, y));
                }
            }
        }

        private void PreloadVisibleTiles()
        {
            foreach (Point tilePos in visibleTiles)
            {
                string tileKey = $"{tilePos.X}_{tilePos.Y}";

                if (!tileCache.ContainsKey(tileKey))
                {
                    Image tileImage = (Image)resources.GetObject($"tile_{tilePos.X}_{tilePos.Y}");
                    if (tileImage != null)
                    {
                        tileCache[tileKey] = tileImage;
                    }
                }
            }

            CleanupTileCache();
        }

        private void CleanupTileCache()
        {
            if (tileCache.Count > visibleTiles.Count * 2)
            {
                List<string> toRemove = new List<string>();

                foreach (var kvp in tileCache)
                {
                    bool isVisible = false;
                    foreach (Point visibleTile in visibleTiles)
                    {
                        if (kvp.Key == $"{visibleTile.X}_{visibleTile.Y}")
                        {
                            isVisible = true;
                            break;
                        }
                    }

                    if (!isVisible)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (string key in toRemove)
                {
                    tileCache[key].Dispose();
                    tileCache.Remove(key);
                }
            }
        }

        public void StartMission(Mission mission)
        {
            if (activeMission != null) return;
            activeMission = mission;
            mission.StartAction?.Invoke(this);
            Invalidate();
        }

        public void UpdateCamera(Point playerPosition)
        {
            if (isGameOver) return;

            screenWidth = this.Parent.Width;
            screenHeight = this.Parent.Height;
            if (tekHouse == null)
            {
                int mapWidth = tilesX * tileSize;
                int mapHeight = tilesY * tileSize;
                int screenWidth = this.Parent.Width;
                int screenHeight = this.Parent.Height;

                int targetX = playerPosition.X - (screenWidth / 2);
                int targetY = playerPosition.Y - (screenHeight / 2);

                int maxCameraX = Math.Max(0, mapWidth - screenWidth);
                int maxCameraY = Math.Max(0, mapHeight - screenHeight);

                if (mapWidth < screenWidth)
                {
                    cameraPosition.X = (screenWidth - mapWidth) / 2;
                }
                else
                {
                    cameraPosition.X = Math.Max(0, Math.Min(targetX, maxCameraX));
                }

                if (mapHeight < screenHeight)
                {
                    cameraPosition.Y = (screenHeight - mapHeight) / 2;
                }
                else
                {
                    cameraPosition.Y = Math.Max(0, Math.Min(targetY, maxCameraY));
                }

                UpdatePlayerPosition(playerPosition);

                foreach (var humon in humons.ToList())
                {
                    if (this.Controls.Contains(humon))
                    {
                        humon.Location = new Point(
                            humon.WorldPosition.X - cameraPosition.X,
                            humon.WorldPosition.Y - cameraPosition.Y
                        );
                    }
                }

                foreach (var police in policeUnits.ToList())
                {
                    if (police == null || police.IsDisposed)
                    {
                        policeUnits.Remove(police);
                        continue;
                    }

                    police.UpdatePosition(this, form);

                    if (Controls.Contains(police))
                    {
                        police.Location = new Point(
                            police.WorldPosition.X - cameraPosition.X,
                            police.WorldPosition.Y - cameraPosition.Y
                        );
                    }
                }

                UpdateVisibleTiles();
                PreloadVisibleTiles();
                VisibleHumon();
                VisibleCar();

                for (int i = bullets.Count - 1; i >= 0; i--)
                {
                    bullets[i].Update();

                    bool hitSomething = false;
                    foreach (var humon in humons.ToList())
                    {
                        if (humon == null || humon.IsDisposed) continue;

                        float humonCenterX = humon.WorldPosition.X + humon.Width / 2f;
                        float humonCenterY = humon.WorldPosition.Y + humon.Height / 2f;
                        float dxHumon = bullets[i].Position.X - humonCenterX;
                        float dyHumon = bullets[i].Position.Y - humonCenterY;
                        float distToHumon = (float)Math.Sqrt(dxHumon * dxHumon + dyHumon * dyHumon);
                        float humonHitRadius = 20f;

                        if (distToHumon < humonHitRadius)
                        {
                            humon.health -= bullets[i].Damage;
                            hitSomething = true;

                            if (humon.health <= 0 && !humon.isDied)
                            {
                                humon.isDied = true;
                                humon.deathTime = DateTime.Now;
                                humon.deathProcessed = true;
                                humon.KeysUp = humon.KeysDown = humon.KeysLeft = humon.KeysRight = false;

                                if (bullets[i].owner == "user")
                                {
                                    player.money += 100;
                                    player.wantedPoints += 30;
                                    player.UpdateWantedLevel();
                                }

                                if (bullets[i].owner == "user" && activeMission != null && activeMission.Name == "Рампейдж")
                                {
                                    player.rampageKillCount++;
                                }
                            }
                            break;
                        }
                    }

                    if (!hitSomething)
                    {
                        foreach (var police in policeUnits.ToList())
                        {
                            if (police == null || police.IsDisposed || police.isDied) continue;

                            float policeCenterX = police.WorldPosition.X + police.Width / 2f;
                            float policeCenterY = police.WorldPosition.Y + police.Height / 2f;

                            float dxPolice = bullets[i].Position.X - policeCenterX;
                            float dyPolice = bullets[i].Position.Y - policeCenterY;
                            float distToPolice = (float)Math.Sqrt(dxPolice * dxPolice + dyPolice * dyPolice);

                            float policeHitRadius = 20f;

                            if (distToPolice < policeHitRadius)
                            {
                                police.health -= bullets[i].Damage;

                                hitSomething = true;

                                if (police.health <= 0 && !police.isDied)
                                {
                                    police.isDied = true;
                                    police.deathTime = DateTime.Now;
                                    police.deathProcessed = true;
                                    police.KeysUp = police.KeysDown = police.KeysLeft = police.KeysRight = false;

                                    if (bullets[i].owner == "user")
                                    {
                                        player.money += 200;
                                        player.wantedPoints += 80;
                                        player.UpdateWantedLevel();
                                    }

                                    if (bullets[i].owner == "user" && activeMission != null && activeMission.Name == "Рампейдж")
                                    {
                                        player.rampageKillCount++;
                                    }
                                }
                                break;
                            }
                        }
                    }

                    if (!hitSomething)
                    {
                        var allCars = cars.Cast<Car>().Concat(policeCars.Cast<Car>());
                        foreach (var car in allCars)
                        {
                            var carMask = car.createMask();

                            bool bulletHitCar = false;
                            Point bulletPos = new Point((int)bullets[i].Position.X, (int)bullets[i].Position.Y);

                            for (int j = 0; j < carMask.Count; j += 1)
                            {
                                Point carPoint = carMask[j];
                                if (Math.Abs(carPoint.X - bulletPos.X) < 8 &&
                                    Math.Abs(carPoint.Y - bulletPos.Y) < 8)
                                {
                                    bulletHitCar = true;
                                    break;
                                }
                            }

                            if (bulletHitCar)
                            {
                                car.health -= bullets[i].Damage;

                                if (car.health <= 0 && !car.isDied)
                                {
                                    car.isDied = true;
                                    car.deathTime = DateTime.Now;
                                    car.deathProcessed = true;
                                    car.speed = 0;

                                    if (bullets[i].owner == "user")
                                    {
                                        player.money += 500;
                                        player.wantedPoints += 65;
                                        player.UpdateWantedLevel();
                                    }
                                }

                                hitSomething = true;
                                break;
                            }
                        }
                    }

                    if (!hitSomething)
                    {
                        float playerCenterX = player.WorldPosition.X + player.Width / 2f;
                        float playerCenterY = player.WorldPosition.Y + player.Height / 2f;

                        float dxPlayer = bullets[i].Position.X - playerCenterX;
                        float dyPlayer = bullets[i].Position.Y - playerCenterY;
                        float distToPlayer = (float)Math.Sqrt(dxPlayer * dxPlayer + dyPlayer * dyPlayer);

                        float playerHitRadius = 15f;

                        if (distToPlayer < playerHitRadius)
                        {
                            if (tekCar == null)
                            {
                                player.health -= bullets[i].Damage;
                            }

                            hitSomething = true;

                            if (player.health <= 0)
                            {
                                EndGame();
                                return;
                            }

                            break;
                        }
                    }

                    if (!hitSomething)
                    {
                        Point bulletWorldPos = new Point((int)bullets[i].Position.X, (int)bullets[i].Position.Y);

                        for (int dx = -8; dx <= 8; dx += 1)
                        {
                            for (int dy = -8; dy <= 8; dy += 1)
                            {
                                Point checkPoint = new Point(bulletWorldPos.X + dx, bulletWorldPos.Y + dy);

                                if (mask.TryGetValue(checkPoint, out Color pixelColor))
                                {
                                    if (pixelColor.A > 0)
                                    {
                                        hitSomething = true;
                                        goto BulletHitWall;
                                    }
                                }
                            }
                        }
                    }

                BulletHitWall:
                    if (hitSomething || !bullets[i].Active)
                    {
                        bullets[i].Active = false;
                        bullets.RemoveAt(i);
                    }
                }

                for (int i = humons.Count - 1; i >= 0; i--)
                {
                    var humon = humons[i];
                    if (humon.isDied && (DateTime.Now - humon.deathTime).TotalSeconds >= 3.0)
                    {
                        humons.RemoveAt(i);
                        if (this.Controls.Contains(humon))
                        {
                            this.Controls.Remove(humon);
                        }
                        humon.Dispose();
                    }
                }

                for (int i = policeUnits.Count - 1; i >= 0; i--)
                {
                    var police = policeUnits[i];
                    if (police == null || police.IsDisposed)
                    {
                        policeUnits.RemoveAt(i);
                        continue;
                    }

                    if (police.isDied && (DateTime.Now - police.deathTime).TotalSeconds >= 3.0)
                    {
                        policeUnits.RemoveAt(i);
                        if (this.Controls.Contains(police))
                        {
                            this.Controls.Remove(police);
                        }
                        police.Dispose();
                    }
                }

                for (int i = cars.Count - 1; i >= 0; i--)
                {
                    var car = cars[i];
                    if (car.isDied && (DateTime.Now - car.deathTime).TotalSeconds >= 3.0 && tekCar == car)
                    {
                        EndGame();
                        return;
                    }
                    else if (car.isDied && (DateTime.Now - car.deathTime).TotalSeconds >= 3.0)
                    {
                        cars.RemoveAt(i);
                        if (this.Controls.Contains(car))
                        {
                            this.Controls.Remove(car);
                        }
                        car.Dispose();
                    }
                }

                for (int i = policeCars.Count - 1; i >= 0; i--)
                {
                    var car = policeCars[i];
                    if (car.isDied && (DateTime.Now - car.deathTime).TotalSeconds >= 3.0)
                    {
                        policeCars.RemoveAt(i);
                        if (this.Controls.Contains(car))
                        {
                            this.Controls.Remove(car);
                        }
                        car.Dispose();
                    }
                }

                if (frameCounter >= 25)
                {
                    frameCounter = 0;
                    if (player.searchLevel > 0)
                    {
                        if (player.searchTimer > 0)
                        {
                            player.searchTimer--;
                        }

                        else
                        {
                            player.searchTimer = 0;
                            player.wantedPoints = 0;
                            player.UpdateWantedLevel();
                        }
                    }
                }

                frameCounter++;

                if (activeMission != null)
                {
                    if (activeMission.TimeLimitSeconds > 0 &&
                        (DateTime.Now - activeMission.StartTime).TotalSeconds > activeMission.TimeLimitSeconds && activeMission.IsActive)
                    {
                        activeMission.OnFail?.Invoke(this);
                        activeMission = null;
                    }
                    else if (activeMission.CheckFail?.Invoke(this) == true)
                    {
                        activeMission.OnFail?.Invoke(this);
                        activeMission = null;
                    }
                    else if (activeMission.CheckComplete?.Invoke(this) == true)
                    {
                        activeMission.OnComplete?.Invoke(this);
                        activeMission.IsCompleted = true;
                        activeMission = null;
                    }
                }

                foreach (var pickup in missionPickups.ToList())
                {
                    pickup.CheckPlayerProximity();
                }

                this.Invalidate();
            }
            else
            {
                player.Location = playerPosition;
                this.Invalidate();
            }
        }

        private void UpdatePlayerPosition(Point playerPosition)
        {
            if (player != null)
            {
                int screenX = playerPosition.X - cameraPosition.X;
                int screenY = playerPosition.Y - cameraPosition.Y;

                player.Location = new Point(screenX, screenY);
                player.BringToFront();
            }
        }

        public void HumonUpdate_Tick(object sender, EventArgs e)
        {
            if (!this.IsHandleCreated || this.IsDisposed)
                return;

            Task.Run(() =>
            {
                UpdateHumon();
                CreateHumon();

                this.Invoke(new Action(() =>
                {
                    VisibleHumon();
                }));
            });
        }

        public void CreateHumon()
        {
            if (humons.Count + policeUnits.Count > maxHumonCount) return;

            int desiredCarCount = player.searchLevel <= 2 ? 1 : player.searchLevel <= 4 ? 2 : 3;

            int needToSpawn = maxHumonCount - humons.Count - desiredCarCount * 2;

            float screenDiag = (float)Math.Sqrt(screenWidth * screenWidth / 4 + screenHeight * screenHeight / 4);

            float minRadius = screenDiag;
            float maxRadius = screenDiag * 1.1f;

            PointF cameraCenter = new PointF(
                cameraPosition.X + screenWidth / 2f,
                cameraPosition.Y + screenHeight / 2f
            );

            for (int i = 0; i < needToSpawn; i++)
            {
                int attempts = 0;
                bool spawned = false;

                while (attempts < 70 && !spawned)
                {
                    attempts++;

                    double angle = rand.NextDouble() * Math.PI * 2;

                    float distance = minRadius + (float)(rand.NextDouble() * (maxRadius - minRadius));

                    int spawnX = (int)(cameraCenter.X + Math.Cos(angle) * distance);
                    int spawnY = (int)(cameraCenter.Y + Math.Sin(angle) * distance);

                    if (spawnX < -50 || spawnX > 4096 + 50 || spawnY < -50 || spawnY > 5120 + 50)
                        continue;

                    Point spawnPos = new Point(spawnX, spawnY);

                    if (IsPositionBlocked(spawnPos))
                        continue;

                    Humon humon = new Humon(
                        speed: rand.Next(1, 3),
                        health: rand.Next(3, 6),
                        type: rand.Next(1, 4),
                        WorldPosition: spawnPos
                    );

                    DirectToCenter(humon, cameraCenter);

                    humon.Tag = "approaching";

                    humons.Add(humon);
                    spawned = true;
                }
            }
        }

        private void DirectToCenter(Humon humon, PointF center)
        {
            humon.KeysUp = humon.KeysDown = humon.KeysLeft = humon.KeysRight = false;

            float dx = center.X - humon.WorldPosition.X;
            float dy = center.Y - humon.WorldPosition.Y;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                if (dx > 0) humon.KeysRight = true;
                else humon.KeysLeft = true;
            }
            else
            {
                if (dy > 0) humon.KeysDown = true;
                else humon.KeysUp = true;
            }

            if (rand.Next(0, 100) < 30)
            {
                if (rand.Next(0, 2) == 0) humon.KeysUp = true;
                else humon.KeysDown = true;
            }
        }

        private void SetRandomDirection(Humon humon)
        {
            humon.KeysUp = humon.KeysDown = humon.KeysLeft = humon.KeysRight = false;

            if (rand.Next(0, 100) < 25)
                return;

            int dir = rand.Next(0, 8);
            switch (dir)
            {
                case 0: humon.KeysRight = true; break;
                case 1: humon.KeysLeft = true; break;
                case 2: humon.KeysUp = true; break;
                case 3: humon.KeysDown = true; break;
                case 4: humon.KeysUp = humon.KeysRight = true; break;
                case 5: humon.KeysUp = humon.KeysLeft = true; break;
                case 6: humon.KeysDown = humon.KeysRight = true; break;
                case 7: humon.KeysDown = humon.KeysLeft = true; break;
            }
        }

        private void UpdateHumon()
        {
            float screenDiag = (float)Math.Sqrt(screenWidth * screenWidth / 4 + screenHeight * screenHeight / 4);

            PointF cameraCenter = new PointF(
                cameraPosition.X + screenWidth / 2f,
                cameraPosition.Y + screenHeight / 2f
            );

            foreach (var humon in humons.ToList())
            {
                if (humon == null || humon.IsDisposed)
                {
                    humons.Remove(humon);
                    continue;
                }

                float dx = cameraCenter.X - humon.WorldPosition.X;
                float dy = cameraCenter.Y - humon.WorldPosition.Y;
                float distToCenter = (float)Math.Sqrt(dx * dx + dy * dy);

                if (humon.Tag?.ToString() == "approaching")
                {
                    if (distToCenter <= screenDiag)
                    {
                        humon.Tag = "wandering";
                        SetRandomDirection(humon);
                    }
                    else
                    {
                        DirectToCenter(humon, cameraCenter);
                    }
                }
                else
                {
                    if (rand.Next(0, 100) < 8)
                    {
                        SetRandomDirection(humon);

                        if (distToCenter > screenDiag)
                        {
                            DirectToCenter(humon, cameraCenter);
                        }
                    }
                }

                humon.UpdatePosition(this, form);

                if (distToCenter > screenDiag * 1.1f)
                {
                    humons.Remove(humon);
                }
            }
        }

        private void VisibleHumon()
        {
            if (tekHouse != null)
            {
                foreach (var h in humons)
                    if (this.Controls.Contains(h))
                        this.Controls.Remove(h);
                return;
            }

            int bufferX = screenWidth / 2;
            int bufferY = screenHeight / 2;

            int left = cameraPosition.X - bufferX;
            int top = cameraPosition.Y - bufferY;
            int right = cameraPosition.X + screenWidth + bufferX;
            int bottom = cameraPosition.Y + screenHeight + bufferY;

            foreach (var humon in humons.ToList())
            {
                if (humon == null || humon.IsDisposed) continue;

                bool shouldBeVisible =
                    humon.WorldPosition.X + humon.Width / 2 >= left &&
                    humon.WorldPosition.X - humon.Width / 2 <= right &&
                    humon.WorldPosition.Y + humon.Height / 2 >= top &&
                    humon.WorldPosition.Y - humon.Height / 2 <= bottom;

                if (shouldBeVisible)
                {
                    if (!this.Controls.Contains(humon))
                    {
                        this.Controls.Add(humon);
                        humon.BringToFront();
                    }
                }
                else
                {
                    if (this.Controls.Contains(humon))
                        this.Controls.Remove(humon);
                }
            }
        }

        private void VisibleCar()
        {
            if (tekHouse != null)
            {
                foreach (var c in cars)
                    if (this.Controls.Contains(c))
                        this.Controls.Remove(c);
                return;
            }

            int bufferX = screenWidth / 2;
            int bufferY = screenHeight / 2;

            int left = cameraPosition.X - bufferX;
            int top = cameraPosition.Y - bufferY;
            int right = cameraPosition.X + screenWidth + bufferX;
            int bottom = cameraPosition.Y + screenHeight + bufferY;

            var allCars = cars.Cast<Car>().Concat(policeCars.Cast<Car>());

            foreach (var car in allCars)
            {
                if (car == null || car.IsDisposed) continue;

                bool shouldBeVisible =
                    car.WorldPosition.X + car.Width / 2 >= left &&
                    car.WorldPosition.X - car.Width / 2 <= right &&
                    car.WorldPosition.Y + car.Height / 2 >= top &&
                    car.WorldPosition.Y - car.Height / 2 <= bottom;

                if (shouldBeVisible)
                {
                    if (!this.Controls.Contains(car))
                    {
                        this.Controls.Add(car);
                        car.BringToFront();
                    }
                }
                else
                {
                    if (this.Controls.Contains(car))
                        this.Controls.Remove(car);
                }
            }
        }

        public bool IsPositionBlocked(Point worldPos)
        {
            foreach (var kvp in mask)
            {
                int dx = Math.Abs(kvp.Key.X - worldPos.X);
                int dy = Math.Abs(kvp.Key.Y - worldPos.Y);
                if (dx <= 50 && dy <= 50)
                {
                    if (kvp.Value.A != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void CarUpdate_Tick(object sender, EventArgs e)
        {
            CreateCar();
        }

        public void CreateCar()
        {
            if (cars.Count + policeCars.Count > maxCarCount) return;
            int needToSpawn = maxCarCount - cars.Count - policeCars.Count;

            List<SpawnCar> validSpawns = wayMask.spawn.Where(s => s.type == "exit").ToList();
            List<SpawnCar> validExit = wayMask.spawn.Where(s => s.type != "exit").ToList();

            for (int i = 0; i < needToSpawn; i++)
            {
                SpawnCar spawnCar = validSpawns[rand.Next(0, validSpawns.Count)];
                SpawnCar exitCar = validExit[rand.Next(0, validExit.Count)];

                Point start = spawnCar.start;
                Point end = spawnCar.end;
                int dx = (end.X + start.X) / 2;
                int dy = (end.Y + start.Y) / 2;

                Point spawnPosition = new Point(0, 0);

                int type = rand.Next(1, 6);

                Car tempCar = new Car(this, new Point(0, 0), type);

                switch (spawnCar.angle)
                {
                    case 0:
                        spawnPosition = new Point(dx, dy - tempCar.GetSize().Height - 15);
                        break;
                    case 90:
                        spawnPosition = new Point(dx + tempCar.GetSize().Width + 15, dy);
                        break;
                    case 180:
                        spawnPosition = new Point(dx, dy + tempCar.GetSize().Height + 15);
                        break;
                    case 270:
                        spawnPosition = new Point(dx - tempCar.GetSize().Width - 15, dy);
                        break;
                }

                Car car = new Car(this, spawnPosition, type);
                car.TargetPoint = new Point((exitCar.end.X + exitCar.start.X) / 2, (exitCar.end.Y + exitCar.start.Y) / 2);  // Цель
                car.Rotation = spawnCar.angle;
                car.Lane = spawnCar.angle == 0 || spawnCar.angle == 90 ? 1 : 2;

                car.Path = new LaneAStar(wayMask)
                    .FindPath(car.WorldPosition, car.TargetPoint, car.Lane);

                cars.Add(car);
            }
        }

        private void SpawnWeaponsOnMap()
        {
            WeaponInfo gun = new WeaponInfo
            {
                type = 0,
                rechargeCount = 6,
                currentAmmo = 6,
                rechargeTime = 1.5f,
                split = 1,
                WorldPosition = new Point(628, 4375)
            };
            weapons.Add(gun);

            WeaponInfo machine = new WeaponInfo
            {
                type = 1,
                rechargeCount = 30,
                currentAmmo = 30,
                rechargeTime = 4,
                split = 0.1f,
                WorldPosition = new Point(2725, 2863)
            };
            weapons.Add(machine);
        }

        private void Map_MouseClick(object sender, MouseEventArgs e)
        {
            if (tekHouse != null || tekCar != null || player.tekWeapon == null) return;

            if (player.tekWeapon.tekRechargeTime > 0) return;

            ShootBullet(e);

            if (player.tekWeapon.split > 0)
            {
                shootTimer.Interval = (int)(player.tekWeapon.split * 1000);
                shootTimer.Enabled = false;
                shootTimer.Enabled = true;
            }
        }

        private void ShootTimer_Tick(object sender, EventArgs e)
        {
            if (tekHouse != null || tekCar != null || player.tekWeapon == null) return;

            if (player.tekWeapon.tekRechargeTime > 0)
            {
                player.tekWeapon.tekRechargeTime -= player.tekWeapon.split;
                if (player.tekWeapon.tekRechargeTime <= 0)
                {
                    player.tekWeapon.currentAmmo = player.tekWeapon.rechargeCount;
                    player.tekWeapon.tekRechargeTime = 0;
                }
                return;
            }

            if (Control.MouseButtons == MouseButtons.Left && player.tekWeapon.split < 1)
            {
                Point mousePos = this.PointToClient(Cursor.Position);
                MouseEventArgs fakeE = new MouseEventArgs(MouseButtons.Left, 1, mousePos.X, mousePos.Y, 0);
                ShootBullet(fakeE);
            }
        }

        private void ShootBullet(MouseEventArgs e)
        {
            if (player.tekWeapon.tekRechargeTime > 0 || player.tekWeapon.currentAmmo <= 0) return;

            if (player.searchLevel == 0)
            {
                player.wantedPoints += 20;
            }
            else
            {
                player.wantedPoints += 1;
            }
            player.UpdateWantedLevel();

            Point mouseWorldPos = new Point(e.X + cameraPosition.X, e.Y + cameraPosition.Y);
            float dx = mouseWorldPos.X - player.WorldPosition.X;
            float dy = mouseWorldPos.Y - player.WorldPosition.Y;
            float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
            float rad = angle * (float)Math.PI / 180f;

            PointF bulletStart = new PointF(
                player.WorldPosition.X + 25 * (float)Math.Cos(rad),
                player.WorldPosition.Y + 25 * (float)Math.Sin(rad)
            );

            bullets.Add(new Bullet(bulletStart, angle, 1, "user"));
            player.tekWeapon.currentAmmo--;

            if (player.tekWeapon.currentAmmo <= 0)
            {
                player.tekWeapon.tekRechargeTime = player.tekWeapon.rechargeTime;
            }
        }

        private void PoliceSpawnTimer_Tick(object sender, EventArgs e)
        {
            if (player == null) return;

            int wanted = player.searchLevel;
            if (wanted == 0)
            {
                maxHumonCount = 6;
                maxCarCount = 3;

                foreach (var pc in policeCars.ToList())
                {
                    pc.isDied = true;
                    if (Controls.Contains(pc)) Controls.Remove(pc);
                    pc.Dispose();
                }
                policeCars.Clear();

                foreach (var p in policeUnits.ToList())
                {
                    policeUnits.Remove(p);
                    if (Controls.Contains(p)) Controls.Remove(p);
                    p.Dispose();
                }
                return;
            }

            maxCarCount = 4;
            maxHumonCount = 8;

            int desiredCarCount;
            int policeType;

            if (wanted <= 2)
            {
                desiredCarCount = 1;
                policeType = 6;
            }
            else if (wanted <= 4)
            {
                desiredCarCount = 2;
                policeType = rand.Next(0, 100) < 70 ? 6 : 7;
            }
            else
            {
                desiredCarCount = 3;
                policeType = 7;
            }

            int desiredPoliceUnitsCount = desiredCarCount * 2;
            int bufferX = screenWidth / 2;
            int bufferY = screenHeight / 2;

            int left = cameraPosition.X - bufferX;
            int top = cameraPosition.Y - bufferY;
            int right = cameraPosition.X + screenWidth + bufferX;
            int bottom = cameraPosition.Y + screenHeight + bufferY;

            var allCars = cars.Cast<Car>().Concat(policeCars.Cast<Car>());

            int neededHumonSlots = desiredPoliceUnitsCount - policeUnits.Count;
            if (neededHumonSlots > 0)
            {
                var humonsToRemove = humons.Take(neededHumonSlots).ToList();

                foreach (var humon in humonsToRemove)
                {
                    bool shouldBeVisible =
                        humon.WorldPosition.X + humon.Width / 2 >= left &&
                        humon.WorldPosition.X - humon.Width / 2 <= right &&
                        humon.WorldPosition.Y + humon.Height / 2 >= top &&
                        humon.WorldPosition.Y - humon.Height / 2 <= bottom;

                    if (!shouldBeVisible && this.Controls.Contains(humon))
                    {
                        Controls.Remove(humon);

                        humon.Dispose();
                        humons.Remove(humon);
                    }
                }
            }

            int neededCarSlots = desiredCarCount - policeCars.Count;
            if (neededCarSlots > 0)
            {
                var carsToRemove = cars.Take(neededCarSlots).ToList();

                foreach (var car in carsToRemove)
                {
                    if (car == tekCar || car.type >= 6) continue;

                    bool shouldBeVisible =
                       car.WorldPosition.X + car.Width / 2 >= left &&
                       car.WorldPosition.X - car.Width / 2 <= right &&
                       car.WorldPosition.Y + car.Height / 2 >= top &&
                       car.WorldPosition.Y - car.Height / 2 <= bottom;

                    if (!shouldBeVisible && this.Controls.Contains(car))
                    {
                        Controls.Remove(car);

                        car.Dispose();
                        cars.Remove(car);
                    }
                }
            }

            while (policeCars.Count > desiredCarCount)
            {
                var farthest = policeCars.OrderByDescending(c =>
                    Distance(c.WorldPosition, player.WorldPosition)).First();
                farthest.isDied = true;
                if (Controls.Contains(farthest)) Controls.Remove(farthest);
                farthest.Dispose();
                policeCars.Remove(farthest);
            }

            while (policeCars.Count < desiredCarCount)
            {
                Point spawnPos = FindPoliceSpawnPosition();
                var policeCar = new PoliceCar(this, spawnPos, policeType);
                policeCars.Add(policeCar);
            }
        }

        private Point FindPoliceSpawnPosition()
        {
            Random rand = new Random();
            Point spawnPos;
            int attempts = 0;
            do
            {
                double angle = rand.NextDouble() * Math.PI * 2;
                float dist = 400 + (float)rand.NextDouble() * 300;
                Point Pos = tekCar == null ? new Point(player.WorldPosition.X, player.WorldPosition.Y) : new Point(tekCar.WorldPosition.X, tekCar.WorldPosition.Y);
                spawnPos = new Point(
                    (int)(Pos.X + Math.Cos(angle) * dist),
                    (int)(Pos.Y + Math.Sin(angle) * dist)
                );
                attempts++;
            } while ((IsPositionBlocked(spawnPos) || spawnPos.X < -50 || spawnPos.Y < -50 || spawnPos.X > 4096 + 50 || spawnPos.Y > 5120 + 50) && attempts < 50);

            return spawnPos;
        }

        private float Distance(Point a, Point b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public void EndGame()
        {
            this.Invoke(new Action(() =>
            {
                isGameOver = true;
                var gameOver = new DiedWindow(player);
                gameOver.Size = new Size(form.Width, form.Height);
                gameOver.OnRestart += () =>
                {
                    player.health = 5;
                    player.money = player.money / 2;
                    player.wantedPoints = 0;
                    player.searchLevel = 0;
                    player.searchTimer = 0;
                    player.WorldPosition = new Point(500, 400);
                    player.ResetControls();

                    var allCars = cars.Cast<Car>().Concat(policeCars.Cast<Car>());
                    foreach (var car in allCars)
                    {
                        if (this.Controls.Contains(car))
                            this.Controls.Remove(car);
                        car.Dispose();
                    }
                    foreach (var humon in humons)
                    {
                        if (this.Controls.Contains(humon))
                            this.Controls.Remove(humon);
                        humon.Dispose();
                    }
                    foreach (var police in policeUnits)
                    {
                        if (this.Controls.Contains(police))
                            this.Controls.Remove(police);
                        police.Dispose();
                    }

                    cars.Clear();
                    humons.Clear();
                    bullets.Clear();
                    policeUnits.Clear();
                    policeCars.Clear();
                    activeMission = null;

                    cameraPosition = new Point(500, 400);
                    isGameOver = false;

                    this.Invalidate();
                };

                gameOver.OnExit += () =>
                {
                    Application.Exit();
                };

                gameOver.ShowDialog();
            }));

        }

        private Mission CreateStealCarMission()
        {
            Mission mission = new Mission();

            mission.Name = "Угон машины";
            mission.Description = "Угони спортивную машину на парковке";
            mission.TimeLimitSeconds = 150;

            bool wantedAdded = false;

            mission.StartAction = (map) =>
            {
                Point spawnPos = new Point(650, 4662);
                Car targetCar = new Car(map, spawnPos, 4);
                targetCar.Tag = "mission_steal_target";
                map.cars.Add(targetCar);
                mission.MarkerPosition = spawnPos;
                mission.IsActive = false;
            };

            mission.CheckComplete = (map) =>
            {
                Car targetCar = map.cars.FirstOrDefault(c => (string)c.Tag == "mission_steal_target");
                if (targetCar == null || targetCar.isDied) return false;

                if (map.tekCar == targetCar && !mission.IsActive)
                {
                    mission.IsActive = true;
                    mission.StartTime = DateTime.Now;
                    mission.Description = "Уезжай от копов! Не выходи из машины!";

                    if (!wantedAdded)
                    {
                        map.player.wantedPoints += 150;
                        map.player.UpdateWantedLevel();
                        wantedAdded = true;
                    }
                }

                return mission.IsActive && map.tekCar == targetCar && player.searchLevel == 0;
            };

            mission.CheckFail = (map) =>
            {
                if (mission.IsActive)
                {
                    Car targetCar = map.cars.FirstOrDefault(c => (string)c.Tag == "mission_steal_target");
                    if (targetCar == null || targetCar.isDied || map.player.health <= 0)
                        return true;

                    if (map.tekCar != targetCar)
                        return true;
                }

                return false;
            };

            mission.OnComplete = (map) =>
            {
                map.player.money += 500;
                Car targetCar = map.cars.FirstOrDefault(c => (string)c.Tag == "mission_steal_target");
                if (targetCar != null)
                {
                    targetCar.Tag = "owned";
                }
                mission.MarkerPosition = null;
                map.hud.ShowMissionResult("Миссия выполнена!", true);
            };

            mission.OnFail = (map) =>
            {
                Car targetCar = map.cars.FirstOrDefault(c => (string)c.Tag == "mission_steal_target");
                if (targetCar != null)
                {
                    tekCar = null;
                    player.wantedPoints = 0;
                    player.UpdateWantedLevel();
                    map.cars.Remove(targetCar);
                    if (map.Controls.Contains(targetCar)) map.Controls.Remove(targetCar);
                    targetCar.Dispose();
                }
                mission.MarkerPosition = null;
                map.hud.ShowMissionResult("Миссия провалена!", false);
            };

            return mission;
        }

        private Mission CreateRampageMission()
        {
            Mission mission = new Mission();
            mission.Name = "Рампейдж";
            mission.Description = "Застрели 40 человек за 3 минуты";
            mission.TimeLimitSeconds = 180;

            mission.StartAction = (map) =>
            {
                map.player.rampageKillCount = 0;
                mission.StartTime = DateTime.Now;
                map.player.UpdateWantedLevel();
                mission.Description = $"Убито: 0/40";
            };

            mission.CheckComplete = (map) =>
            {
                mission.Description = $"Убито: {map.player.rampageKillCount}/40";
                return map.player.rampageKillCount >= 40;
            };

            mission.CheckFail = (map) =>
            {
                return map.player.health <= 0;
            };

            mission.OnComplete = (map) =>
            {
                map.player.money += 3000;
                map.player.wantedPoints = 0;
                map.player.UpdateWantedLevel();
                map.hud.ShowMissionResult("Рампейдж завершён!", true);
            };

            mission.OnFail = (map) =>
            {
                map.hud.ShowMissionResult("Рампейдж провален!", false);
            };

            return mission;
        }

        private Mission CreateTimeRaceMission()
        {
            Mission mission = new Mission();
            mission.Name = "Гонка на время";
            mission.Description = "Проедь через все чекпоинты за 2 минуты";
            mission.TimeLimitSeconds = 45;

            List<Point> checkpoints = new List<Point>
            {
                new Point(3364, 3489),
                new Point(3912, 3075),
                new Point(3997, 2178),
                new Point(3752, 1625),
                new Point(3325, 682),
                new Point(2096, 189),
                new Point(1237, 624),
                new Point(1711, 893),
                new Point(2076, 2448),
                new Point(3366, 3374),
                new Point(3900, 2447),
                new Point(3886, 4665)
            };
            int currentCheckpoint = 0;

            mission.StartAction = (map) =>
            {
                currentCheckpoint = 0;
                mission.MarkerPosition = checkpoints[0];
                mission.StartTime = DateTime.Now;
                mission.Description = "Чекпоинт 0/12";
                mission.IsActive = true;
            };

            mission.CheckComplete = (map) =>
            {
                if (currentCheckpoint >= checkpoints.Count) return true;

                if (tekCar != null)
                {
                    float dist = Distance(map.tekCar.WorldPosition, checkpoints[currentCheckpoint]);
                    if (dist < 100)
                    {
                        currentCheckpoint++;
                        if (currentCheckpoint < checkpoints.Count)
                        {
                            mission.MarkerPosition = checkpoints[currentCheckpoint];
                            mission.Description = $"Чекпоинт {currentCheckpoint}/{checkpoints.Count}";
                        }
                        else
                        {
                            mission.MarkerPosition = null;
                        }
                    }
                }

                return currentCheckpoint >= checkpoints.Count;
            };

            mission.CheckFail = (map) =>
            {
                if (mission.IsActive)
                {
                    return player.health <= 0 || player.searchLevel > 0;
                }
                return false;
            };

            mission.OnComplete = (map) =>
            {
                map.player.money += 2000;
                map.hud.ShowMissionResult("Гонка выиграна!", true);
            };

            mission.OnFail = (map) =>
            {
                map.hud.ShowMissionResult("Гонка провалена!", false);
            };

            return mission;
        }
    }
}