using System;
using System.Drawing;

namespace Project5
{
    public class Mission
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public Point? MarkerPosition { get; set; }
        public Action<Map> StartAction { get; set; } 
        public Func<Map, bool> CheckComplete { get; set; } 
        public Func<Map, bool> CheckFail { get; set; }
        public Action<Map> OnComplete { get; set; }
        public Action<Map> OnFail { get; set; }
        public DateTime StartTime { get; set; }
        public int TimeLimitSeconds { get; set; }
    }
}