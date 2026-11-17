using Engine;
using System;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class LookingAtInfoSettings
    {
        public static string PackageName => HUDSettingsManager.PackageName;
        public const string GroupName = "LookingAtInfoSettings";

        public bool ShowHUD { get; set; } = true;

        public bool ShowDetail { get; set; } = true;

        public bool ShowInventory { get; set; } = true;

        public bool ShowPackageName { get; set; } = true;

        public void LoadSettings()
        {
            if (ModSettingsManager.TryGet<bool>(out bool hud, PackageName, GroupName, "ShowHUD"))
                ShowHUD = hud;

            if (ModSettingsManager.TryGet<bool>(out bool detail, PackageName, GroupName, "ShowDetail"))
                ShowDetail = detail;

            if (ModSettingsManager.TryGet<bool>(out bool inventory, PackageName, GroupName, "ShowInventory"))
                ShowInventory = inventory;

            if (ModSettingsManager.TryGet<bool>(out bool packageName, PackageName, GroupName, "ShowPackageName"))
                ShowPackageName = packageName;
        }

        public void SettingChanged(string path, object newValue)
        {
            if (path == $"{PackageName}/{GroupName}/ShowHUD")
                ShowHUD = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/ShowDetail")
                ShowDetail = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/ShowInventory")
                ShowInventory = (bool)newValue;

            else if (path == $"{PackageName}/{GroupName}/ShowPackageName")
                ShowPackageName = (bool)newValue;
        }
    }
}
