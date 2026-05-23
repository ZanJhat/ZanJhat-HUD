using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class ClothingInfoSettings
    {
        public bool Enable { get; set; } = true;

        public Anchor Anchor { get; set; } = Anchor.TopLeft;

        public float MarginX { get; set; } = 72f;

        public float MarginY { get; set; } = 94f;

        public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Vertical;

        public bool HideEmptyClothingSlots { get; set; } = false;

        public float Scale { get; set; } = 0.5f;
    }
}
