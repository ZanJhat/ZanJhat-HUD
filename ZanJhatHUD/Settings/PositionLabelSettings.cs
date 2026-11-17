using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class PositionLabelSettings
    {
        public static string PackageName => HUDSettingsManager.PackageName;
        public const string GroupName = "PositionLabelSettings";

        public bool ShowHUD { get; set; } = true;

        public Anchor Anchor { get; set; } = Anchor.TopLeft;

        public float MarginX { get; set; } = 72f;

        public float MarginY { get; set; } = 67f;

        public float FontScale { get; set; } = 0.8f;

        public void LoadSettings()
        {
            if (ModSettingsManager.TryGet<bool>(out bool hud, PackageName, GroupName, "ShowHUD"))
                ShowHUD = hud;

            if (ModSettingsManager.TryGet<Anchor>(out Anchor anchor, PackageName, GroupName, "Anchor"))
                Anchor = anchor;

            if (ModSettingsManager.TryGet<float>(out float marginX, PackageName, GroupName, "MarginX"))
                MarginX = marginX;

            if (ModSettingsManager.TryGet<float>(out float marginY, PackageName, GroupName, "MarginY"))
                MarginY = marginY;

            if (ModSettingsManager.TryGet<float>(out float fontScale, PackageName, GroupName, "FontScale"))
                FontScale = fontScale;
        }

        public void SettingChanged(string path, object newValue)
        {
            if (path == $"{PackageName}/{GroupName}/ShowHUD")
                ShowHUD = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/Anchor")
                Anchor = (Anchor)newValue;

            else if (path == $"{PackageName}/{GroupName}/MarginX")
                MarginX = (float)newValue;

            else if (path == $"{PackageName}/{GroupName}/MarginY")
                MarginY = (float)newValue;

            else if (path == $"{PackageName}/{GroupName}/FontScale")
                FontScale = (float)newValue;
        }
    }
}
