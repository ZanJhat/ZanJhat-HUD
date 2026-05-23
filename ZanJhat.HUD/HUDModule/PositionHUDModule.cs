using Engine;
using Game;
using System;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class PositionHUDModule : HUDModule
    {
        public PositionLabelSettings m_positionLabelSettings;

        public AutoSizeCanvasWidget m_positionWidget;
        public LabelWidget m_positionLabel;

        protected override void OnLoad()
        {
            m_positionLabelSettings = HUDSettingsManager.PositionLabelSettings;

            m_positionWidget = ComponentGui.ControlsContainerWidget.Children.Find<AutoSizeCanvasWidget>("PositionWidget", false);
            m_positionLabel = ComponentGui.ControlsContainerWidget.Children.Find<LabelWidget>("PositionLabel", false);

            if (m_positionWidget == null || m_positionLabel == null)
            {
                m_positionWidget = new AutoSizeCanvasWidget
                {
                    Name = "PositionWidget"
                };

                RectangleWidget background = new RectangleWidget
                {
                    FillColor = new Color(0, 0, 0, 128),
                    OutlineColor = Color.Transparent
                };
                m_positionWidget.Children.Add(background);

                m_positionLabel = WidgetUtils.AddLabel(m_positionWidget, "PositionLabel", "", Color.White, m_positionLabelSettings.FontScale, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Center);

                WidgetUtils.DisableHitTestRecursive(m_positionWidget);
                ComponentGui.ControlsContainerWidget.Children.Insert(0, m_positionWidget);
            }
        }

        protected override void OnUnload()
        {
            if (m_positionWidget?.ParentWidget != null)
            {
                m_positionWidget.ParentWidget.Children.Remove(m_positionWidget);
            }

            m_positionWidget = null;
            m_positionLabel = null;
        }

        protected override void OnUpdate(float dt)
        {
            if (m_positionWidget == null || m_positionLabel == null)
                return;

            Vector3 position = ComponentBody.Position;

            m_positionLabel.Text = $"Position: {position.X:0.0}, {position.Y:0.0}, {position.Z:0.0}";
            m_positionLabel.FontScale = m_positionLabelSettings.FontScale;
            m_positionWidget.IsVisible = m_positionLabelSettings.Enable;

            WidgetUtils.SetAnchor(m_positionWidget, ComponentGui.ControlsContainerWidget, m_positionLabelSettings.Anchor, m_positionLabelSettings.MarginX, m_positionLabelSettings.MarginY);
        }
    }
}
