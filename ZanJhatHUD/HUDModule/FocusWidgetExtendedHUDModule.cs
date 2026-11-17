using Engine;
using Game;
using System;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class FocusWidgetExtendedHUDModule : HUDModule
    {
        public AutoSizeCanvasWidget m_buttonsCanvas;
        public StackPanelWidget m_buttonsPanel;
        public BevelledButtonWidget m_recipeButton;
        public BevelledButtonWidget m_usageButton;

        protected override void OnLoad()
        {
            m_buttonsCanvas = ComponentGui.ControlsContainerWidget.Children.Find<AutoSizeCanvasWidget>("ButtonsCanvas", false);
            m_buttonsPanel = ComponentGui.ControlsContainerWidget.Children.Find<StackPanelWidget>("ButtonsPanel", false);

            if (m_buttonsCanvas == null || m_buttonsPanel == null)
            {
                m_buttonsCanvas = new AutoSizeCanvasWidget
                {
                    Name = "ButtonsCanvas"
                };

                m_buttonsPanel = new StackPanelWidget
                {
                    Name = "ButtonsPanel",
                    Direction = LayoutDirection.Vertical
                };
                m_buttonsCanvas.Children.Add(m_buttonsPanel);

                m_recipeButton = CreateButton("Recipe", "Recipe");
                m_buttonsPanel.Children.Add(m_recipeButton);

                m_usageButton = CreateButton("Usage", "Usage");
                m_buttonsPanel.Children.Add(m_usageButton);

                ComponentGui.ControlsContainerWidget.Children.Add(m_buttonsCanvas);
            }
            else
            {
                m_recipeButton = m_buttonsPanel.Children.Find<BevelledButtonWidget>("Recipe", true);
                m_usageButton = m_buttonsPanel.Children.Find<BevelledButtonWidget>("Usage", true);
            }
        }

        public BevelledButtonWidget CreateButton(string name, string text)
        {
            BevelledButtonWidget button = new BevelledButtonWidget
            {
                Name = name,
                Text = text,
                Size = new Vector2(120f, 50f),
                FontScale = 0.75f,
            };
            return button;
        }

        protected override void OnUnload()
        {
            if (m_buttonsCanvas?.ParentWidget != null)
            {
                m_buttonsCanvas.ParentWidget.Children.Remove(m_buttonsCanvas);
            }

            m_recipeButton = null;
            m_usageButton = null;
            m_buttonsPanel = null;
            m_buttonsCanvas = null;
        }

        // Hỗ trợ [R], [U] cho Touch
        protected override void OnUpdate(float dt)
        {
            if (GameWidget == null || m_buttonsPanel == null || m_recipeButton == null || m_usageButton == null)
                return;

            int value = 0;
            int count = 0;

            if (ComponentInput != null)
            {
                // Lấy thông tin Split
                IInventory splitInventory = ComponentInput.SplitSourceInventory;
                int splitSlotIndex = ComponentInput.SplitSourceSlotIndex;

                // Kiểm tra xem có đang ở chế độ Split không
                if (splitInventory != null && splitSlotIndex != -1)
                {
                    value = splitInventory.GetSlotValue(splitSlotIndex);
                    count = splitInventory.GetSlotCount(splitSlotIndex);
                }
            }

            bool hasCursor = HUDManager.TryGetCursorPosition(GameWidget.Input, out _, out _);
            bool isTouch = !hasCursor;

            // Xem công thức
            bool hasRecipe = count > 0 && RecipeHelper.HasRecipe(value);
            m_recipeButton.IsVisible = hasRecipe && isTouch;

            if (m_recipeButton.IsClicked && hasRecipe)
                RecipeHelper.OpenRecipeScreen(value);

            // Xem công dụng
            bool isUsedAsIngredient = count > 0 && RecipeHelper.IsUsedAsIngredient(value);
            m_usageButton.IsVisible = isUsedAsIngredient && isTouch;

            if (m_usageButton.IsClicked && isUsedAsIngredient)
                RecipeHelper.OpenIngredientUsageScreen(value);

            if (m_recipeButton.IsVisible || m_usageButton.IsVisible)
                WidgetUtils.SetAnchor(m_buttonsCanvas, ComponentGui.ControlsContainerWidget, Anchor.RightCenter, 72f, 16f);
        }
    }
}
