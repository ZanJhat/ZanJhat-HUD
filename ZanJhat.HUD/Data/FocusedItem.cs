using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public enum FocusInputType
    {
        None,
        Vr,
        Mouse,
        Gamepad,
        Touch
    }

    public class FocusedItem
    {
        public string Name { get; set; }

        public Widget Owner { get; set; }

        public bool IsDirty { get; set; }

        public Action<FocusedItem> OnFocusEnter { get; set; }
        public Action<FocusedItem> OnFocusUpdate { get; set; }
        public Action<FocusedItem> OnFocusLost { get; set; }

        public Action<FocusedItem, AutoSizeCanvasWidget, FocusInputType> OnBuildFocusContent { get; set; }
    }
}
