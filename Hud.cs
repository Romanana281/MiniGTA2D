using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;


namespace Project5
{
    public partial class Hud : UserControl
    {
        private Map map;
        private Player player;
        private Form form;

        private string missionResultText = null;
        private DateTime missionResultTime = DateTime.MinValue;
        private bool missionResultIsSuccess = false;
        private const float animationDuration = 3f;

        private ComponentResourceManager resource = new ComponentResourceManager(typeof(Player));

        public Hud(Map map, Player player, Form form)
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.Transparent;

            this.map = map;
            this.player = player;
            this.form = form;
            this.Enabled = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawHud(e.Graphics);
        }

        public void UpdateHud()
        {
            this.Invalidate();
        }

        public void ShowMissionResult(string text, bool isSuccess)
        {
            missionResultText = text;
            missionResultTime = DateTime.Now;
            missionResultIsSuccess = isSuccess;
            Invalidate();
        }

        private void DrawHud(Graphics g)
        {
            Font missionFont = new Font("Times New Roman", 12, FontStyle.Bold);
            Brush whiteBrush = new SolidBrush(Color.White);
            Brush blackBgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));

            Font font = new Font("Times New Roman", 36, FontStyle.Bold);
            Brush brush = new SolidBrush(Color.FromArgb(255, 147, 170, 0));
            string Text = $"$ {player.money}";
            SizeF textSize = g.MeasureString(Text, font);

            g.DrawString(Text, font, brush, form.Width - textSize.Width - 10, 10);

            Image fullHealthImage = (Image)resource.GetObject("Heart_32x32");
            Image halfHealthImage = (Image)resource.GetObject("Heart_16x16");

            double Width = player.health * fullHealthImage.Width + player.health * 5;
            if (player.health % 1 != 0)
            {
                Width = (int)(player.health + 1) * fullHealthImage.Width + (int)(player.health + 1) * 5;
            }
            int startX = (int)(form.Width - Width - 20), startY = 55;

            for (int i = 0; i < player.health; i++)
            {

                if (player.health % 1 != 0 && i == 0)
                {
                    g.DrawImage(halfHealthImage, startX + halfHealthImage.Width, startY + halfHealthImage.Height / 2);
                }
                else
                {
                    g.DrawImage(fullHealthImage, startX + i * fullHealthImage.Width + i * 5, startY);
                }

            }

            float weaponY = 0;
            float weaponX = 0;
            bool hasWeapon = player.weapons.Count > 0 && player.tekWeapon != null;
            Image currentWeaponImg = null;
            float weaponHeight = startY + fullHealthImage.Height;

            if (hasWeapon)
            {
                currentWeaponImg = (Image)resource.GetObject(player.tekWeapon.type == 0 ? "gun" : "machine");

                weaponX = form.Width - currentWeaponImg.Width * 2 - 20;
                weaponY = startY + fullHealthImage.Height;

                weaponHeight += weaponY;
                g.DrawImage(currentWeaponImg,
                    new Rectangle((int)weaponX, (int)weaponY, currentWeaponImg.Width * 2, currentWeaponImg.Height * 2),
                    0, 0, currentWeaponImg.Width, currentWeaponImg.Height,
                    GraphicsUnit.Pixel);

                if (player.weapons.Count == 2)
                {
                    WeaponInfo secondWeapon = player.weapons[0] == player.tekWeapon
                        ? player.weapons[1]
                        : player.weapons[0];

                    Image secondWeaponImg = (Image)resource.GetObject(secondWeapon.type == 0 ? "gun" : "machine");

                    float secondX = form.Width - secondWeaponImg.Width - 20;
                    float secondY = weaponY + currentWeaponImg.Height * 2 + 10;
                    weaponHeight += currentWeaponImg.Height * 2 + 10;

                    g.DrawImage(secondWeaponImg, secondX, secondY);
                }
            }

            if (map.activeMission != null)
            {
                string missionText = $"{map.activeMission.Name}: {map.activeMission.Description}";
                if (map.activeMission.TimeLimitSeconds > 0)
                {
                    double timeLeft = map.activeMission.TimeLimitSeconds -
                        (DateTime.Now - map.activeMission.StartTime).TotalSeconds;
                    missionText += $" (Осталось: {Math.Max(0, timeLeft):F1}s)";
                }

                float blockWidth = 200f;
                float blockX = form.Width - blockWidth - 20;
                float blockY = weaponHeight + 10;

                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word,
                    FormatFlags = StringFormatFlags.LineLimit
                };

                RectangleF textRect = new RectangleF(blockX, blockY, blockWidth, 200);
                SizeF measuredSize = g.MeasureString(missionText, missionFont, (int)blockWidth, sf);

                g.FillRectangle(blackBgBrush, blockX - 10, blockY - 10, blockWidth + 20, measuredSize.Height + 20);

                g.DrawString(missionText, missionFont, whiteBrush, textRect, sf);
            }

            if (player.searchLevel >= 1)
            {
                Image starNormal = (Image)resource.GetObject("stars_2");
                Image starBlink = (Image)resource.GetObject("stars_1");

                if (starNormal == null) return;

                int starsToDraw = Math.Min(player.searchLevel, 5);

                bool isBlinking = player.searchTimer < 10;
                bool blinkState = (DateTime.Now.Millisecond / 500) % 5 == 0;

                Image currentStar = starNormal;
                if (isBlinking && blinkState)
                {
                    currentStar = starBlink ?? starNormal;
                }

                int starWidth = currentStar.Width;
                int starHeight = currentStar.Height;

                int totalStarsWidth = starsToDraw * starWidth + (starsToDraw - 1) * 10;

                float starsX = (form.Width - totalStarsWidth) / 2f;
                float starsY = 20;

                for (int i = 0; i < starsToDraw; i++)
                {
                    float x = starsX + i * (starWidth + 10);
                    g.DrawImage(currentStar, x, starsY);
                }
            }

            if (missionResultText != null)
            {
                float elapsed = (float)(DateTime.Now - missionResultTime).TotalSeconds;

                if (elapsed < animationDuration)
                {
                    float opacity;
                    float scale = 1f;

                    if (elapsed <= 1f)
                    {
                        opacity = elapsed; 
                        scale = 0.8f + elapsed * 0.4f;
                    }
                    else if (elapsed <= 2f)
                    {
                        opacity = 1f;
                        scale = 1.2f;
                    }
                    else
                    {
                        opacity = 1f - (elapsed - 2f);
                        scale = 1.2f - (elapsed - 2f) * 0.2f;
                    }

                    opacity = Math.Max(0f, Math.Min(1f, opacity));

                    int alpha = (int)(255 * opacity);

                    Font resultFont = new Font("Times New Roman", 48, FontStyle.Bold);
                    Color textColor = missionResultIsSuccess ? Color.LimeGreen : Color.Red;
                    Color borderColor = missionResultIsSuccess ? Color.Gold : Color.DarkRed;

                    Brush textBrush = new SolidBrush(Color.FromArgb(alpha, textColor));

                    SizeF textS = g.MeasureString(missionResultText, resultFont);
                    SizeF scaledSize = new SizeF(textS.Width * scale, textS.Height * scale);

                    float centerX = form.Width / 2f;
                    float centerY = form.Height / 2f;

                    float textX = centerX - (scaledSize.Width * scale) / 2;
                    float textY = centerY - (scaledSize.Height * scale) / 2;

                    g.TranslateTransform(centerX, centerY);
                    g.ScaleTransform(scale, scale);
                    g.TranslateTransform(-centerX, -centerY);

                    g.DrawString(missionResultText, resultFont, textBrush, textX, textY);

                    g.ResetTransform();

                    Invalidate();
                }
                else
                {
                    missionResultText = null;
                }
            }

        }
    }
}
