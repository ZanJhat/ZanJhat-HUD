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
using Engine.Input;
using System.Globalization;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class HUDModLoader : ModLoader
    {
        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemTime m_subsystemTime;
        public SubsystemPlayers subsystemPlayers;
        public SubsystemParticles m_subsystemParticles;
        public SubsystemBodies m_subsystemBodies;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemTimeOfDay m_subsystemTimeOfDay;
        public SubsystemPickables m_subsystemPickables;
        public SubsystemAudio m_subsystemAudio;

        public Game.Random m_random = new();

        public override void __ModInitialize()
        {
            ModsManager.RegisterHook("BeforeWidgetUpdate", this);
            ModsManager.RegisterHook("OnInventorySlotWidgetDefined", this);
            ModsManager.RegisterHook("OnWidgetContentsLoaded", this);
            ModsManager.RegisterHook("OnProjectLoaded", this);
            ModsManager.RegisterHook("OnLoadingFinished", this);
        }

        public override void BeforeWidgetUpdate(Widget widget)
        {
            if (widget == null)
                return;

            if (widget is DragHostWidget dragHost && dragHost.IsDragInProgress)
            {
                Widget dragWidget = dragHost.m_dragWidget;

                if (dragWidget != null && FocusItemManager.GetFocusedItem(dragWidget) == null)
                {
                    if (dragHost.m_dragData is InventoryDragData dragData && dragData.Inventory != null)
                    {
                        FocusedItem dragFocusItem = FocusItemManager.CreateInventoryItemFocusedItem(dragWidget, "Drag", dragData.Inventory, dragData.SlotIndex);

                        FocusItemManager.SetFocusedItem(dragWidget, dragFocusItem);
                    }
                }
            }
        }

        public override void OnInventorySlotWidgetDefined(InventorySlotWidget inventorySlotWidget, out List<Widget> childrenWidgetsToAdd)
        {
            base.OnInventorySlotWidgetDefined(inventorySlotWidget, out childrenWidgetsToAdd);

            FocusedItem inventorySlotFocusItem = FocusItemManager.CreateInventoryItemFocusedItem(inventorySlotWidget, "InventorySlot", inventorySlotWidget.m_inventory, inventorySlotWidget.m_slotIndex);

            FocusItemManager.SetFocusedItem(inventorySlotWidget, inventorySlotFocusItem);
        }


        public override void OnWidgetContentsLoaded(Widget widget)
        {
            if (widget == null) return;
        }

        public override void OnProjectLoaded(Project project)
        {
            m_subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemTime = project.FindSubsystem<SubsystemTime>(true);
            subsystemPlayers = project.FindSubsystem<SubsystemPlayers>(true);
            m_subsystemParticles = project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemBodies = project.FindSubsystem<SubsystemBodies>(throwOnError: true);
            m_subsystemTerrain = project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemTimeOfDay = project.FindSubsystem<SubsystemTimeOfDay>(true);
            m_subsystemPickables = project.FindSubsystem<SubsystemPickables>(true);
            m_subsystemAudio = project.FindSubsystem<SubsystemAudio>(true);
        }

        public override void OnLoadingFinished(List<System.Action> actions)
        {
            actions.Add(() =>
            {
                // 1. Core systems
                HUDManager.Initialize();

                // 2. Settings
                HUDSettingsManager.Initialize();

                // 3. Gameplay systems

                // 4. UI
                ScreensManager.AddScreen("RecipaediaIngredientUsage", new RecipaediaIngredientUsageScreen());
            });
        }
    }
}
