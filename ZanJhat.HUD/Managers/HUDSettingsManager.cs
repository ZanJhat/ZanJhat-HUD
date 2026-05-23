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
        public static LookingAtInfoSettings LookingAtInfoSettings;
        public static FocusWidgetSettings FocusWidgetSettings;
        public static PositionLabelSettings PositionLabelSettings;
        public static ClothingInfoSettings ClothingInfoSettings;
        public static TimeInfoSettings TimeInfoSettings;

        public static void Initialize()
        {
            RegisterModSettings();
            ResolveSettings();
            RegisterSettingsScreen();
        }

        public static void RegisterModSettings()
        {
            CoreSettingsManager.Register(new LookingAtInfoSettings());
            CoreSettingsManager.Register(new FocusWidgetSettings());
            CoreSettingsManager.Register(new PositionLabelSettings());
            CoreSettingsManager.Register(new ClothingInfoSettings());
            CoreSettingsManager.Register(new TimeInfoSettings());
        }

        public static void ResolveSettings()
        {
            LookingAtInfoSettings = CoreSettingsManager.Get<LookingAtInfoSettings>();
            FocusWidgetSettings = CoreSettingsManager.Get<FocusWidgetSettings>();
            PositionLabelSettings = CoreSettingsManager.Get<PositionLabelSettings>();
            ClothingInfoSettings = CoreSettingsManager.Get<ClothingInfoSettings>();
            TimeInfoSettings = CoreSettingsManager.Get<TimeInfoSettings>();
        }

        public static void RegisterSettingsScreen()
        {
            // Looking At Info
            SettingsScreenRegistry.Register("Looking At Info", builder =>
            {
                builder.AddToggle("Enable",
                    () => LookingAtInfoSettings.Enable,
                    v => LookingAtInfoSettings.Enable = v);

                builder.AddToggle("Show Detail",
                    () => LookingAtInfoSettings.ShowDetail,
                    v => LookingAtInfoSettings.ShowDetail = v);

                builder.AddToggle("Show Inventory",
                    () => LookingAtInfoSettings.ShowInventory,
                    v => LookingAtInfoSettings.ShowInventory = v);

                builder.AddToggle("Show Package Name",
                    () => LookingAtInfoSettings.ShowPackageName,
                    v => LookingAtInfoSettings.ShowPackageName = v);
            });

            // Focus Widget
            SettingsScreenRegistry.Register("Focus Widget", builder =>
            {
                builder.AddToggle("Enable",
                    () => FocusWidgetSettings.Enable,
                    v => FocusWidgetSettings.Enable = v);

                builder.AddToggle("Show Detail",
                    () => FocusWidgetSettings.ShowDetail,
                    v => FocusWidgetSettings.ShowDetail = v);

                builder.AddToggle("Show Description",
                    () => FocusWidgetSettings.ShowDescription,
                    v => FocusWidgetSettings.ShowDescription = v);

                builder.AddToggle("Show Package Name",
                    () => FocusWidgetSettings.ShowPackageName,
                    v => FocusWidgetSettings.ShowPackageName = v);
            });

            // Position Label
            SettingsScreenRegistry.Register("Position Label", builder =>
            {
                builder.AddToggle("Enable",
                    () => PositionLabelSettings.Enable,
                    v => PositionLabelSettings.Enable = v);

                builder.AddEnum("Anchor",
                    () => PositionLabelSettings.Anchor,
                    v => PositionLabelSettings.Anchor = v,
                    v => Regex.Replace(v.ToString(), "([a-z])([A-Z])", "$1 $2"));

                builder.AddSlider("Margin X",
                   () => PositionLabelSettings.MarginX,
                   v => PositionLabelSettings.MarginX = v,
                   -256f, 256f, 1f);

                builder.AddSlider("Margin Y",
                   () => PositionLabelSettings.MarginY,
                   v => PositionLabelSettings.MarginY = v,
                   -128f, 128f, 1f);

                builder.AddSlider("Font Scale",
                   () => PositionLabelSettings.FontScale,
                   v => PositionLabelSettings.FontScale = v,
                   0.5f, 1.5f, 0.1f);
            });

            // Time Info
            SettingsScreenRegistry.Register("Time Info", builder =>
            {
                builder.AddToggle("Enable",
                    () => TimeInfoSettings.Enable,
                    v => TimeInfoSettings.Enable = v);

                builder.AddEnum("Anchor",
                    () => TimeInfoSettings.Anchor,
                    v => TimeInfoSettings.Anchor = v,
                    v => Regex.Replace(v.ToString(), "([a-z])([A-Z])", "$1 $2"));

                builder.AddSlider("Margin X",
                   () => TimeInfoSettings.MarginX,
                   v => TimeInfoSettings.MarginX = v,
                   -256f, 256f, 1f);

                builder.AddSlider("Margin Y",
                   () => TimeInfoSettings.MarginY,
                   v => TimeInfoSettings.MarginY = v,
                   -128f, 128f, 1f);

                builder.AddSlider("Scale",
                   () => TimeInfoSettings.Scale,
                   v => TimeInfoSettings.Scale = v,
                   0.5f, 1.5f, 0.1f);
            });

            // Clothing Info
            SettingsScreenRegistry.Register("Clothing Info", builder =>
            {
                builder.AddToggle("Enable",
                    () => ClothingInfoSettings.Enable,
                    v => ClothingInfoSettings.Enable = v);

                builder.AddEnum("Anchor",
                    () => ClothingInfoSettings.Anchor,
                    v => ClothingInfoSettings.Anchor = v,
                    v => Regex.Replace(v.ToString(), "([a-z])([A-Z])", "$1 $2"));

                builder.AddSlider("Margin X",
                   () => ClothingInfoSettings.MarginX,
                   v => ClothingInfoSettings.MarginX = v,
                   -256f, 256f, 1f);

                builder.AddSlider("Margin Y",
                   () => ClothingInfoSettings.MarginY,
                   v => ClothingInfoSettings.MarginY = v,
                   -128f, 128f, 1f);

                builder.AddEnum("Layout Direction",
                    () => ClothingInfoSettings.LayoutDirection,
                    v => ClothingInfoSettings.LayoutDirection = v,
                    v => Regex.Replace(v.ToString(), "([a-z])([A-Z])", "$1 $2"));

                builder.AddToggle("Hide Empty Clothing Slots",
                    () => ClothingInfoSettings.HideEmptyClothingSlots,
                    v => ClothingInfoSettings.HideEmptyClothingSlots = v);

                builder.AddSlider("Scale",
                   () => ClothingInfoSettings.Scale,
                   v => ClothingInfoSettings.Scale = v,
                   0.5f, 1.5f, 0.1f);
            });
        }
    }
}
