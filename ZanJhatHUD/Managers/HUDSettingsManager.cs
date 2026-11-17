using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TemplatesDatabase;
using System.IO;
using System.Text;
using XmlUtilities;
using System.Text.RegularExpressions;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public static class HUDSettingsManager
    {
        public const string PackageName = "ZanJhat.HUD";

        public static LookingAtInfoSettings LookingAtInfoSettings = new LookingAtInfoSettings();
        public static FocusWidgetSettings FocusWidgetSettings = new FocusWidgetSettings();
        public static PositionLabelSettings PositionLabelSettings = new PositionLabelSettings();
        public static ClothingInfoSettings ClothingInfoSettings = new ClothingInfoSettings();
        public static TimeInfoSettings TimeInfoSettings = new TimeInfoSettings();

        public static void Initialize()
        {
            LoadSettings();
            ModSettingsManager.SettingChanged += OnSettingChanged;
        }

        public static void LoadSettings()
        {
            LookingAtInfoSettings.LoadSettings();
            FocusWidgetSettings.LoadSettings();
            PositionLabelSettings.LoadSettings();
            ClothingInfoSettings.LoadSettings();
            TimeInfoSettings.LoadSettings();
        }

        public static void OnSettingChanged(string path, object newValue)
        {
            LookingAtInfoSettings.SettingChanged(path, newValue);
            FocusWidgetSettings.SettingChanged(path, newValue);
            PositionLabelSettings.SettingChanged(path, newValue);
            ClothingInfoSettings.SettingChanged(path, newValue);
            TimeInfoSettings.SettingChanged(path, newValue);
        }
    }
}
