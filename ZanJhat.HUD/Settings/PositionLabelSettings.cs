using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class PositionLabelSettings
    {
        public bool Enable { get; set; } = true;

        public Anchor Anchor { get; set; } = Anchor.TopLeft;

        public float MarginX { get; set; } = 72f;

        public float MarginY { get; set; } = 66f;

        public float FontScale { get; set; } = 0.8f;
    }
}
