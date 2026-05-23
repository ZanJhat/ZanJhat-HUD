using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class TimeInfoSettings
    {
        public bool Enable { get; set; } = true;

        public Anchor Anchor { get; set; } = Anchor.TopLeft;

        public float MarginX { get; set; } = 114f;

        public float MarginY { get; set; } = 94f;

        public float Scale { get; set; } = 0.8f;
    }
}
