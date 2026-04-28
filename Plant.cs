using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Project5
{
    public partial class Plant : UserControl
    {
        private ComponentResourceManager resources = new ComponentResourceManager(typeof(Plant));

        public readonly List<Color> colorPlant = new List<Color>() {
             Color.FromArgb(255, 117, 249, 77),
             Color.FromArgb(255, 34, 177, 76),
             Color.FromArgb(255, 161, 251, 142),
             Color.FromArgb(255, 161, 251, 132),
             Color.FromArgb(255, 67, 105, 55),

        };

        private Color color;
        private Image plantImage = null;
        public int startX { get; set; }
        public int startY { get; set; }
        public int endX { get; set; }
        public int endY { get; set; }
        public Plant(Color color, int startX, int startY)
        {
            this.color = color;
            this.startX = startX;
            this.startY = startY;

            if (color == colorPlant[0])
                plantImage = (Image)resources.GetObject("Bed_3");
            else if (color == colorPlant[1])
                plantImage = (Image)resources.GetObject("bush");
            else if (color == colorPlant[2])
                plantImage = (Image)resources.GetObject("Bed_1");
            else if (color == colorPlant[3])
                plantImage = (Image)resources.GetObject("Bed_2");
            else if (color == colorPlant[4])
                plantImage = (Image)resources.GetObject("tree");

            if (plantImage != null)
            {
                endX = startX + plantImage.Width;
                endY = startY + plantImage.Height;
            }
        }
        public void Draw(Graphics g, Point cameraPosition)
        {
            g.DrawImage(plantImage, startX - cameraPosition.X, startY - cameraPosition.Y);
        }
    }
}
