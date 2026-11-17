using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Globalization;
using Engine;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public static class HUDIconPaths
    {
        private const string Base = "Textures/HUDIcons/";

        // Combat
        public const string Sword = Base + "Sword";
        public const string Probability = Base + "Probability";
        public const string Projectile = Base + "Projectile";

        // Tools
        public const string Shovel = Base + "Shovel";
        public const string Axe = Base + "Axe";
        public const string Pickaxe = Base + "Pickaxe";

        // Light / Fuel
        public const string LightBulb = Base + "LightBulb";
        public const string Fuel = Base + "Fuel";

        // Stack / Fire
        public const string Layer = Base + "Layer";
        public const string Fire = Base + "Fire";

        // Storage / Rot
        public const string Ice = Base + "Ice";

        // Digging
        public const string DigResilience = Base + "DigResilience";

        // Explosion
        public const string ExplosionResilience = Base + "ExplosionResilience";
        public const string Explosion = Base + "Explosion";

        // Clothing
        public const string PaintBrush = Base + "PaintBrush";
        public const string Armor = Base + "Armor";
        public const string Absorption = Base + "Absorption";
        public const string Insulation = Base + "Insulation";
        public const string MovementSpeed = Base + "MovementSpeed";
    }
}
