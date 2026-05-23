using Engine;
using Game;
using System;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class FocusWidgetHUDModule : HUDModule
    {
        public Widget m_hoveredWidget;
        public FocusedItem m_hoveredItem;

        public AutoSizeCanvasWidget m_focusContentWidget;

        public DragHostWidget DragHostWidget => ComponentPlayer.DragHostWidget;

        protected override void OnLoad()
        {
        }

        protected override void OnUnload()
        {
            HUDManager.ClearCurrentHover(ref m_hoveredItem, ref m_hoveredWidget, m_focusContentWidget);

            if (m_focusContentWidget != null && m_focusContentWidget.ParentWidget != null)
            {
                m_focusContentWidget.ParentWidget.Children.Remove(m_focusContentWidget);
                m_focusContentWidget = null;
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (GameWidget == null)
                return;

            HUDManager.ProcessFocusUpdate(GameWidget, GameWidget.Input, DragHostWidget, ref m_hoveredItem, ref m_hoveredWidget, ref m_focusContentWidget);
        }
    }
}
