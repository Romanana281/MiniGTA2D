using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Project5
{
    public partial class PoliceUnit : Humon
    {
        private Map map;
        private Random rand = new Random();
        public int WeaponType;
        public float ShootCooldown = 0f;
        public float ShootDelay = 1.5f;
        private string weapon = "";

        public PoliceUnit(Map map, Point worldPosition, int wantedLevel, int types)
            : base(speed: 4, health: wantedLevel >= 3 ? 12 : 8, type: types, WorldPosition: worldPosition)
        {
            this.map = map;
            switch (wantedLevel)
            {
                case 1:
                case 2:
                    WeaponType = 0;
                    ShootDelay = 1.8f;
                    break;
                case 3:
                    WeaponType = rand.Next(0, 100) < 70 ? 0 : 1;
                    ShootDelay = 1.2f;
                    break;
                case 4:
                    WeaponType = rand.Next(0, 100) < 40 ? 1 : 2;
                    ShootDelay = 0.8f;
                    break;
                case 5:
                    WeaponType = 2;
                    ShootDelay = 0.4f;
                    break;
            }

            switch (WeaponType)
            {
                case 0:
                    weapon = "gun";
                    break;
                case 1:
                    weapon = "machine";
                    break;
                case 2:
                    weapon = "silencer";
                    break;
            }

            this.Tag = $"police;{weapon}";
        }

        public new void UpdatePosition(Map map, Form form)
        {
            if (isDied)
            {
                base.UpdatePosition(map, form);
                return;
            }

            base.UpdatePosition(map, form);

            if (map.player == null) return;

            float playerCenterX = map.player.WorldPosition.X + map.player.Width / 2f;
            float playerCenterY = map.player.WorldPosition.Y + map.player.Height / 2f;
            float policeCenterX = WorldPosition.X + Width / 2f;
            float policeCenterY = WorldPosition.Y + Height / 2f;

            float dx = playerCenterX - policeCenterX;
            float dy = playerCenterY - policeCenterY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            ShootCooldown -= 0.016f;

            if (dist < 200 && ShootCooldown <= 0)
            {
                ShootAtPlayer();
                ShootCooldown = ShootDelay + (float)rand.NextDouble() * 0.3f;
            }

            const float attackRange = 75f;

            const float stopRange = 65f;

            const float policeMinDist = 60f;

            foreach (var other in map.policeUnits)
            {
                if (other == this || other.isDied || other.IsDisposed) continue;

                float odx = policeCenterX - (other.WorldPosition.X + other.Width / 2f);
                float ody = policeCenterY - (other.WorldPosition.Y + other.Height / 2f);
                float oDist = (float)Math.Sqrt(odx * odx + ody * ody);

                if (oDist < policeMinDist && oDist > 0)
                {
                    float pushForce = (policeMinDist - oDist) * 1.5f;
                    float pushX = (odx / oDist) * pushForce;
                    float pushY = (ody / oDist) * pushForce;

                    WorldPosition = new Point(WorldPosition.X + (int)pushX, WorldPosition.Y + (int)pushY);

                    other.WorldPosition = new Point(
                        other.WorldPosition.X - (int)(pushX * 0.6f),
                        other.WorldPosition.Y - (int)(pushY * 0.6f)
                    );
                }
            }

            if (dist <= stopRange)
            {
                KeysUp = KeysDown = KeysLeft = KeysRight = false;
                return;
            }

            KeysUp = KeysDown = KeysLeft = KeysRight = false;

            if (dist > attackRange)
            {
                float targetAngle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);

                if (targetAngle < 0) targetAngle += 360;

                if (!this.Tag.ToString().Contains("lastDir")) this.Tag = $"police;{weapon};lastDir=0";

                string[] tagParts = this.Tag.ToString().Split(';');
                float lastDirection = float.Parse(tagParts.FirstOrDefault(t => t.StartsWith("lastDir="))?.Substring(8) ?? "0");
                int candidateDir = (int)(Math.Round(targetAngle / 45.0) * 45.0) % 360;

                float angleDiff = Math.Min(Math.Abs(targetAngle - lastDirection), 360 - Math.Abs(targetAngle - lastDirection));
                if (angleDiff > 30f)
                {
                    lastDirection = candidateDir;
                    this.Tag = $"police;{weapon};lastDir={lastDirection}";
                }

                switch ((int)lastDirection)
                {
                    case 0: KeysRight = true; break;
                    case 45: KeysRight = true; KeysDown = true; break;
                    case 90: KeysDown = true; break;
                    case 135: KeysLeft = true; KeysDown = true; break;
                    case 180: KeysLeft = true; break;
                    case 225: KeysLeft = true; KeysUp = true; break;
                    case 270: KeysUp = true; break;
                    case 315: KeysRight = true; KeysUp = true; break;
                    default: KeysRight = true; break;
                }
            }
        }

        private void ShootAtPlayer()
        {
            if (map.player == null || isDied) return;

            float dx = map.player.WorldPosition.X + map.player.Width / 2f - (WorldPosition.X + Width / 2f);
            float dy = map.player.WorldPosition.Y + map.player.Height / 2f - (WorldPosition.Y + Height / 2f);
            float angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);

            PointF bulletStart = new PointF(
                WorldPosition.X + Width / 2f + 15 * (float)Math.Cos(angle * Math.PI / 180f),
                WorldPosition.Y + Height / 2f + 15 * (float)Math.Sin(angle * Math.PI / 180f)
            );

            float damage = 1;

            switch (WeaponType)
            {
                case 0:
                    damage = 0.5f;
                    break;
                case 1:
                    damage = 1f;
                    break;
                case 2:
                    damage = 1.5f;
                    break;
            }

            var bullet = new Bullet(bulletStart, angle, damage, "police")
            {
                Speed = 15f
            };

            map.bullets.Add(bullet);
        }
    }
}