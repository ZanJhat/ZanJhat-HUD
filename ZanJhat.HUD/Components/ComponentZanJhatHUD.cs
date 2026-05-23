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
using ZanJhat.HUD;

namespace ZanJhat.HUD
{
    public abstract class HUDModule
    {
        public ComponentZanJhatHUD Owner;

        public SubsystemTime SubsystemTime => Owner.m_subsystemTime;
        public SubsystemGameInfo SubsystemGameInfo => Owner.m_subsystemGameInfo;
        public SubsystemParticles SubsystemParticles => Owner.m_subsystemParticles;
        public SubsystemBodies SubsystemBodies => Owner.m_subsystemBodies;
        public SubsystemTerrain SubsystemTerrain => Owner.m_subsystemTerrain;
        public SubsystemMovingBlocks SubsystemMovingBlocks => Owner.m_subsystemMovingBlocks;
        public SubsystemSaplingBlockBehavior SubsystemSaplingBlockBehavior => Owner.m_subsystemSaplingBlockBehavior;
        public SubsystemMetersBlockBehavior SubsystemMetersBlockBehavior => Owner.m_subsystemMetersBlockBehavior;
        public SubsystemBlockEntities SubsystemBlockEntities => Owner.m_subsystemBlockEntities;

        public ComponentPlayer ComponentPlayer => Owner.m_componentPlayer;
        public ComponentHealth ComponentHealth => Owner.m_componentHealth;
        public ComponentBody ComponentBody => Owner.m_componentBody;
        public ComponentInput ComponentInput => Owner.m_componentInput;
        public ComponentGui ComponentGui => Owner.m_componentGui;

        public GameWidget GameWidget => Owner.m_componentPlayer.GameWidget;
        public ContainerWidget GuiWidget => Owner.m_componentPlayer.GuiWidget;

        public static event Action<HUDModule> OnHUDLoaded;
        public static event Action<HUDModule> OnHUDUnloaded;
        public static event Action<HUDModule, float> OnBeforeHUDUpdate;
        public static event Action<HUDModule, float> OnAfterHUDUpdate;

        public void Load(ComponentZanJhatHUD owner)
        {
            Owner = owner;
            OnLoad();
            OnHUDLoaded?.Invoke(this);
        }

        public void Unload()
        {
            OnUnload();
            OnHUDUnloaded?.Invoke(this);
        }

        public void Update(float dt)
        {
            OnBeforeHUDUpdate?.Invoke(this, dt);
            OnUpdate(dt);
            OnAfterHUDUpdate?.Invoke(this, dt);
        }

        protected virtual void OnLoad()
        {
        }

        protected virtual void OnUnload()
        {
        }

        protected virtual void OnUpdate(float dt)
        {
        }
    }

    public class ComponentZanJhatHUD : Component, IUpdateable
    {
        public SubsystemTime m_subsystemTime;
        public SubsystemGameInfo m_subsystemGameInfo;
        public SubsystemParticles m_subsystemParticles;
        public SubsystemBodies m_subsystemBodies;
        public SubsystemTerrain m_subsystemTerrain;
        public SubsystemMovingBlocks m_subsystemMovingBlocks;
        public SubsystemSaplingBlockBehavior m_subsystemSaplingBlockBehavior;
        public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;
        public SubsystemBlockEntities m_subsystemBlockEntities;

        public ComponentPlayer m_componentPlayer;
        public ComponentHealth m_componentHealth;
        public ComponentBody m_componentBody;
        public ComponentInput m_componentInput;
        public ComponentGui m_componentGui;

        private List<HUDModule> m_modules = new();

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public void Update(float dt)
        {
            for (int i = 0; i < m_modules.Count; i++)
                m_modules[i].Update(dt);
        }

        public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
        {
            base.Load(valuesDictionary, idToEntityMap);
            m_subsystemTime = Project.FindSubsystem<SubsystemTime>(true);
            m_subsystemGameInfo = Project.FindSubsystem<SubsystemGameInfo>(true);
            m_subsystemParticles = Project.FindSubsystem<SubsystemParticles>(true);
            m_subsystemBodies = Project.FindSubsystem<SubsystemBodies>(true);
            m_subsystemTerrain = Project.FindSubsystem<SubsystemTerrain>(true);
            m_subsystemMovingBlocks = Project.FindSubsystem<SubsystemMovingBlocks>(true);
            m_subsystemSaplingBlockBehavior = Project.FindSubsystem<SubsystemSaplingBlockBehavior>(true);
            m_subsystemMetersBlockBehavior = Project.FindSubsystem<SubsystemMetersBlockBehavior>(true);
            m_subsystemBlockEntities = Project.FindSubsystem<SubsystemBlockEntities>(true);

            m_componentPlayer = Entity.FindComponent<ComponentPlayer>(true);
            m_componentHealth = Entity.FindComponent<ComponentHealth>(true);
            m_componentBody = Entity.FindComponent<ComponentBody>(true);
            m_componentInput = Entity.FindComponent<ComponentInput>(true);
            m_componentGui = Entity.FindComponent<ComponentGui>(true);

            ClearModules();
            RegisterModules();
        }

        public void ClearModules()
        {
            foreach (HUDModule module in m_modules)
                module.Unload();

            m_modules.Clear();
        }

        private void RegisterModules()
        {
            RegisterModule<LookingAtInfoHUDModule>();
            RegisterModule<FocusWidgetHUDModule>();
            RegisterModule<FocusWidgetExtendedHUDModule>();
            RegisterModule<PositionHUDModule>();
            RegisterModule<TimeInfoHUDModule>();
            RegisterModule<ClothingInfoHUDModule>();
        }

        public void RegisterModule<T>(bool logError = true, bool logSuccess = false) where T : HUDModule, new()
        {
            if (m_modules.Any(m => m is T))
            {
                if (logError)
                    Log.Information($"[{GetType().Name}] Module {typeof(T).Name} already registered.");
                return;
            }

            T module = new T();
            module.Load(this);
            m_modules.Add(module);

            if (logSuccess)
                Log.Information($"[{GetType().Name}] Module {typeof(T).Name} registered.");
        }

        public T GetModule<T>(bool logError = true, bool logSuccess = false) where T : HUDModule
        {
            for (int i = 0; i < m_modules.Count; i++)
            {
                if (m_modules[i] is T module)
                {
                    if (logSuccess)
                        Log.Information($"[{GetType().Name}] Module {typeof(T).Name} retrieved.");

                    return module;
                }
            }

            if (logError)
                Log.Information($"[{GetType().Name}] Module {typeof(T).Name} not found.");

            return null;
        }

        public void UnregisterModule<T>(bool logError = true, bool logSuccess = false) where T : HUDModule
        {
            HUDModule module = m_modules.FirstOrDefault(m => m is T);

            if (module == null)
            {
                if (logError)
                    Log.Information($"[{GetType().Name}] Module {typeof(T).Name} not registered.");
                return;
            }

            module.Unload();
            m_modules.Remove(module);

            if (logSuccess)
                Log.Information($"[{GetType().Name}] Module {typeof(T).Name} unregistered.");
        }

        public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
        {
            base.Save(valuesDictionary, entityToIdMap);
        }
    }
}
