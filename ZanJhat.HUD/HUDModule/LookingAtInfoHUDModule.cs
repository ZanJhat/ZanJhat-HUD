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
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public enum LookTargetType
    {
        None = 0,
        Player = 1,
        Creature = 2,
        Entity = 3,
        MovingBlock = 4,
        Block = 5
    }

    public class LookTargetInfo
    {
        public LookTargetType LookTargetType;
        public ComponentPlayer ComponentPlayer;
        public ComponentCreature ComponentCreature;
        public ComponentBody ComponentBody;
        public int BlockValue;
        public MovingBlock MovingBlock;
        public CellFace CellFace;
        public float DigProgress;
    }

    public delegate void BuildLookingAtEntityInfoEventHandler(LookingAtInfoHUDModule module, LookTargetInfo targetInfo, StackPanelWidget stackPanel);
    public delegate void BuildLookingAtBlockInfoEventHandler(LookingAtInfoHUDModule module, LookTargetInfo targetInfo, Block block, int value, StackPanelWidget stackPanel);

    public class LookingAtInfoHUDModule : HUDModule
    {
        public readonly struct EntityInfoRow
        {
            public readonly string IconPath;
            public readonly string Text;
            public readonly Color IconColor;

            public EntityInfoRow(string iconPath, string text, Color iconColor)
            {
                IconPath = iconPath;
                Text = text;
                IconColor = iconColor;
            }
        }

        public AutoSizeCanvasWidget m_lookingAtInfo;

        public const float MaxDistance = 10f;

        public static event BuildLookingAtEntityInfoEventHandler OnBuildEntityInfo;
        public static event BuildLookingAtBlockInfoEventHandler OnBuildBlockInfo;

        protected override void OnLoad()
        {
            m_lookingAtInfo = ComponentGui.ControlsContainerWidget.Children.Find<AutoSizeCanvasWidget>("LookingAtInfo", false);

            if (m_lookingAtInfo == null)
                CreateLookingAtInfoContainer();
        }

        public void CreateLookingAtInfoContainer()
        {
            m_lookingAtInfo = new AutoSizeCanvasWidget
            {
                Name = "LookingAtInfo",
                MaxWidth = 500f
            };

            RectangleWidget background = new RectangleWidget
            {
                FillColor = new Color(0, 0, 0, 191),
                OutlineColor = Color.Transparent
            };
            m_lookingAtInfo.Children.Add(background);

            RainbowRectangleWidget border = new RainbowRectangleWidget
            {
                FillColor = Color.Transparent,
                OutlineColor = Color.White,
                OutlineThickness = 2f,
                Margin = new Vector2(3f)
            };
            m_lookingAtInfo.Children.Add(border);

            ComponentGui.ControlsContainerWidget.Children.Insert(1, m_lookingAtInfo);
        }

        protected override void OnUnload()
        {
            if (m_lookingAtInfo != null && m_lookingAtInfo.ParentWidget is ContainerWidget parent)
            {
                parent.Children.Remove(m_lookingAtInfo);
                m_lookingAtInfo = null;
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (HUDSettingsManager.LookingAtInfoSettings.Enable && ComponentPlayer.ComponentGui.ModalPanelWidget == null)
            {
                LookTargetInfo targetInfo = new();

                if (TryGetLookingAtInfo(targetInfo))
                {
                    BuildLookingAtInfo(targetInfo);
                    WidgetUtils.DisableHitTestRecursive(m_lookingAtInfo);
                    WidgetUtils.SetAnchor(m_lookingAtInfo, ComponentGui.ControlsContainerWidget.ActualSize, Anchor.TopCenter, 0f, 0f);
                }
                else
                {
                    ClearLookingAtInfo(true);
                }
            }
            else
            {
                ClearLookingAtInfo(true);
            }
        }

        public void BuildLookingAtInfo(LookTargetInfo targetInfo)
        {
            ClearLookingAtInfo();

            if (m_lookingAtInfo == null)
                return;

            m_lookingAtInfo.IsVisible = true;

            if (targetInfo.LookTargetType == LookTargetType.Player)
                BuildPlayerInfo(targetInfo);
            else if (targetInfo.LookTargetType == LookTargetType.Creature)
                BuildCreatureInfo(targetInfo);
            else if (targetInfo.LookTargetType == LookTargetType.Entity)
                BuildEntityInfo(targetInfo);
            else if (targetInfo.LookTargetType == LookTargetType.MovingBlock || targetInfo.LookTargetType == LookTargetType.Block)
                BuildBlockInfo(targetInfo);
            else
                ClearLookingAtInfo(true);
        }

        public void BuildPlayerInfo(LookTargetInfo targetInfo)
        {
            ComponentPlayer componentPlayer = targetInfo.ComponentPlayer;
            ComponentHealth componentHealth = componentPlayer?.ComponentHealth;

            if (componentPlayer != null && componentHealth != null)
            {
                PlayerData playerData = componentPlayer.PlayerData;
                string name = playerData.Name;

                float attackResilience = componentHealth.AttackResilience;
                float health = componentHealth.Health;
                string text = $"{(health * attackResilience):0.#} / {attackResilience:0.#}";

                List<EntityInfoRow> infoRows = new List<EntityInfoRow>
                {
                    new EntityInfoRow("Textures/Atlas/HealthBar", text, ColorPalette.HealthLitColor)
                };

                BuildCommonEntityInfo(targetInfo, name, infoRows, string.Empty);
            }
        }

        public void BuildCreatureInfo(LookTargetInfo targetInfo)
        {
            ComponentCreature componentCreature = targetInfo.ComponentCreature;
            ComponentHealth componentHealth = componentCreature?.ComponentHealth;

            if (componentCreature != null && componentHealth != null)
            {
                string name = componentCreature.DisplayName;

                float attackResilience = componentHealth.AttackResilience;
                float health = componentHealth.Health;
                string text = $"{(health * attackResilience):0.#} / {attackResilience:0.#}";

                List<EntityInfoRow> infoRows = new List<EntityInfoRow>
                {
                    new EntityInfoRow("Textures/Atlas/HealthBar", text, ColorPalette.HealthLitColor)
                };

                string packageName = ModEntityHelper.GetPackageName(componentCreature.Entity);

                ComponentBehaviorSelector componentBehaviorSelector = componentCreature.Entity.FindComponent<ComponentBehaviorSelector>();

                BuildCommonEntityInfo(targetInfo, name, infoRows, packageName, componentBehaviorSelector);
            }
        }

        public void BuildEntityInfo(LookTargetInfo targetInfo)
        {
            ComponentBody componentBody = targetInfo.ComponentBody;

            string name = string.Empty;
            List<EntityInfoRow> infoRows = new List<EntityInfoRow>();
            string packageName = string.Empty;

            if (componentBody != null)
            {
                name = componentBody.Entity.ValuesDictionary.DatabaseObject.Name;

                ComponentDamage componentDamage = componentBody.Entity.FindComponent<ComponentDamage>();
                if (componentDamage != null)
                {
                    float attackResilience = componentDamage.AttackResilience;
                    float hitpoints = componentDamage.Hitpoints;
                    string text = $"{(hitpoints * attackResilience):0.#} / {attackResilience:0.#}";
                    infoRows.Add(new EntityInfoRow("Textures/HUDIcons/Shield", text, Color.White));
                }

                packageName = ModEntityHelper.GetPackageName(componentBody.Entity);
            }

            BuildCommonEntityInfo(
targetInfo, name, infoRows, packageName);
        }

        public void BuildCommonEntityInfo(LookTargetInfo targetInfo, string name, IEnumerable<EntityInfoRow> infoRows, string packageName, ComponentBehaviorSelector componentBehaviorSelector = null)
        {
            StackPanelWidget stackPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Vertical,
                Margin = new Vector2(8f)
            };
            m_lookingAtInfo.Children.Add(stackPanel);

            WidgetUtils.AddLabel(stackPanel, name, Color.White, 0.85f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            if (CoreSettingsManager.CoreSettings.DevMode && componentBehaviorSelector != null)
            {
                ComponentBehavior selected = null;
                float max = 0f;

                foreach (ComponentBehavior componentBehavior in componentBehaviorSelector.m_behaviors)
                {
                    if (componentBehavior.ImportanceLevel > max)
                    {
                        max = componentBehavior.ImportanceLevel;
                        selected = componentBehavior;
                    }
                }

                if (selected != null)
                {
                    WidgetUtils.AddLabel(stackPanel, $"Behavior: {selected.GetType().Name}", Color.Yellow, 0.6f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                    WidgetUtils.AddLabel(stackPanel, $"Importance Level: {max}", Color.Yellow, 0.6f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
            }

            if (infoRows != null)
            {
                foreach (EntityInfoRow infoRow in infoRows)
                    stackPanel.Children.Add(CreateInfoRow(infoRow.IconPath, infoRow.Text, infoRow.IconColor));
            }

            // Cho phép Mod khác chèn thêm thông tin khác
            OnBuildEntityInfo?.Invoke(this, targetInfo, stackPanel);

            if (!string.IsNullOrEmpty(packageName) && HUDSettingsManager.LookingAtInfoSettings.ShowPackageName)
            {
                WidgetUtils.AddLabel(stackPanel, packageName, ColorPalette.AccentBlue, 0.85f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
            }
        }

        public static StackPanelWidget CreateInfoRow(string iconPath, string text, Color iconColor)
        {
            StackPanelWidget stackPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Horizontal,
                VerticalAlignment = WidgetAlignment.Center,
                HorizontalAlignment = WidgetAlignment.Near,
                Margin = new Vector2(2f)
            };

            RectangleWidget rectangle = new RectangleWidget
            {
                Size = new Vector2(16f),
                Subtexture = ContentManager.Get<Subtexture>(iconPath),
                OutlineColor = new Color(0, 0, 0, 0),
                FillColor = iconColor
            };
            stackPanel.Children.Add(rectangle);

            CanvasWidget canvas = new CanvasWidget
            {
                Size = new Vector2(5f, 0f)
            };
            stackPanel.Children.Add(canvas);

            LabelWidget label = new LabelWidget
            {
                FontScale = 0.6f,
                Color = Color.White,
                VerticalAlignment = WidgetAlignment.Center,
                WordWrap = false,
                Text = text,
                Margin = new Vector2(0f)
            };
            stackPanel.Children.Add(label);

            return stackPanel;
        }

        public void BuildBlockInfo(LookTargetInfo targetInfo)
        {
            int value = targetInfo.BlockValue;

            if (value <= 0)
                return;

            int contents = Terrain.ExtractContents(value);
            int data = Terrain.ExtractData(value);
            Block block = BlocksManager.Blocks[contents];

            StackPanelWidget blockInfoPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Vertical,
                Margin = new Vector2(8f)
            };
            m_lookingAtInfo.Children.Add(blockInfoPanel);

            StackPanelWidget blockHeaderPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Horizontal,
                Margin = new Vector2(0f)
            };
            blockInfoPanel.Children.Add(blockHeaderPanel);

            BlockIconWidget blockIcon = new BlockIconWidget
            {
                Value = value,
                Size = new Vector2(48f),
                Scale = 1f,
                Color = Color.White,
                Margin = new Vector2(2f)
            };
            blockHeaderPanel.Children.Add(blockIcon);

            StackPanelWidget blockDetailsPanel = new StackPanelWidget
            {
                Direction = LayoutDirection.Vertical,
                Margin = new Vector2(0f)
            };
            blockHeaderPanel.Children.Add(blockDetailsPanel);

            AddBlockDetails(targetInfo, blockDetailsPanel, value, contents, data, block);

            AddBlockDigMethod(targetInfo, blockHeaderPanel, value, contents, data, block);

            AddBlockDigProgress(targetInfo, blockInfoPanel, value, contents, data, block);
        }

        public void AddBlockDetails(LookTargetInfo targetInfo, StackPanelWidget stackPanel, int value, int contents, int data, Block block)
        {
            int x;
            int y;
            int z;
            ComponentBlockEntity componentBlockEntity;

            if (targetInfo.LookTargetType == LookTargetType.MovingBlock)
            {
                MovingBlock movingBlock = targetInfo.MovingBlock;

                x = Terrain.ToCell(movingBlock.Position.X);
                y = Terrain.ToCell(movingBlock.Position.Y);
                z = Terrain.ToCell(movingBlock.Position.Z);
                componentBlockEntity = SubsystemBlockEntities.GetBlockEntity(movingBlock);
            }
            else
            {
                CellFace cellFace = targetInfo.CellFace;

                x = cellFace.X;
                y = cellFace.Y;
                z = cellFace.Z;
                componentBlockEntity = SubsystemBlockEntities.GetBlockEntity(x, y, z);
            }

            Point3 point = new Point3(x, y, z);

            WidgetUtils.AddLabel(stackPanel, block.GetDisplayName(null, value), Color.White, 0.85f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

            if (CoreSettingsManager.CoreSettings.DevMode)
            {
                WidgetUtils.AddLabel(stackPanel, $"Type: {block.GetType().Name} Value: {value}", Color.Yellow, 0.6f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                WidgetUtils.AddLabel(stackPanel, $"Contents: {contents}  Data: {data}", Color.Yellow, 0.6f, true, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
            }

            if (HUDSettingsManager.LookingAtInfoSettings.ShowDetail)
            {
                if (block is SoilBlock)
                {
                    int nitrogen = SoilBlock.GetNitrogen(data);
                    if (nitrogen > 0)
                    {
                        WidgetUtils.AddLabel(stackPanel, $"Nitrogen: {nitrogen}/3", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                    }
                }
                else if (block is RyeBlock)
                {
                    bool isWild = RyeBlock.GetIsWild(data);
                    WidgetUtils.AddLabel(stackPanel, $"Is Wild: {isWild}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                    int size = RyeBlock.GetSize(data);
                    WidgetUtils.AddLabel(stackPanel, $"Size: {size}/7", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is SaplingBlock)
                {
                    if (SubsystemSaplingBlockBehavior.m_saplings.TryGetValue(point, out SubsystemSaplingBlockBehavior.SaplingData saplingData))
                    {
                        double timeRemaining = saplingData.MatureTime - SubsystemGameInfo.TotalElapsedGameTime;
                        WidgetUtils.AddLabel(stackPanel, $"Time Remaining: {timeRemaining:0.#}s", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                    }
                }
                else if (block is BasePumpkinBlock)
                {
                    bool isDead = BasePumpkinBlock.GetIsDead(data);
                    WidgetUtils.AddLabel(stackPanel, $"Is Dead: {isDead}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                    int size = BasePumpkinBlock.GetSize(data);
                    WidgetUtils.AddLabel(stackPanel, $"Size: {size}/7", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is CottonBlock)
                {
                    bool isWild = CottonBlock.GetIsWild(data);
                    WidgetUtils.AddLabel(stackPanel, $"Is Wild: {isWild}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                    int size = CottonBlock.GetSize(data);
                    WidgetUtils.AddLabel(stackPanel, $"Size: {size}/2", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is ThermometerBlock)
                {
                    float temperature = GetEnvironmentTemperature(point);
                    WidgetUtils.AddLabel(stackPanel, $"Temperature: {temperature:0.#}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is HygrometerBlock)
                {
                    float humidity = SubsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
                    WidgetUtils.AddLabel(stackPanel, $"Humidity: {humidity:0.#}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is AdjustableDelayGateBlock)
                {
                    int delay = AdjustableDelayGateBlock.GetDelay(data);
                    WidgetUtils.AddLabel(stackPanel, $"Delay: {(delay + 1) * 0.01}s", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is BatteryBlock)
                {
                    int voltage = BatteryBlock.GetVoltageLevel(data);
                    WidgetUtils.AddLabel(stackPanel, $"Voltage: {voltage * 0.1}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is SwitchBlock)
                {
                    int voltage = SwitchBlock.GetVoltageLevel(data);
                    WidgetUtils.AddLabel(stackPanel, $"Voltage: {voltage * 0.1}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
                else if (block is PistonBlock)
                {
                    PistonMode pistonMode = PistonBlock.GetMode(data);
                    int maxExtension = PistonBlock.GetMaxExtension(data);
                    int pullCount = PistonBlock.GetPullCount(data);
                    int speed = PistonBlock.GetSpeed(data);
                    WidgetUtils.AddLabel(stackPanel, $"Max Extension: {maxExtension + 1}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                    if (pistonMode != PistonMode.Pushing)
                        WidgetUtils.AddLabel(stackPanel, $"Pull Count: {pullCount + 1}", Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                    WidgetUtils.AddLabel(stackPanel, "Speed: " + (speed <= 0 ? "Fast" : speed <= 1 ? "Medium" : speed <= 2 ? "Slow" : "Very Slow"), Color.LightGray, 0.6f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
                }
            }

            // Cho phép Mod khác chèn thêm thông tin khác
            OnBuildBlockInfo?.Invoke(this, targetInfo, block, value, stackPanel);

            if (componentBlockEntity != null && HUDSettingsManager.LookingAtInfoSettings.ShowInventory)
            {
                ComponentInventoryBase componentInventoryBase = componentBlockEntity.Entity.FindComponent<ComponentInventoryBase>();
                if (componentInventoryBase is IInventory inventory)
                {
                    BuildInventoryGrid(inventory, stackPanel, new Vector2(32f), new Vector2(32f), 0.5f);
                }
            }

            if (HUDSettingsManager.LookingAtInfoSettings.ShowPackageName)
            {
                string packageName = ModEntityHelper.GetPackageName(block);
                WidgetUtils.AddLabel(stackPanel, packageName, ColorPalette.AccentBlue, 0.85f, false, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);
            }
        }

        public static void BuildInventoryGrid(IInventory inventory, ContainerWidget parent, Vector2 canvasSize, Vector2 iconSize, float fontScale)
        {
            List<(int value, int count)> items = new List<(int, int)>();

            for (int i = 0; i < inventory.SlotsCount; i++)
            {
                int value = inventory.GetSlotValue(i);
                int count = inventory.GetSlotCount(i);

                if (count > 0)
                {
                    items.Add((value, count));
                }
            }

            BuildInventoryGrid(items, parent, canvasSize, iconSize, fontScale);
        }

        public static void BuildInventoryGrid(List<(int value, int count)> items, ContainerWidget parent, Vector2 canvasSize, Vector2 iconSize, float fontScale)
        {
            if (parent == null || items == null || items.Count == 0)
                return;

            const int maxPerRow = 8;
            const int maxRows = 4;

            int totalSlots = Math.Min(items.Count, maxPerRow * maxRows);

            StackPanelWidget currentRow = null;
            int column = 0;
            int row = 0;

            for (int i = 0; i < totalSlots; i++)
            {
                if (column == 0)
                {
                    if (row >= maxRows)
                        break;

                    currentRow = new StackPanelWidget
                    {
                        Direction = LayoutDirection.Horizontal,
                        HorizontalAlignment = WidgetAlignment.Near,
                        VerticalAlignment = WidgetAlignment.Center,
                        Margin = new Vector2(2f)
                    };

                    parent.Children.Add(currentRow);
                    row++;
                }

                (int value, int count) item = items[i];

                WidgetUtils.AddInventorySlot(currentRow, item.value, item.count, canvasSize, iconSize, fontScale, new Vector2(2f), WidgetAlignment.Center, WidgetAlignment.Near);

                column++;

                if (column >= maxPerRow)
                    column = 0;
            }

            int maxSlots = maxPerRow * maxRows;
            int remaining = items.Count - maxSlots;

            if (remaining > 0 && currentRow != null)
            {
                WidgetUtils.AddLabel(currentRow, $"+{remaining}", Color.LightGray, 0.6f, false, new Vector2(6f, 2f), WidgetAlignment.Center, WidgetAlignment.Near);
            }
        }

        public float GetEnvironmentTemperature(Point3 point)
        {
            SubsystemMetersBlockBehavior.CalculateTemperature(point.X, point.Y, point.Z, 0f, 0f, out float targetTemperature, out float targetTemperatureFlux, out float environmentTemperature);
            return environmentTemperature;
        }

        public static float GetSmoothHumidity(SubsystemTerrain subsystemTerrain, float worldX, float worldZ)
        {
            Terrain terrain = subsystemTerrain.Terrain;

            int cellX = Terrain.ToCell(worldX);
            int cellZ = Terrain.ToCell(worldZ);

            float fx = worldX - cellX;
            float fz = worldZ - cellZ;

            float h00 = terrain.GetSeasonalHumidity(cellX, cellZ);
            float h01 = terrain.GetSeasonalHumidity(cellX, cellZ + 1);
            float h10 = terrain.GetSeasonalHumidity(cellX + 1, cellZ);
            float h11 = terrain.GetSeasonalHumidity(cellX + 1, cellZ + 1);

            float h0 = MathUtils.Lerp(h00, h01, fz);
            float h1 = MathUtils.Lerp(h10, h11, fz);

            return MathUtils.Lerp(h0, h1, fx);
        }

        public void AddBlockDigMethod(LookTargetInfo targetInfo, StackPanelWidget stackPanel, int value, int contents, int data, Block block)
        {
            ComponentMiner componentMiner = ComponentPlayer?.ComponentMiner;
            GameMode gameMode = SubsystemGameInfo.WorldSettings.GameMode;

            if (gameMode == GameMode.Creative || gameMode == GameMode.Adventure || componentMiner == null)
                return;

            BlockDigMethod blockDigMethod = block.GetBlockDigMethod(value);
            bool valid = IsDigMethodValid(blockDigMethod, componentMiner.ActiveBlockValue);

            CreateDigMethod(stackPanel, blockDigMethod, valid, new Vector2(32f), new Vector2(32f), new Vector2(16f), new Vector2(2f), WidgetAlignment.Near, WidgetAlignment.Near);
        }

        public static bool IsDigMethodValid(BlockDigMethod target, int value)
        {
            int contents = Terrain.ExtractContents(value);
            Block block = BlocksManager.Blocks[contents];

            float shovelPower = block.GetShovelPower(value);
            float hackPower = block.GetHackPower(value);
            float quarryPower = block.GetQuarryPower(value);

            if (target == BlockDigMethod.None)
                return true;

            if (target == BlockDigMethod.Shovel && shovelPower >= 2f)
                return true;

            if (target == BlockDigMethod.Hack && hackPower >= 3f)
                return true;

            if (target == BlockDigMethod.Quarry && quarryPower >= 5f)
                return true;

            return false;
        }

        public static CanvasWidget CreateDigMethod(ContainerWidget parent, BlockDigMethod blockDigMethod, bool valid, Vector2 cavasSize, Vector2 iconSize, Vector2 tickSize, Vector2 margin, WidgetAlignment verticalAlignment, WidgetAlignment horizontalAlignment)
        {
            if (parent != null)
            {
                CanvasWidget canvas = new CanvasWidget
                {
                    Size = cavasSize,
                    VerticalAlignment = verticalAlignment,
                    HorizontalAlignment = horizontalAlignment,
                    Margin = margin
                };

                int value = blockDigMethod == BlockDigMethod.Shovel ? IronShovelBlock.Index :
                    blockDigMethod == BlockDigMethod.Hack ? IronAxeBlock.Index :
                    blockDigMethod == BlockDigMethod.Quarry ? IronPickaxeBlock.Index : 0;

                BlockIconWidget blockIcon = new BlockIconWidget
                {
                    Value = value,
                    HorizontalAlignment = WidgetAlignment.Center,
                    VerticalAlignment = WidgetAlignment.Center,
                    Margin = new Vector2(2f, 2f)
                };
                canvas.Children.Add(blockIcon);

                RectangleWidget rectangle = new RectangleWidget
                {
                    Size = tickSize,
                    VerticalAlignment = WidgetAlignment.Far,
                    HorizontalAlignment = WidgetAlignment.Far,
                    Subtexture = ContentManager.Get<Subtexture>(valid ? "Textures/Atlas/Tick" : "Textures/Gui/Unavailable"),
                    OutlineColor = Color.Transparent,
                    FillColor = valid ? Color.Green : Color.Red
                };
                canvas.Children.Add(rectangle);

                parent.Children.Add(canvas);
                return canvas;
            }
            return null;
        }

        public void AddBlockDigProgress(LookTargetInfo targetInfo, StackPanelWidget stackPanel, int value, int contents, int data, Block block)
        {
            ComponentMiner componentMiner = ComponentPlayer?.ComponentMiner;
            GameMode gameMode = SubsystemGameInfo.WorldSettings.GameMode;

            if (gameMode == GameMode.Creative || gameMode == GameMode.Adventure)
                return;

            float digProgress = targetInfo.DigProgress;
            if (digProgress <= 0f)
                return;

            ValueFillWidget valueFill = new ValueFillWidget
            {
                Value = digProgress,
                BarSize = new Vector2(0f, 2f),
                HorizontalAlignment = WidgetAlignment.Stretch
            };
            stackPanel.Children.Add(valueFill);
        }

        public bool TryGetLookingAtInfo(LookTargetInfo targetInfo)
        {
            targetInfo.LookTargetType = LookTargetType.None;

            ComponentMiner componentMiner = ComponentPlayer?.ComponentMiner;

            if (componentMiner != null && componentMiner.DigCellFace.HasValue)
            {
                CellFace cellFace = componentMiner.DigCellFace.Value;
                int value = SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);

                targetInfo.LookTargetType = LookTargetType.Block;
                targetInfo.BlockValue = value;
                targetInfo.CellFace = cellFace;
                targetInfo.DigProgress = componentMiner.DigProgress;

                return true;
            }

            Camera camera = GameWidget.ActiveCamera;

            Vector3 startPosition = camera.ViewPosition;
            Vector3 endPosition = camera.ViewPosition + camera.ViewDirection * MaxDistance;

            if (TryBodyRaycast(targetInfo, startPosition, endPosition))
                return true;

            MovingBlocksRaycastResult? movingBlocksRaycastResult = SubsystemMovingBlocks.Raycast(startPosition, endPosition, true, (int value, float distance) =>
            {
                int contents = Terrain.ExtractContents(value);
                return contents != 0 && distance <= MaxDistance;
            });

            TerrainRaycastResult? terrainRaycastResult = SubsystemTerrain.Raycast(startPosition, endPosition, true, true, (int value, float distance) =>
            {
                int contents = Terrain.ExtractContents(value);
                return contents != 0 && contents != WaterBlock.Index && distance <= MaxDistance;
            });

            return TryResolveBlockHit(targetInfo, movingBlocksRaycastResult, terrainRaycastResult);
        }

        public bool TryBodyRaycast(LookTargetInfo targetInfo, Vector3 startPosition, Vector3 endPosition)
        {
            BodyRaycastResult? bodyRaycastResult = SubsystemBodies.Raycast(startPosition, endPosition, 0.1f, (ComponentBody componentBody, float distance) =>
            {
                return componentBody != null && componentBody != ComponentBody && distance <= MaxDistance;
            });

            if (bodyRaycastResult is BodyRaycastResult bodyHit)
            {
                ComponentBody componentBody = bodyHit.ComponentBody;
                targetInfo.ComponentBody = componentBody;

                ComponentPlayer componentPlayer = componentBody.Entity.FindComponent<ComponentPlayer>();

                if (componentPlayer != null)
                {
                    targetInfo.LookTargetType = LookTargetType.Player;
                    targetInfo.ComponentPlayer = componentPlayer;
                }
                else
                {
                    ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();

                    if (componentCreature != null)
                    {
                        targetInfo.LookTargetType = LookTargetType.Creature;
                        targetInfo.ComponentCreature = componentCreature;
                    }
                    else
                    {
                        targetInfo.LookTargetType = LookTargetType.Entity;
                    }
                }
                return true;
            }
            return false;
        }

        public bool TryResolveBlockHit(LookTargetInfo targetInfo, MovingBlocksRaycastResult? movingBlocksRaycastResult, TerrainRaycastResult? terrainRaycastResult)
        {
            float movingDistance = movingBlocksRaycastResult?.Distance ?? float.MaxValue;
            float terrainDistance = terrainRaycastResult?.Distance ?? float.MaxValue;

            if (movingDistance == float.MaxValue && terrainDistance == float.MaxValue)
                return false;

            if (movingDistance < terrainDistance)
            {
                MovingBlocksRaycastResult movingBlockHit = movingBlocksRaycastResult.Value;

                targetInfo.LookTargetType = LookTargetType.MovingBlock;
                targetInfo.BlockValue = movingBlockHit.BlockValue;
                targetInfo.MovingBlock = movingBlockHit.MovingBlock;
            }
            else
            {
                TerrainRaycastResult terrainHit = terrainRaycastResult.Value;

                targetInfo.LookTargetType = LookTargetType.Block;
                targetInfo.BlockValue = terrainHit.Value;
                targetInfo.CellFace = terrainHit.CellFace;
            }

            return true;
        }

        public void ClearLookingAtInfo(bool hideWidget = false)
        {
            if (m_lookingAtInfo == null)
                return;

            for (int i = m_lookingAtInfo.Children.Count - 1; i >= 2; i--)
                m_lookingAtInfo.Children.RemoveAt(i);

            if (hideWidget)
                m_lookingAtInfo.IsVisible = false;
        }
    }
}
