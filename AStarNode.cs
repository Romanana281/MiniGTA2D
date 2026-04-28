using System.Drawing;

namespace Project5
{
    public class AStarNode
    {
        public Point Pos;
        public float G;
        public float H;
        public float F => G + H;
        public AStarNode Parent;
    }
}

