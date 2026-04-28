using System.Drawing;

namespace Project5
{
    public class WeaponInfo
    {
        public int type { get; set; }
        public int rechargeCount { get; set; }
        public int currentAmmo { get; set; }
        public float rechargeTime { get; set; }
        public float tekRechargeTime { get; set; } = 0;
        public float split { get; set; }             
        public Point WorldPosition { get; set; }
    }
}
