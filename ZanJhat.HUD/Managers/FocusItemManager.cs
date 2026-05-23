using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Globalization;
using Engine.Input;
using Engine;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    // Delegate cho phép các mod khác can thiệp vào danh sách infos
    public delegate void BuildBlockInfoEventHandler(int value, Block block, List<StackPanelWidget> infos);

    public static class FocusItemManager
    {
        private static ConditionalWeakTable<Widget, FocusedItem> m_focusItems = new();

        public static void SetFocusedItem(Widget widget, FocusedItem item)
        {
            m_focusItems.AddOrUpdate(widget, item);
        }

        public static FocusedItem GetFocusedItem(Widget widget)
        {
            if (widget == null)
                return null;

            m_focusItems.TryGetValue(widget, out FocusedItem item);
            return item;
        }

        public static void RemoveFocusedItem(Widget widget)
        {
            m_focusItems.Remove(widget);
        }

        public static FocusedItem CreateInventoryItemFocusedItem(Widget ownerWidget, string name, IInventory inventory, int slotIndex)
        {
            bool logged = false;
            bool lastShiftState = false; // Theo dõi phím Shift

            return new FocusedItem()
            {
                Name = name,
                Owner = ownerWidget,

                OnFocusEnter = item =>
                {
                    logged = false;

                    if (CoreSettingsManager.CoreSettings.DevMode)
                        Log.Information($"{name} Enter: " + item.Owner);
                },

                OnFocusUpdate = item =>
                {
                    if (!logged && CoreSettingsManager.CoreSettings.DevMode)
                    {
                        logged = true;
                        Log.Information($"{name} Update: " + item.Owner);
                    }

                    // Phím Shift - Đánh dấu Dirty để đổi giao diện mô tả
                    bool currentShiftState = Keyboard.IsKeyDown(Key.Shift);
                    if (currentShiftState != lastShiftState)
                    {
                        lastShiftState = currentShiftState;
                        item.IsDirty = true; // Đánh dấu cần vẽ lại UI
                    }

                    int count = inventory != null ? inventory.GetSlotCount(slotIndex) : -1;

                    // Phím R - Xem công thức
                    if (Keyboard.IsKeyDownOnce(Key.R) && count > 0)
                    {
                        int value = inventory.GetSlotValue(slotIndex);
                        if (RecipeHelper.HasRecipe(value))
                            RecipeHelper.OpenRecipeScreen(value);
                    }

                    // Phím U - Xem công dụng
                    if (Keyboard.IsKeyDownOnce(Key.U) && count > 0)
                    {
                        int value = inventory.GetSlotValue(slotIndex);
                        if (RecipeHelper.IsUsedAsIngredient(value))
                            RecipeHelper.OpenIngredientUsageScreen(value);
                    }
                },

                OnFocusLost = item =>
                {
                    logged = false;

                    if (CoreSettingsManager.CoreSettings.DevMode)
                        Log.Information($"{name} Lost: " + item.Owner);
                },

                OnBuildFocusContent = (item, container, inputType) =>
                {
                    if (CoreSettingsManager.CoreSettings.DevMode)
                        Log.Information($"{name} Build: " + item.Owner);

                    if (ownerWidget is InventorySlotWidget inventorySlotWidget)
                    {
                        inventory = inventorySlotWidget.m_inventory;
                        slotIndex = inventorySlotWidget.m_slotIndex;
                    }

                    if (inventory != null && inventory.GetSlotCount(slotIndex) > 0)
                    {
                        int value = inventory.GetSlotValue(slotIndex);
                        BuildBlockFocusContent(container, value, inputType);
                    }
                }
            };
        }

        // Hook
        public static event BuildBlockInfoEventHandler OnBuildBlockInfo;

        public static void BuildBlockFocusContent(AutoSizeCanvasWidget contentWidget, int value, FocusInputType inputType)
        {
            int contents = Terrain.ExtractContents(value);
            int data = Terrain.ExtractData(value);
            Block block = BlocksManager.Blocks[contents];

            StackPanelWidget stackPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Vertical,
                Margin = new Vector2(8f)
            };
            contentWidget.Children.Add(stackPanel);

            WidgetUtils.AddLabel(stackPanel, block.GetDisplayName(null, value), Color.White, 0.75f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            if (CoreSettingsManager.CoreSettings.DevMode)
            {
                WidgetUtils.AddLabel(stackPanel, $"Type: {block.GetType().Name} Value: {value}  Contents: {contents}  Data: {data}", Color.Yellow, 0.5f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
            }

            List<StackPanelWidget> infos = new();

            bool showDurability = false;

            float meleePower = block.GetMeleePower(value);
            if (meleePower > 1f)
            {
                infos.Add(CreateBlockInfo(HUDIconPaths.Sword, "Melee Power", meleePower.ToString("0.##")));
                infos.Add(CreateBlockInfo(HUDIconPaths.Probability, "Melee Hit Ratio", $"{100f * block.GetMeleeHitProbability(value):0}%"));
                showDurability = true;
            }

            float projectilePower = block.GetProjectilePower(value);
            if (projectilePower > 1f)
            {
                infos.Add(CreateBlockInfo(HUDIconPaths.Projectile, "Projectile Power", projectilePower.ToString("0.##")));
                showDurability = true;
            }

            float shovelPower = block.GetShovelPower(value);
            if (shovelPower > 1f)
            {
                infos.Add(CreateBlockInfo(HUDIconPaths.Shovel, "Shovel Power", shovelPower.ToString("0.##")));
                showDurability = true;
            }

            float hackPower = block.GetHackPower(value);
            if (hackPower > 1f)
            {
                infos.Add(CreateBlockInfo(HUDIconPaths.Axe, "Hack Power", hackPower.ToString("0.##")));
                showDurability = true;
            }

            float quarryPower = block.GetQuarryPower(value);
            if (quarryPower > 1f)
            {
                infos.Add(CreateBlockInfo(HUDIconPaths.Pickaxe, "Quarry Power", quarryPower.ToString("0.##")));
                showDurability = true;
            }

            float damage = block.GetDamage(value);
            int durability = block.GetDurability(value);
            float blockHealth = block.GetBlockHealth(value);
            if ((showDurability && durability > 0) || block.CanWear(value) || blockHealth >= 0f)
            {
                infos.Add(CreateBlockInfo("Textures/Atlas/HealthBar", "Durability", $"{durability - damage}/{durability}", new Color(224, 24, 0)));
            }

            int emittedLightAmount = block.GetEmittedLightAmount(value);
            if (emittedLightAmount > 0)
                infos.Add(CreateBlockInfo(HUDIconPaths.LightBulb, "Luminosity", emittedLightAmount.ToString()));

            float fuelFireDuration = block.GetFuelFireDuration(value);
            if (fuelFireDuration > 0f)
                infos.Add(CreateBlockInfo(HUDIconPaths.Fuel, "Fuel", fuelFireDuration.ToString()));

            infos.Add(CreateBlockInfo(HUDIconPaths.Layer, "Max Stacking", block.GetMaxStacking(value).ToString()));

            float fireDuration = block.GetFireDuration(value);
            if (fireDuration > 0f)
                infos.Add(CreateBlockInfo(HUDIconPaths.Fire, "Fire Duration", fireDuration.ToString()));

            float nutritionalValue = block.GetNutritionalValue(value);
            if (nutritionalValue > 0f)
                infos.Add(CreateBlockInfo("Textures/Atlas/FoodBar", "Nutrition", (nutritionalValue * 10f).ToString("F1", CultureInfo.InvariantCulture), new Color(150, 170, 190)));

            int rotPeriod = block.GetRotPeriod(value);
            if (rotPeriod > 0)
                infos.Add(CreateBlockInfo(HUDIconPaths.Ice, "Max Storage Time", $"{2 * rotPeriod * 60f / 1200f:0.0} days"));

            BlockDigMethod blockDigMethod = block.GetBlockDigMethod(value);
            if (blockDigMethod != BlockDigMethod.None)
            {
                string iconPath =
                    blockDigMethod == BlockDigMethod.Quarry ? HUDIconPaths.Pickaxe :
                    blockDigMethod == BlockDigMethod.Hack ? HUDIconPaths.Axe :
                    blockDigMethod == BlockDigMethod.Shovel ? HUDIconPaths.Shovel :
                    "Textures/Gui/Unavailable";
                Color colorDigMethod = blockDigMethod == BlockDigMethod.None ? Color.Red : Color.White;
                infos.Add(CreateBlockInfo(iconPath, "Dig Method", blockDigMethod.ToString(), colorDigMethod));
                infos.Add(CreateBlockInfo(HUDIconPaths.DigResilience, "Dig Resilience", block.GetDigResilience(value).ToString(CultureInfo.InvariantCulture)));
            }

            float explosionResilience = block.GetExplosionResilience(value);
            if (explosionResilience > 0f)
                infos.Add(CreateBlockInfo(HUDIconPaths.ExplosionResilience, "Explosion Resilience", explosionResilience.ToString(CultureInfo.InvariantCulture)));

            float explosionPressure = block.GetExplosionPressure(value);
            if (explosionPressure > 0f)
                infos.Add(CreateBlockInfo(HUDIconPaths.Explosion, "Explosion Power", explosionPressure.ToString(CultureInfo.InvariantCulture)));

            float experienceCount = block.DefaultExperienceCount;
            if (experienceCount > 0f)
                infos.Add(CreateBlockInfo("Textures/Experience", "Experience Orbs", experienceCount.ToString(CultureInfo.InvariantCulture)));

            if (block.CanWear(value))
            {
                ClothingData clothingData = block.GetClothingData(value);
                infos.Add(CreateBlockInfo(HUDIconPaths.PaintBrush, "Can Be Dyed", clothingData.CanBeDyed ? LanguageControl.Yes : LanguageControl.No));
                infos.Add(CreateBlockInfo(HUDIconPaths.Armor, "Armor Protection", $"{(int)(clothingData.ArmorProtection * 100f)}% "));
                infos.Add(CreateBlockInfo(HUDIconPaths.Absorption, "Absorption", clothingData.Sturdiness.ToString(CultureInfo.InvariantCulture)));
                infos.Add(CreateBlockInfo(HUDIconPaths.Insulation, "Insulation", $"{clothingData.Insulation:0.0} clo"));
                infos.Add(CreateBlockInfo(HUDIconPaths.MovementSpeed, "Movement Speed", $"{clothingData.MovementSpeedFactor * 100f:0}% "));
            }

            // Hook cho Mod khác chèn thông tin
            OnBuildBlockInfo?.Invoke(value, block, infos);
            BuildBlockInfos(infos, stackPanel);

            // --- Extra ---

            bool isTouchInput = inputType == FocusInputType.Touch;
            bool isShiftHeld = Keyboard.IsKeyDown(Key.Shift);

            // Hiển thị mô tả nếu Setting bật sẵn (only Touch), HOẶC nếu người chơi đang giữ Shift
            if ((HUDSettingsManager.FocusWidgetSettings.ShowDescription && isTouchInput) || isShiftHeld)
                WidgetUtils.AddLabel(stackPanel, block.GetDescription(value), Color.LightGray, 0.5f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
            else if (!isTouchInput) // Nếu chưa giữ Shift, hiển thị gợi ý
                WidgetUtils.AddLabel(stackPanel, "Hold [Shift] to view the block description", Color.LightGray, 0.5f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            // Hiện gợi ý nếu không phải Touch và có công thức chế tạo
            if (inputType != FocusInputType.Touch && RecipeHelper.HasRecipe(value))
                WidgetUtils.AddLabel(stackPanel, "Press [R] to view crafting recipes for this block", Color.LightGray, 0.5f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            // Hiện gợi ý nếu không phải Touch và có công thức sử dụng nguyên liệu nayf
            if (inputType != FocusInputType.Touch && RecipeHelper.IsUsedAsIngredient(value))
                WidgetUtils.AddLabel(stackPanel, "Press [U] to view recipes using this ingredient", Color.LightGray, 0.5f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            // ------

            string packageName = ModEntityHelper.GetPackageName(block);

            if (!string.IsNullOrEmpty(packageName) && HUDSettingsManager.FocusWidgetSettings.ShowPackageName)
                WidgetUtils.AddLabel(stackPanel, packageName, ColorPalette.AccentBlue, 0.75f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
        }

        public static void BuildBlockInfos(List<StackPanelWidget> infos, StackPanelWidget parent)
        {
            if (infos == null || infos.Count == 0 || parent == null)
                return;

            int maxChildPerRow = HUDSettingsManager.FocusWidgetSettings.ShowDetail ? 3 : 6;

            StackPanelWidget currentRow = null;
            int childCount = 0;

            foreach (StackPanelWidget info in infos)
            {
                // Tạo row mới nếu chưa có hoặc đã đủ 5 child
                if (currentRow == null || childCount >= maxChildPerRow)
                {
                    currentRow = new StackPanelWidget
                    {
                        Direction = LayoutDirection.Horizontal,
                        VerticalAlignment = WidgetAlignment.Center,
                        HorizontalAlignment = WidgetAlignment.Near,
                        Margin = new Vector2(2f)
                    };

                    parent.Children.Add(currentRow);
                    childCount = 0;
                }

                if (childCount > 0 && childCount < maxChildPerRow)
                {
                    CanvasWidget canvas = new CanvasWidget
                    {
                        Size = new Vector2(10f, 0f)
                    };
                    currentRow.Children.Add(canvas);
                }

                currentRow.Children.Add(info);
                childCount++;
            }
        }

        public static StackPanelWidget CreateBlockInfo(string iconPath, string name, string value) => CreateBlockInfo(iconPath, name, value, Color.White);

        public static StackPanelWidget CreateBlockInfo(string iconPath, string name, string value, Color color)
        {
            string text = HUDSettingsManager.FocusWidgetSettings.ShowDetail ? $"{name}: {value}" : value;
            StackPanelWidget stackPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Horizontal,
                VerticalAlignment = WidgetAlignment.Center,
                HorizontalAlignment = WidgetAlignment.Near,
                Margin = new Vector2(0f)
            };

            // Truyền tham số false ở cuối để ContentManager trả về null nếu không tìm thấy file
            Subtexture iconSubtexture = ContentManager.Get<Subtexture>(iconPath, null, false);

            if (iconSubtexture == null)
            {
                Log.Warning($"[ZanJhat HUD] Missing texture: '{iconPath}' (used for: {name}). Using fallback icon.");
                iconSubtexture = ContentManager.Get<Subtexture>("Textures/Gui/Unavailable", null, false);
            }

            RectangleWidget rectangle = new RectangleWidget
            {
                Size = new Vector2(16f),
                Subtexture = iconSubtexture,
                OutlineColor = new Color(0, 0, 0, 0),
                FillColor = color
            };
            stackPanel.Children.Add(rectangle);

            CanvasWidget canvas = new CanvasWidget
            {
                Size = new Vector2(5f, 0f)
            };
            stackPanel.Children.Add(canvas);

            LabelWidget label = new LabelWidget
            {
                FontScale = 0.5f,
                Color = Color.White,
                VerticalAlignment = WidgetAlignment.Center,
                WordWrap = false,
                Text = text,
                Margin = new Vector2(0f)
            };
            stackPanel.Children.Add(label);

            return stackPanel;
        }
    }
}
