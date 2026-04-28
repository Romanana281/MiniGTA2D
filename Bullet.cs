using System;
using System.Drawing;

namespace Project5
{
    public class Bullet
    {
        public PointF Position { get; set; }

        public float Angle { get; set; }

        public float Damage { get; set; }

        public float Speed { get; set; } = 15f;

        public bool Active { get; set; } = true;

        public int Lifetime { get; set; } = 100;

        public string owner { get; set; }

        public Bullet(PointF startPos, float angle, float damage, string owner)
        {
            this.Position = startPos;
            this.Angle = angle;
            this.Damage = damage;
            this.owner = owner;
        }

        public void Update()
        {
            if (!Active) return;

            float rad = Angle * (float)Math.PI / 180f;
            Position = new PointF(
                Position.X + Speed * (float)Math.Cos(rad),
                Position.Y + Speed * (float)Math.Sin(rad)
            );

            Lifetime--;
            if (Lifetime <= 0) Active = false;
        }
    }
}
