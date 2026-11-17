using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class ClothingInfoSettings
    {
        public static string PackageName => HUDSettingsManager.PackageName;
        public const string GroupName = "ClothingInfoSettings";

        public bool ShowHUD { get; set; } = true;

        public bool HideEmptyClothingSlots { get; set; } = false;

        public Anchor Anchor { get; set; } = Anchor.TopLeft;

        public float MarginX { get; set; } = 72f;

        public float MarginY { get; set; } = 94f;

        public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Vertical;

        public float Scale { get; set; } = 0.5f;

        public void LoadSettings()
        {
            if (ModSettingsManager.TryGet<bool>(out bool hud, PackageName, GroupName, "ShowHUD"))
                ShowHUD = hud;

            if (ModSettingsManager.TryGet<bool>(out bool hideEmptyClothingSlots, PackageName, GroupName, "HideEmptyClothingSlots"))
                HideEmptyClothingSlots = hideEmptyClothingSlots;

            if (ModSettingsManager.TryGet<Anchor>(out Anchor anchor, PackageName, GroupName, "Anchor"))
                Anchor = anchor;

            if (ModSettingsManager.TryGet<float>(out float marginX, PackageName, GroupName, "MarginX"))
                MarginX = marginX;

            if (ModSettingsManager.TryGet<float>(out float marginY, PackageName, GroupName, "MarginY"))
                MarginY = marginY;

            if (ModSettingsManager.TryGet<LayoutDirection>(out LayoutDirection direction, PackageName, GroupName, "LayoutDirection"))
                LayoutDirection = direction;

            if (ModSettingsManager.TryGet<float>(out float scale, PackageName, GroupName, "Scale"))
                Scale = scale;
        }

        public void SettingChanged(string path, object newValue)
        {
            if (path == $"{PackageName}/{GroupName}/ShowHUD")
                ShowHUD = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/HideEmptyClothingSlots")
                HideEmptyClothingSlots = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/Anchor")
                Anchor = (Anchor)newValue;

            else if (path == $"{PackageName}/{GroupName}/MarginX")
                MarginX = (float)newValue;

            else if (path == $"{PackageName}/{GroupName}/MarginY")
                MarginY = (float)newValue;

            else if (path == $"{PackageName}/{GroupName}/LayoutDirection")
                LayoutDirection = (LayoutDirection)newValue;

            else if (path == $"{PackageName}/{GroupName}/Scale")
                Scale = (float)newValue;
        }
    }
}
