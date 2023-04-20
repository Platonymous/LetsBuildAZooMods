using HarmonyLib;
using ModLoader;
using ModLoader.Utilities;
using System;
using System.Collections.Generic;
using TinyZoo.Tile_Data;

namespace DebugCheats
{
    public class DebugCheatsMod : IMod
    {
        Harmony HarmonyInstance;
        public static IModHelper Helper;
        public static Config ModConfig;

        public void ModEntry(IModHelper helper)
        {
            Helper = helper;
            ModConfig = helper.Config.LoadConfig<Config>();
            HarmonyInstance = new Harmony("Platonymous.DebugCheats");
            var researcher = Type.GetType("TinyZoo.PlayerDir.Researcher, LetsBuildAZoo");

            HarmonyInstance.Patch(
                    original: AccessTools.Method(researcher, "DebugUnlockAllResearch"),
                    postfix: new HarmonyMethod(GetType(), nameof(DebugUnlockAllResearch))
                    );


            if (ModConfig.UnlockAll)
            {
                Reflection.SetStaticFieldValue("Z_DebugFlags", "UnlockAllBuildingsHack", true);
                helper.Events.UpdateTicked += Events_UpdateTicked;
            }

            if (ModConfig.IgnoreMorality)
                Reflection.SetStaticFieldValue("Z_DebugFlags", "DisableMoralityBlocks", true);

            if (ModConfig.InfiniteMoney)
                Reflection.SetStaticFieldValue("DebugFlags", "HasEndlessMoney", true);
        }

        private void Events_UpdateTicked(object sender, ModLoader.Events.UpdateTickEventArgs e)
        {
            object player = Reflection.GetStaticFieldValue<object>("Player", "singleton");
            if (player != null)
            {
                object unlocks = Reflection.GetFieldValue<object>("unlocks", player);

                if (unlocks != null && Reflection.GetFieldValue<object>("_ResearchPoints", unlocks) is int i && i < 99) { 

                    object stats = Reflection.GetFieldValue<object>("Stats", player);
                    object variants = Reflection.GetFieldValue<object>("variantsfound", stats);
                    object busroute = Reflection.GetFieldValue<object>("busroutes", player);
                    AccessTools.Method(variants.GetType(), "UnlockAllForCheat")?.Invoke(variants, new object[0]);
                    AccessTools.Method(busroute.GetType(), "UnlockAllForCheat")?.Invoke(unlocks, new object[0]);
                    AccessTools.Method(unlocks.GetType(), "UnlockAllForCheat")?.Invoke(unlocks, new object[0]);
                    Reflection.SetFieldValue("_ResearchPoints", unlocks, 99);
                }
            }
        }

        public static void DebugUnlockAllResearch(object __instance)
        {
            Helper.Console.Info("Unlocking Everyhing");
            List<TILETYPE> unlocked = new List<TILETYPE>();
            foreach (string name in Enum.GetNames(typeof(TILETYPE)))
            {
                TILETYPE type = (TILETYPE)Enum.Parse(typeof(TILETYPE), name);
                unlocked.Add(type);
            }

            AccessTools.Method(__instance.GetType(), "UnlockAllresearchForCheat").Invoke(__instance, new object[0]);
        }
    }
}
