using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Project5
{
    public partial class House : UserControl
    {
        private ComponentResourceManager resources = new ComponentResourceManager(typeof(House));

        public readonly List<Color> colorHouse = new List<Color>() {
             Color.FromArgb(255, 255, 242, 0),
             Color.FromArgb(255, 127, 127, 127),
             Color.FromArgb(255, 240, 134, 80)
        };

        public Dictionary<Point, Color> mask = new Dictionary<Point, Color>();

        private Color color;
        private Image houseImage = null;
        public int startX { get; set; }
        public int startY { get; set; }
        public int endX { get; set; }
        public int endY { get; set; }
        public List<Point> doorOut { get; set; } = new List<Point>();
        public List<Point> doorIn { get; set; } = new List<Point>();
        public Point WorldPositionPlayer { get; set; }
        public float baseRotation = 0;
        public House(Color color, int startX, int startY, Form form)
        {
            this.color = color;
            this.startX = startX;
            this.startY = startY;

            if (color == colorHouse[0])
                houseImage = (Image)resources.GetObject("Factory_Top_1");
            else if (color == colorHouse[1])
                houseImage = (Image)resources.GetObject("Factory_Top_2");
            else if (color == colorHouse[2])
                houseImage = (Image)resources.GetObject("Factory_Top_3");

            if (houseImage != null)
            {
                endX = startX + houseImage.Width;
                endY = startY + houseImage.Height;
                this.createMask(form);
            }
        }
        public void DrawOut(Graphics g, Point cameraPosition)
        {
            g.DrawImage(houseImage, startX - cameraPosition.X, startY - cameraPosition.Y);
        }
        public void DrawIn(Graphics g, Form form)
        {
            Image img = null;

            if (color == colorHouse[0])
                img = (Image)resources.GetObject("Factory_In_1");
            else if (color == colorHouse[1])
                img = (Image)resources.GetObject("Factory_In_2");
            else if (color == colorHouse[2])
                img = (Image)resources.GetObject("Factory_In_3");

            float Width = (float)(img.Width * 1.4);
            float Height = (float)(img.Height * 1.4);

            g.DrawImage(img, form.Width / 2 - Width / 2, form.Height / 2 - Height / 2, Width, Height);
        }
        public void createMask(Form form)
        {
            Image image = null;

            if (color == colorHouse[0])
            {
                image = (Image)resources.GetObject("mask_Factory_1");
                this.baseRotation = 90;
            }
            else if (color == colorHouse[1])
                image = (Image)resources.GetObject("mask_Factory_2");
            else if (color == colorHouse[2])
                image = (Image)resources.GetObject("mask_Factory_3");

            int Width = (int)(image.Width * 1.4);
            int Height = (int)(image.Height * 1.4);

            Bitmap img = new Bitmap(image, Width, Height);

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int x = 0; x < Width; x += 1)
            {
                for (int y = 0; y < Height; y += 1)
                {
                    Color pixelColor = img.GetPixel(x, y);
                    mask[new Point(x, y)] = pixelColor;

                    if (pixelColor == Color.FromArgb(255, 0, 0, 0))
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            doorIn.AddRange(
                new List<Point>() {
                    new Point(minX, minY),
                    new Point(maxX, maxY)
                }
            );

            this.Width = Width;
            this.Height = Height;
        }

    }
}
