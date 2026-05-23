using Engine;
using Game;
using System;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class ClothingInfoHUDModule : HUDModule
    {
        public ComponentClothing m_componentClothing;

        public ClothingInfoSettings m_clothingInfoSettings;
        public float Scale => m_clothingInfoSettings.Scale;

        public float m_lastScale;

        public AutoSizeCanvasWidget m_clothingInfoWidget;
        public StackPanelWidget m_clothingInfoPanel;

        protected override void OnLoad()
        {
            m_componentClothing = Owner.Entity.FindComponent<ComponentClothing>(true);

            m_clothingInfoSettings = HUDSettingsManager.ClothingInfoSettings;
            m_lastScale = Scale;

            m_clothingInfoWidget = ComponentGui.ControlsContainerWidget.Children.Find<AutoSizeCanvasWidget>("ClothingInfoWidget", false);
            m_clothingInfoPanel = ComponentGui.ControlsContainerWidget.Children.Find<StackPanelWidget>("ClothingInfoPanel", false);

            if (m_clothingInfoWidget == null || m_clothingInfoPanel == null)
            {
                m_clothingInfoWidget = new AutoSizeCanvasWidget
                {
                    Name = "ClothingInfoWidget"
                };

                m_clothingInfoPanel = new StackPanelWidget
                {
                    Name = "ClothingInfoPanel",
                    Direction = m_clothingInfoSettings.LayoutDirection,
                    Margin = new Vector2(-2f * Scale)
                };
                m_clothingInfoWidget.Children.Add(m_clothingInfoPanel);

                m_clothingInfoPanel.Children.Add(CreateClothingSlot("Head"));
                m_clothingInfoPanel.Children.Add(CreateClothingSlot("Torso"));
                m_clothingInfoPanel.Children.Add(CreateClothingSlot("Legs"));
                m_clothingInfoPanel.Children.Add(CreateClothingSlot("Feet"));

                WidgetUtils.DisableHitTestRecursive(m_clothingInfoWidget);
                ComponentGui.ControlsContainerWidget.Children.Insert(0, m_clothingInfoWidget);
            }
        }

        public AutoSizeCanvasWidget CreateClothingSlot(string name)
        {
            AutoSizeCanvasWidget clothingSlot = new AutoSizeCanvasWidget
            {
                Name = name,
                Margin = new Vector2(2f * Scale)
            };

            RectangleWidget background = new RectangleWidget
            {
                FillColor = new Color(64, 64, 64, 128),
                OutlineColor = Color.DarkGray,
                OutlineThickness = 2f * Scale
            };
            clothingSlot.Children.Add(background);

            BlockIconWidget blockIconWidget = new BlockIconWidget
            {
                Name = name,
                Size = new Vector2(72f * Scale),
                HorizontalAlignment = WidgetAlignment.Center,
                VerticalAlignment = WidgetAlignment.Center,
                Margin = new Vector2(2f * Scale)
            };
            clothingSlot.Children.Add(blockIconWidget);

            ValueBarWidget healthBarWidget = new ValueBarWidget
            {
                Name = name,
                LayoutDirection = LayoutDirection.Vertical,
                HorizontalAlignment = WidgetAlignment.Near,
                VerticalAlignment = WidgetAlignment.Far,
                BarsCount = 3,
                FlipDirection = true,
                LitBarColor = new Color(32, 128, 0),
                UnlitBarColor = new Color(24, 24, 24, 64),
                BarSize = new Vector2(12f * Scale),
                BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/ProgressBar"),
                Margin = new Vector2(4f * Scale)
            };
            clothingSlot.Children.Add(healthBarWidget);

            return clothingSlot;
        }

        protected override void OnUnload()
        {
            if (m_clothingInfoWidget?.ParentWidget != null)
                m_clothingInfoWidget.ParentWidget.Children.Remove(m_clothingInfoWidget);

            m_clothingInfoWidget = null;
            m_clothingInfoPanel = null;
            m_componentClothing = null;
        }

        protected override void OnUpdate(float dt)
        {
            if (m_clothingInfoWidget == null || m_clothingInfoPanel == null)
                return;

            if (Math.Abs(Scale - m_lastScale) > 0.001f)
            {
                ApplyScale();
                m_lastScale = Scale;
            }

            if (m_clothingInfoPanel.Direction != m_clothingInfoSettings.LayoutDirection)
                m_clothingInfoPanel.Direction = m_clothingInfoSettings.LayoutDirection;

            UpdateSlot(ClothingSlot.Head, "Head");
            UpdateSlot(ClothingSlot.Torso, "Torso");
            UpdateSlot(ClothingSlot.Legs, "Legs");
            UpdateSlot(ClothingSlot.Feet, "Feet");

            m_clothingInfoWidget.IsVisible = m_clothingInfoSettings.Enable;

            WidgetUtils.SetAnchor(m_clothingInfoWidget, ComponentGui.ControlsContainerWidget, m_clothingInfoSettings.Anchor, m_clothingInfoSettings.MarginX, m_clothingInfoSettings.MarginY);
        }

        public void ApplyScale()
        {
            m_clothingInfoPanel.Margin = new Vector2(-2f * Scale);

            foreach (AutoSizeCanvasWidget slot in m_clothingInfoPanel.Children)
            {
                slot.Margin = new Vector2(2f * Scale);

                BlockIconWidget icon = slot.Children.Find<BlockIconWidget>(slot.Name, true);
                ValueBarWidget bar = slot.Children.Find<ValueBarWidget>(slot.Name, true);
                RectangleWidget background = slot.Children.Find<RectangleWidget>(null, true);

                if (icon != null)
                {
                    icon.Size = new Vector2(72f * Scale);
                    icon.Margin = new Vector2(2f * Scale);
                }

                if (bar != null)
                {
                    bar.BarSize = new Vector2(12f * Scale);
                    bar.Margin = new Vector2(4f * Scale);
                }

                if (background != null)
                    background.OutlineThickness = 2f * Scale;
            }
        }

        public void UpdateSlot(ClothingSlot slot, string widgetName)
        {
            AutoSizeCanvasWidget slotWidget = m_clothingInfoPanel.Children.Find<AutoSizeCanvasWidget>(widgetName, true);

            if (slotWidget == null)
                return;

            BlockIconWidget icon = slotWidget.Children.Find<BlockIconWidget>(widgetName, true);
            ValueBarWidget bar = slotWidget.Children.Find<ValueBarWidget>(widgetName, true);

            int value = GetTopClothing(slot);

            if (value == 0)
            {
                bool hide = m_clothingInfoSettings.HideEmptyClothingSlots;

                slotWidget.IsVisible = !hide;

                icon.Value = 0;
                icon.IsVisible = !hide;

                bar.Value = 0f;
                bar.IsVisible = !hide;
                return;
            }

            int contents = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[contents];

            float percent = block.GetBlockHealth(value);
            slotWidget.IsVisible = true;

            icon.Value = value;
            icon.IsVisible = true;

            bar.Value = percent;
            bar.IsVisible = true;
        }

        public int GetTopClothing(ClothingSlot slot)
        {
            var clothes = m_componentClothing.GetClothes(slot);

            if (clothes.Count == 0)
                return 0;

            return clothes[clothes.Count - 1];
        }
    }
}
