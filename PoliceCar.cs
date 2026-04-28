using Project5.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Project5
{
    public partial class PoliceCar : Car
    {
        public List<PoliceUnit> Officers = new List<PoliceUnit>(2);
        private bool officersDeployed = false;
        private float deployDistance = 200f;

        private string currentMigalka = "blue";
        private int migalkaTimer = 0;
        private int types;

        public PoliceCar(Map map, Point worldPosition, int types)
            : base(map, worldPosition, types)
        {
            this.Tag = "police_car";
            this.types = types;
            for (int i = 0; i < 2; i++)
            {
                var officer = new PoliceUnit(map, worldPosition, map.player.searchLevel, types - 2)
                {
                    Visible = false
                };
                Officers.Add(officer);
            }

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.AllPaintingInWmPaint |
              ControlStyles.UserPaint, true);
        }

        public override void Draw(Graphics g, Point cameraPosition)
        {
            Image carImage = null;
            if (type == 6)
            {
                carImage = (Image)resources.GetObject($"car_{type}_{currentMigalka}");
            }
            else if (type == 7)
            {
                carImage = (Image)resources.GetObject($"car_{type}");
            }

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

        protected override void Update_Tick(object sender, EventArgs e)
        {
            if (isDied) return;

            if (type == 6)
            {
                migalkaTimer++;
                if (migalkaTimer >= 5)
                {
                    migalkaTimer = 0;
                    currentMigalka = (currentMigalka == "red") ? "blue" : "red";
                }
            }

            Point targetPos = map.tekCar != null ? map.tekCar.WorldPosition : map.player.WorldPosition;

            float targetCenterX = targetPos.X + Width / 2f;
            float targetCenterY = targetPos.Y + Height / 2f;

            float carCenterX = WorldPosition.X + Width / 2f;
            float carCenterY = WorldPosition.Y + Height / 2f;

            float dx = targetCenterX - carCenterX;
            float dy = targetCenterY - carCenterY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            bool playerInCar = map.tekCar != null;
            if (!playerInCar && dist < deployDistance && !officersDeployed)
            {
                DeployOfficers();
                officersDeployed = true;
                currentMigalka = "no";
                speed = 0;
                return;
            }

            if (playerInCar && officersDeployed)
            {
                ReturnOfficersToCar();
                officersDeployed = false;
            }

            if (officersDeployed && Officers.All(o => o == null || o.IsDisposed || o.isDied))
            {
                isDied = true;
                deathTime = DateTime.Now;
                deathProcessed = true;
                speed = 0;
                return;
            }

            KeysUp = false;
            KeysDown = false;
            KeysLeft = false;
            KeysRight = false;
            KeysSpace = false;

            if (officersDeployed || dist < 90f)
            {
                KeysSpace = true;
            }
            else if (dist > 60f)
            {
                KeysUp = true;

                float angleToTarget = (float)(Math.Atan2(dx, -dy) * 180 / Math.PI);
                if (angleToTarget < 0) angleToTarget += 360f;

                float angleDiff = angleToTarget - Rotation;
                while (angleDiff > 180) angleDiff -= 360;
                while (angleDiff < -180) angleDiff += 360;

                if (angleDiff > 10f)
                    KeysRight = true;
                else if (angleDiff < -10f)
                    KeysLeft = true;

                if (Math.Abs(angleDiff) > 60f && speed > 8f)
                    KeysSpace = true;
            };

            base.Update_Tick(sender, e);
        }

        private void DeployOfficers()
        {
            KeysUp = KeysDown = KeysLeft = KeysRight = false;
            KeysSpace = true;

            foreach (var officer in Officers)
            {
                float offset = Officers.IndexOf(officer) == 0 ? 90f : -90f;
                float exitAngle = (Rotation + offset) % 360f;
                if (exitAngle < 0) exitAngle += 360f;

                float rad = exitAngle * (float)Math.PI / 180f;

                Point spawnPos = new Point(
                    WorldPosition.X + (int)(Math.Cos(rad) * 90),
                    WorldPosition.Y + (int)(Math.Sin(rad) * 90)
                );

                officer.WorldPosition = spawnPos;
                officer.Visible = true;

                if (!map.policeUnits.Contains(officer))
                    map.policeUnits.Add(officer);

                if (!map.Controls.Contains(officer))
                {
                    map.Controls.Add(officer);
                    officer.BringToFront();
                }
            }
        }

        private void ReturnOfficersToCar()
        {
            foreach (var officer in Officers)
            {
                if (map.policeUnits.Contains(officer))
                {
                    map.policeUnits.Remove(officer);
                    if (map.Controls.Contains(officer))
                        map.Controls.Remove(officer);
                    officer.Dispose();
                }
            }
            Officers.Clear();

            for (int i = 0; i < 2; i++)
            {
                var officer = new PoliceUnit(map, WorldPosition, map.player.searchLevel, types - 2)
                {
                    Visible = false
                };
                Officers.Add(officer);
            }
        }
    }
}