using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project5
{
    public partial class MissionPickup : UserControl
    {
        private Map map;
        public Mission AssociatedMission { get; set; }
        public Point WorldPosition { get; set; }
        private bool playerNearby = false;

        public MissionPickup(Map map, Point worldPosition, Mission mission)
        {
            this.map = map;
            this.WorldPosition = worldPosition;
            this.AssociatedMission = mission;
            this.Size = new Size(40, 40);
            this.BackColor = Color.Transparent;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        public void CheckPlayerProximity()
        {
            float dist = (float)Math.Sqrt(
                Math.Pow(map.player.WorldPosition.X - WorldPosition.X, 2) +
                Math.Pow(map.player.WorldPosition.Y - WorldPosition.Y, 2));

            playerNearby = dist < 100f;

            if (playerNearby && map.pressedKeys.Contains(Keys.I))
            {
                if (map.activeMission == null && !AssociatedMission.IsActive)
                {
                    map.StartMission(AssociatedMission);
                    map.missionPickups.Remove(this);
                    map.Invalidate(); 
                    this.Dispose();
                }
                map.pressedKeys.Remove(Keys.I);
            }

            map.Invalidate();
        }

        public void DrawOnMap(Graphics g, Point cameraPosition)
        {
            float screenX = WorldPosition.X - cameraPosition.X - 20;
            float screenY = WorldPosition.Y - cameraPosition.Y - 60;

            g.FillEllipse(Brushes.Yellow, screenX, screenY, 40, 40);
            g.DrawEllipse(Pens.Black, screenX, screenY, 40, 40);

            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("M", new Font("Arial", 20, FontStyle.Bold), Brushes.Black, new RectangleF(screenX, screenY, 40, 40), sf);

            if (playerNearby)
            {
                Font font = new Font("Arial", 12, FontStyle.Bold);
                string text = "[I] чтобы начать миссию";
                SizeF textSize = g.MeasureString(text, font);
                float textX = screenX + 20 - textSize.Width / 2;
                float textY = screenY + 50;

                g.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), textX - 5, textY - 5, textSize.Width + 10, textSize.Height + 10);
                g.DrawString(text, font, Brushes.White, textX, textY);
            }
        }
    }
}