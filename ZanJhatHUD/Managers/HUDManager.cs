using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;
using Engine.Input;
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
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public static class HUDManager
    {
        public static Widget HoveredWidget;
        public static FocusedItem HoveredItem;

        public static AutoSizeCanvasWidget FocusContentWidget;

        // Cho phép mod khác chèn thêm UI vào đáy Tooltip
        public static event Action<FocusedItem, AutoSizeCanvasWidget, FocusInputType> OnGlobalFocusContentBuilt;

        public static void Initialize()
        {
            Window.Frame += Update;
        }

        public static void Update()
        {
            if (ScreensManager.CurrentScreen == null)
                return;

            if (ScreensManager.GetScreenName(ScreensManager.CurrentScreen) == "Game")
            {
                ClearCurrentHover(ref HoveredItem, ref HoveredWidget, FocusContentWidget);
                return;
            }

            ContainerWidget root = ScreensManager.RootWidget;
            if (root == null)
                return;

            DragHostWidget dragHost = GetDragHostWidget(root);

            ProcessFocusUpdate(root, root.Input, dragHost, ref HoveredItem, ref HoveredWidget, ref FocusContentWidget);
        }

        public static void ProcessFocusUpdate(ContainerWidget root, WidgetInput input, DragHostWidget dragHost, ref FocusedItem hoveredItem, ref Widget hoveredWidget, ref AutoSizeCanvasWidget focusContentWidget)
        {
            if (root == null)
                return;

            bool isDragging = dragHost != null && dragHost.IsDragInProgress;
            Widget dragWidget = isDragging ? dragHost.m_dragWidget : null;

            bool hasCursor = TryGetCursorPosition(input, out FocusInputType inputType, out Vector2 screenPos);

            Widget targetWidget = null;

            if (hasCursor)
            {
                if (isDragging)
                {
                    ClearCurrentHover(ref hoveredItem, ref hoveredWidget, focusContentWidget);
                    return;
                }
                else
                {
                    targetWidget = GetHoveredWidget(root, screenPos);
                }
            }
            else
            {
                if (isDragging && dragWidget != null)
                {
                    inputType = FocusInputType.Touch;
                    targetWidget = dragWidget;
                    screenPos = dragHost.m_dragPosition;
                }
                else
                {
                    ClearCurrentHover(ref hoveredItem, ref hoveredWidget, focusContentWidget);
                    return;
                }
            }

            FocusedItem targetItem = targetWidget != null ? FocusItemManager.GetFocusedItem(targetWidget) : null;
            bool needsRebuild = false;

            // KỊCH BẢN 1: Chuột trỏ sang một widget/item hoàn toàn mới
            if (targetWidget != hoveredWidget || targetItem != hoveredItem)
            {
                OnHoverLost(hoveredItem);
                hoveredWidget = targetWidget;
                hoveredItem = targetItem;
                OnHoverEnter(hoveredItem);

                needsRebuild = true;
            }
            // KỊCH BẢN 2: Vẫn đang trỏ vào item cũ, ta chạy hàm Update
            else
            {
                OnHoverUpdate(targetItem);

                // Sau khi Update, nếu item báo cáo trạng thái thay đổi (ví dụ: ấn Shift)
                if (hoveredItem != null && hoveredItem.IsDirty)
                {
                    needsRebuild = true;
                    hoveredItem.IsDirty = false; // Reset lại cờ sau khi đã ghi nhận
                }
            }

            if (needsRebuild)
            {
                ClearFocusContent(focusContentWidget, true);
                BuildFocusContent(hoveredItem, root, ref focusContentWidget, inputType);

                if (focusContentWidget != null && focusContentWidget.IsVisible)
                {
                    Vector2 infiniteSize = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    focusContentWidget.Measure(infiniteSize);
                    focusContentWidget.Arrange(Vector2.Zero, focusContentWidget.DesiredSize);
                }
            }

            Vector2 widgetPos = root.ScreenToWidget(screenPos);
            UpdateFocusPosition(focusContentWidget, root, widgetPos);
        }


        public static void ClearCurrentHover(ref FocusedItem item, ref Widget hovered, AutoSizeCanvasWidget focusContent)
        {
            if (item != null)
            {
                OnHoverLost(item);
                item = null;
            }
            hovered = null;
            ClearFocusContent(focusContent, true);
        }

        public static DragHostWidget GetDragHostWidget(ContainerWidget parent)
        {
            return parent.Children.Find<DragHostWidget>(false) ?? parent.AllChildren.OfType<DragHostWidget>().FirstOrDefault();
        }

        public static bool TryGetCursorPosition(WidgetInput input, out FocusInputType inputType, out Vector2 pos)
        {
            inputType = FocusInputType.None;
            pos = Vector2.Zero; // Toạ độ màn hình

            // Vr
            if (input.IsVrCursorVisible && input.VrCursorPosition.HasValue)
            {
                inputType = FocusInputType.Vr;
                pos = input.VrCursorPosition.Value;
                return true;
            }

            // Mouse
            Vector2? mousePos = input.MousePosition;
            if (input.IsMouseCursorVisible && mousePos.HasValue)
            {
                inputType = FocusInputType.Mouse;
                pos = mousePos.Value;
                return true;
            }

            // Gamepad
            if (input.IsPadCursorVisible)
            {
                inputType = FocusInputType.Gamepad;
                pos = input.PadCursorPosition;
                return true;
            }

            return false;
        }

        public static Widget GetHoveredWidget(ContainerWidget parent, Vector2 pos)
        {
            if (parent == null)
                return null;

            Widget current = parent.HitTestGlobal(pos);

            while (current != null)
            {
                if (FocusItemManager.GetFocusedItem(current) != null)
                    return current;

                current = current.ParentWidget;
            }

            return null;
        }

        public static void OnHoverEnter(FocusedItem item)
        {
            item?.OnFocusEnter?.Invoke(item);
        }

        public static void OnHoverUpdate(FocusedItem item)
        {
            item?.OnFocusUpdate?.Invoke(item);
        }

        public static void OnHoverLost(FocusedItem item)
        {
            item?.OnFocusLost?.Invoke(item);
        }

        public static void BuildFocusContent(FocusedItem item, ContainerWidget parent, ref AutoSizeCanvasWidget focusContent, FocusInputType inputType)
        {
            if (parent == null)
                return;

            if (focusContent == null)
                focusContent = CreateFocusContentContainer(parent);

            if (item == null || item.OnBuildFocusContent == null)
            {
                ClearFocusContent(focusContent, true);
                return;
            }

            // Kiểm tra xem nó đã ở cuối danh sách chưa, nếu chưa thì Move lên
            if (parent.Children.IndexOf(focusContent) != parent.Children.Count - 1)
            {
                parent.Children.Remove(focusContent);
                parent.Children.Add(focusContent);
            }

            ClearFocusContent(focusContent, false);

            item.OnBuildFocusContent.Invoke(item, focusContent, inputType);

            // Cho phép chèn thêm UI toàn cục
            OnGlobalFocusContentBuilt?.Invoke(item, focusContent, inputType);

            // Vô hiệu hóa, bỏ qua khi bị click
            WidgetUtils.DisableHitTestRecursive(focusContent);

            // KIỂM TRA SỐ LƯỢNG CHILDREN SAU KHI BUILD
            // Mặc định focusContent có 2 con là Background (0) và Border (1)
            if (focusContent.Children.Count > 2)
                focusContent.IsVisible = true;
            else
                focusContent.IsVisible = false;
        }

        public static AutoSizeCanvasWidget CreateFocusContentContainer(ContainerWidget parent)
        {
            if (parent == null)
                return null;

            AutoSizeCanvasWidget focusContent = new AutoSizeCanvasWidget
            {
                MaxWidth = 500f
            };

            RectangleWidget background = new RectangleWidget
            {
                FillColor = new Color(0, 0, 0, 191),
                OutlineColor = Color.Transparent
            };
            focusContent.Children.Add(background);

            RainbowRectangleWidget border = new RainbowRectangleWidget
            {
                FillColor = Color.Transparent,
                OutlineColor = Color.White,
                OutlineThickness = 2f,
                Margin = new Vector2(3f)
            };
            focusContent.Children.Add(border);

            parent.Children.Add(focusContent);
            return focusContent;
        }

        public static bool UpdateFocusPosition(AutoSizeCanvasWidget focusContent, ContainerWidget parent, Vector2 pos)
        {
            if (focusContent == null || !focusContent.IsVisible || parent == null)
                return false;

            Vector2 offset = new Vector2(24f, 8f);
            Vector2 size = focusContent.ActualSize;

            // X: ưu tiên bên trái
            if (pos.X - offset.X - size.X >= 0f)
            {
                pos.X -= offset.X + size.X;
            }
            else
            {
                pos.X += offset.X;
            }

            // Y: ưu tiên bên dưới
            pos.Y += offset.Y;

            // Clamp X (trái + phải)
            pos.X = MathUtils.Clamp(pos.X, 0f, parent.ActualSize.X - size.X);

            // Clamp Y (trên + dưới)
            pos.Y = MathUtils.Clamp(pos.Y, 0f, parent.ActualSize.Y - size.Y);

            focusContent.LayoutTransform = Matrix.CreateTranslation(pos.X, pos.Y, 0f);
            return true;
        }

        public static bool ClearFocusContent(AutoSizeCanvasWidget focusContent, bool hideWidget = false)
        {
            if (focusContent == null)
                return false;

            // Xóa các Widget con được thêm vào, giữ lại index 0 (Background) và index 1 (Border)
            for (int i = focusContent.Children.Count - 1; i >= 2; i--)
                focusContent.Children.RemoveAt(i);

            if (hideWidget)
                focusContent.IsVisible = false;

            return true;
        }
    }
}
