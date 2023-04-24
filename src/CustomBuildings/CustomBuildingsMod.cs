using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModLoader;
using ModLoader.Utilities;
using SEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TinyZoo;
using TinyZoo.Tile_Data;

namespace CustomBuildings
{
    public class CustomBuildingsMod : IMod
    {
        Harmony HarmonyInstance;
        static IModHelper Helper;
        static Config Config;
        static List<BuildingDefinition> Buildings = new List<BuildingDefinition>();
        static IEnumerable<BuildingDefinition> Floors => Buildings.Where(b => b.Category == "Floor");
        static IEnumerable<BuildingDefinition> Decorations => Buildings.Where(b => b.Category == "Decoration");

        public const int MinimumID = 1000000;

        public static bool LoadedEntries = false;

        public void ModEntry(IModHelper helper)
        {
            Helper = helper;
            Config = helper.Config.LoadConfig<Config>();
            HarmonyInstance = new Harmony("Platonymous.CustomBuildings");

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.CategoryData, LetsBuildAZoo"), "GetEntriesInThisCategory"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetEntriesInThisCategory)));

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "GetTileStats"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetTileStats)));

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "GetTileInfo"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetTileInfo)));

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Blance.BlingDingCosts, LetsBuildAZoo"), "GetCost"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetCost)));


            HarmonyInstance.Patch(
                    original: AccessTools.Method(typeof(HashSet<TILETYPE>), "Add"),
                    postfix: new HarmonyMethod(this.GetType(), nameof(AddTileType)));


            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), "GetBuildingIconRectangle"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetBuildingIconRectangle)));


            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.OverWorld.OverWorldEnv.WallsAndFloors.Components.ComponentData, LetsBuildAZoo"), "GetRenderComponent"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetRenderComponent)));


            HarmonyInstance.Patch(
                original: AccessTools.PropertyGetter(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), "Name"),
                prefix: new HarmonyMethod(this.GetType(), nameof(TileStatsName)));
            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "IsThisFloorAVolumeFloor"),
                postfix: new HarmonyMethod(this.GetType(), nameof(IsThisFloorAVolumeFloor)));

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "IsThisATopFloorOnly"),
                postfix: new HarmonyMethod(this.GetType(), nameof(IsThisATopFloorOnly)));

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.CategoryData, LetsBuildAZoo"), "IsThisACroppedFloor"),
                postfix: new HarmonyMethod(this.GetType(), nameof(IsThisACroppedFloor)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "IsThisAPartialFloor"),
               postfix: new HarmonyMethod(this.GetType(), nameof(IsThisAPartialFloor)));



            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "IsThisAPenDecoration"),
               postfix: new HarmonyMethod(this.GetType(), nameof(IsThisAPenDecoration)));


            HarmonyInstance.Patch(
               original: AccessTools.Constructor(Type.GetType("TinyZoo.Z_PenInfo.MainBar.SimpleBuildingRenderer, LetsBuildAZoo"), new Type[] { typeof(TILETYPE), typeof(int) }),
               postfix: new HarmonyMethod(this.GetType(), nameof(SimpleBuildingRenderer)));


            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Z_OverWorld.SpawnAnimations.CascadeSpawner, LetsBuildAZoo"), "DoCascadeForBuildingorTree"),
               prefix: new HarmonyMethod(this.GetType(), nameof(DoCascadeForBuildingorTree)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.CategoryData, LetsBuildAZoo"), "GetCategoryDescription", new Type[] { typeof(TILETYPE) }),
               prefix: new HarmonyMethod(this.GetType(), nameof(GetCategoryDescription)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.MainGameCats, LetsBuildAZoo"), "SetUpFloors"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpFloors)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.SandBoxCats, LetsBuildAZoo"), "SetUpFloors"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpFloors)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.DinosaurCats, LetsBuildAZoo"), "SetUpFloors"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpFloors)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.AquariumCats, LetsBuildAZoo"), "SetUpFloors"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpFloors)));


            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.MainGameCats, LetsBuildAZoo"), "SetUpDecoratives"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpDecoratives)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.DinosaurCats, LetsBuildAZoo"), "SetUpDecoratives"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpDecoratives)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.SandBoxCats, LetsBuildAZoo"), "SetUpDecoratives"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpDecoratives2)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.AquariumCats, LetsBuildAZoo"), "SetUpDecoratives"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpDecoratives)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.MainGameCats, LetsBuildAZoo"), "SetUpNature"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpNature)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.SandBoxCats, LetsBuildAZoo"), "SetUpNature"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpNature)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.DinosaurCats, LetsBuildAZoo"), "SetUpNature"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpNature)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.AquariumCats, LetsBuildAZoo"), "SetUpNature"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetUpNature)));


            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.MainGameCats, LetsBuildAZoo"), "SetPenDeco"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetPenDeco)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.SandBoxCats, LetsBuildAZoo"), "SetPenDeco"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetPenDeco)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.DinosaurCats, LetsBuildAZoo"), "SetPenDeco"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetPenDeco)));
            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.ScenarioCategories.AquariumCats, LetsBuildAZoo"), "SetPenDeco"),
               postfix: new HarmonyMethod(this.GetType(), nameof(SetPenDeco)));


            foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(Type.GetType("TinyZoo.PlayerDir.Layout.LayoutEntry, LetsBuildAZoo")))
                HarmonyInstance.Patch(
               original: constructor,
               postfix: new HarmonyMethod(this.GetType(), nameof(LayoutEntry)));

            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.OverWorld.OverWorldEnv.WallsAndFloors.WallsAndFloorsManager, LetsBuildAZoo"), "VallidateAgainstLayout"),
               postfix: new HarmonyMethod(this.GetType(), nameof(VallidateAgainstLayout)));



            LoadContentPacks(helper.GetContentPacks());
        }

        public static void VallidateAgainstLayout(object __instance)
        {
            Array tilesasarray = Reflection.GetFieldValue<Array>("tilesasarray", __instance);
            int width = tilesasarray.GetLength(0);
            int height = tilesasarray.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (tilesasarray.GetValue(x, y) is object tvalue && tvalue != null && Reflection.GetFieldValue<TILETYPE>("tiletypeonconstruct", tvalue) is TILETYPE tiletype)
                        if (tiletype == TILETYPE.None || ((int)tiletype > (int)TILETYPE.Count && !TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd)))
                            tilesasarray.SetValue(null, x, y);
            Reflection.SetFieldValue("tilesasarray", __instance, tilesasarray);

            Array FloorTilesArray = Reflection.GetFieldValue<Array>("FloorTilesArray", __instance);
            width = FloorTilesArray.GetLength(0);
            height = FloorTilesArray.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (FloorTilesArray.GetValue(x, y) is object tvalue && tvalue != null && Reflection.GetFieldValue<TILETYPE>("tiletypeonconstruct", tvalue) is TILETYPE tiletype)
                        if (tiletype == TILETYPE.None || ((int)tiletype > (int)TILETYPE.Count && !TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd)))
                            FloorTilesArray.SetValue(null, x, y);
            Reflection.SetFieldValue("FloorTilesArray", __instance, FloorTilesArray);

            Array UnderFloorTilesArray = Reflection.GetFieldValue<Array>("UnderFloorTilesArray", __instance);
            width = UnderFloorTilesArray.GetLength(0);
            height = UnderFloorTilesArray.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (UnderFloorTilesArray.GetValue(x, y) is object tvalue && tvalue != null && Reflection.GetFieldValue<TILETYPE>("tiletypeonconstruct", tvalue) is TILETYPE tiletype)
                        if (tiletype == TILETYPE.None || ((int)tiletype > (int)TILETYPE.Count && !TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd)))
                            UnderFloorTilesArray.SetValue(null, x, y);
            Reflection.SetFieldValue("UnderFloorTilesArray", __instance, UnderFloorTilesArray);
        }

        public static void LayoutEntry(object __instance)
        {
            TILETYPE tiletype = Reflection.GetFieldValue<TILETYPE>("tiletype", __instance);
            if ((int)tiletype > (int)TILETYPE.Count && !TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
                Reflection.SetFieldValue("tiletype", __instance, TILETYPE.None);
        }

        public static bool GetTileInfo(ref TILETYPE tiletype, ref object __result)
        {
            if ((int)tiletype > (int)TILETYPE.Count && !TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bdx))
                tiletype = TILETYPE.None;
            else if (TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
            {
                __result = RegisterTileInfo(bd);
                return false;
            }

            return true;
        }

        public static void SetPenDeco(ref List<TILETYPE> _Pen_Deco)
        {
            foreach (var bd in Buildings.Where(b => b.Category != "Floors" && b.AllowEnclosure))
                if (!_Pen_Deco.Contains((TILETYPE)bd.GetTileId()))
                    _Pen_Deco.Add((TILETYPE)bd.GetTileId());
        }

        public static void SetUpDecoratives(ref List<TILETYPE> _Decorative)
        {
            foreach (var bd in Buildings.Where(b => b.Category == "Decorative"))
                if (!_Decorative.Contains((TILETYPE)bd.GetTileId()))
                    _Decorative.Add((TILETYPE)bd.GetTileId());
        }
        public static void SetUpDecoratives2(ref List<TILETYPE> _Decoratives)
        {
            foreach (var bd in Buildings.Where(b => b.Category == "Decorative"))
                if (!_Decoratives.Contains((TILETYPE)bd.GetTileId()))
                    _Decoratives.Add((TILETYPE)bd.GetTileId());
        }

        public static void SetUpFloors(ref List<TILETYPE> _Floors)
        {
            foreach (var bd in Buildings.Where(b => b.Category == "Floors"))
                if (!_Floors.Contains((TILETYPE)bd.GetTileId()))
                    _Floors.Add((TILETYPE)bd.GetTileId());

        }

        public static void SetUpNature(ref List<TILETYPE> _Nature)
        {
            foreach (var bd in Buildings.Where(b => b.Category == "Nature"))
                if (!_Nature.Contains((TILETYPE)bd.GetTileId()))
                    _Nature.Add((TILETYPE)bd.GetTileId());

        }

        public static bool GetCategoryDescription(TILETYPE tiletype, ref string __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
            {
                if (bd.Category == "Floors")
                    __result = SEngine.Localization.Localization.GetText(409);
                else if (bd.Category == "Nature")
                    __result = SEngine.Localization.Localization.GetText(411);
                else
                    __result = SEngine.Localization.Localization.GetText(391);

                return false;
            }

            return true;
        }

        public static bool DoCascadeForBuildingorTree(object tilerenderer)
        {
            return tilerenderer != null;
        }

        public static void SimpleBuildingRenderer(object __instance, TILETYPE tiletype, int ROtation = 0)
        {
            if (ROtation == 0 && __instance is GameObject sbr && TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd) && GetBuildingType(bd.BuildingType) != 0)
            {
                if (Reflection.GetFieldValue<GameObject>("TopLayer", __instance) is GameObject topLayer)
                {
                    if (topLayer != null && topLayer.DrawOrigin != null)
                    {
                        topLayer.DrawOrigin.X = 0f;
                        topLayer.DrawOrigin.Y = GetYOffsetForMenu(bd);
                    }
                }
            }

        }


        public static float GetYOffsetForMenu(BuildingDefinition definition)
        {
            return (definition.FrontTileHeight - definition.TileHeight);

        }



        public static void IsThisAPenDecoration(TILETYPE tiletype, ref bool __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
            {
                __result = GetBuildingType(bd.BuildingType) != 0 && bd.AllowEnclosure;
            }
        }

        public static void IsThisAPartialFloor(object entry, ref bool __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)Reflection.GetFieldValue<TILETYPE>("tiletype", entry), out BuildingDefinition bd))
            {
                __result = GetBuildingType(bd.BuildingType) == 0;
            }
        }


        public static void IsThisACroppedFloor(TILETYPE tiletype, ref bool __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
                __result = GetBuildingType(bd.BuildingType) == 0 && bd.IsCroppedFloor;
        }

        public static void IsThisFloorAVolumeFloor(TILETYPE tiletype, ref bool __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)tiletype, out BuildingDefinition bd))
                __result = bd.IsVolumeFloor;
        }

        public static void IsThisATopFloorOnly(TILETYPE tiltype, ref bool __result)
        {
            if (TryGetBuildingDefinitionFromTileId((int)tiltype, out BuildingDefinition bd))
                __result = bd.IsAboveGroundFloor;
        }


        public static void AddTileType(object __instance, object item)
        {
            if (__instance is HashSet<TILETYPE> hs && item is TILETYPE tt)
            {
                foreach (var definition in Buildings.Where(b => b.HasThisTemplate(tt)))
                    if (!hs.Contains((TILETYPE)definition.GetTileId()))
                        hs.Add((TILETYPE)definition.GetTileId());
            }
        }

        public static bool TileStatsName(object __instance, ref string __result)
        {
            int id = (int)Reflection.GetFieldValue<StringID>("__Name", __instance);

            if (TryGetBuildingDefinitionFromTileId(id, out BuildingDefinition definition))
            {
                __result = definition.Name;
                return false;
            }

            return true;
        }

        public void LoadContentPacks(IEnumerable<IModHelper> packs)
        {
            foreach (var pack in packs)
            {
                var content = pack.Content.LoadJson<Content>("content.json");
                foreach (var building in content.Buildings)
                    Buildings.Add(ReserveID(building, pack));
            }
        }

        public static void RegisterTileInfos()
        {
            Array tinfoCheck = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").GetValue(null);

            if (tinfoCheck == null || tinfoCheck.Length < MinimumID * 2)
            {
                var newArray = Array.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileInfo, LetsBuildAZoo"), MinimumID * 2);

                if (tinfoCheck != null)
                    for (int i = 0; i < tinfoCheck.Length; i++)
                        newArray.SetValue(tinfoCheck.GetValue(i), i);

                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, newArray);
            }

            foreach (var definition in Buildings)
            {
                _ = RegisterTileInfo(definition);
            }
        }

        public static object RegisterTileInfo(BuildingDefinition definition)
        {
            object thisinfo = null;
            Array tinfo = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").GetValue(null);

            Texture2D tex = definition.GetPack().Content.LoadContent<Texture2D>(definition.Texture);
            object texture = GetNewTextureHolder(tex);

            if (definition.BasicBuilding >= 0)
            {
                definition.TileHeight = definition.BasicBuilding;
                definition.TileWidth = tex.Width / definition.Rotations;
                definition.FrontTileHeight = tex.Height - definition.BasicBuilding;
                definition.FrontTileWidth = tex.Width;
                definition.BuildingType = "Building";
            }

            if (definition.BasicBuilding == -1)
                thisinfo = GetNewTileInfo(new Rectangle(0, 0, definition.TileWidth, definition.TileHeight), GetBuildingType(definition.BuildingType), false, 1, 0, texture, false, definition.Rotations, null);
            else
                thisinfo = GetNewTileInfo(new Rectangle(0, definition.FrontTileHeight, definition.TileWidth, definition.TileHeight), GetBuildingType(definition.BuildingType), false, 1, 0, texture, false, definition.Rotations, null);

            if (!definition.IsVolumeFloor)
            {
                AddRotations(thisinfo, definition, definition.Rotations);
            }

            if(definition.Frames > 1)
            {
                AccessTools.Method(thisinfo.GetType(), "SetUpBaseAnimation").Invoke(thisinfo, new object[] { definition.Frames, definition.Framerate });
            }

            tinfo.SetValue(thisinfo, definition.GetTileId());
            AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, tinfo);

            if (definition.IsVolumeFloor)
            {
                SetFloorVolumeTileData((TILETYPE)definition.GetTileId(), texture);
                tinfo = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").GetValue(null);
                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, tinfo);
                thisinfo = tinfo.GetValue(definition.GetTileId());
            }

            return thisinfo;
        }

        public BuildingDefinition ReserveID(BuildingDefinition d, IModHelper pack)
        {
            d.SetPack(pack);
            int id = -1;
            if (Config.Reserved.FirstOrDefault(r => r.ModId == d.Id) is ReservedIds reserved)
                id = reserved.TileId;

            if (id == -1)
            {
                Config.LastId += 1;
                Config.Reserved.Add(new ReservedIds() { ModId = d.Id, TileId = Config.LastId });
                Helper.Config.SaveConfig(Config);
                id = Config.LastId;
            }

            d.SetTileId(id);

            return d;
        }

        internal static bool GetRenderComponent(TILETYPE tiletype, object parent, bool IsConstructionPreview, bool IsAChild, ref object __result)
        {
            int tileid = (int)tiletype;
            if (Buildings.FirstOrDefault(b => b.GetTileId() == tileid) is BuildingDefinition bd)
            {
                if (bd.Frames == 1)
                {
                    __result = null;
                    return false;
                }
            }

            return true;
        }

        internal static bool GetBuildingIconRectangle(TILETYPE productiontype, ref Rectangle __result)
        {
            int current = (int)productiontype;
            if (Buildings.FirstOrDefault(b => b.GetTileId() == current) is BuildingDefinition bd && bd.TryGetTemplate(out TILETYPE tt))
            {
                __result = (Rectangle)AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), "GetBuildingIconRectangle").Invoke(null, new object[] { tt });
                return false;
            }
            return true;
        }

        internal static bool TryGetBuildingDefinitionFromTileId(int tileid, out BuildingDefinition definition)
        {
            if (Buildings.FirstOrDefault(b => b.GetTileId() == tileid) is BuildingDefinition bd)
            {
                definition = bd;
                return true;
            }

            definition = null;
            return false;
        }


        internal static bool GetCost(object __instance, TILETYPE tile, int Duplicates, ref int __result)
        {
            int current = (int)tile;
            if (TryGetBuildingDefinitionFromTileId(current, out BuildingDefinition definition))
            {
                if (definition.TryGetTemplate(out TILETYPE tt))
                    __result = (int)AccessTools.Method(Type.GetType("TinyZoo.Blance.BlingDingCosts, LetsBuildAZoo"), "GetCost").Invoke(null, new object[] { tt });

                if (definition.Cost != -1)
                    __result = definition.Cost;
                return false;
            }

            return true;
        }

        internal static bool GetTileStats(TILETYPE getthis, ref object __result)
        {
            Array tscheck = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").GetValue(null);

            if (tscheck.Length < MinimumID * 2)
            {
                var newArray = Array.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), MinimumID * 2);
                for (int i = 0; i < tscheck.Length; i++)
                    newArray.SetValue(tscheck.GetValue(i), i);
                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").SetValue(null, newArray);
            }

            int current = (int)getthis;
            if (TryGetBuildingDefinitionFromTileId(current, out BuildingDefinition definition))
            {
                Array tstats = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").GetValue(null);

                if (tstats.Length <= current)
                {
                    var newArray = Array.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), current + MinimumID);
                    for (int i = 0; i < tstats.Length; i++)
                        newArray.SetValue(tstats.GetValue(i), i);
                    AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").SetValue(null, newArray);
                }

                tstats = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").GetValue(null);


                if (definition.TryGetTemplate(out TILETYPE tt))
                {
                    __result = AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "GetTileStats").Invoke(null, new object[] { tt });

                    if (tstats.GetValue((int)tt) is object tts && tts != null)
                    {
                        Reflection.SetFieldValue("Productions", __result, Reflection.GetFieldValue<int[]>("Productions", tts));
                        Reflection.SetFieldValue("Consumptions", __result, Reflection.GetFieldValue<int[]>("Consumptions", tts));
                    }

                    Reflection.SetFieldValue("__Name", __result, (StringID)definition.GetTileId());
                    Reflection.SetFieldValue("__Description", __result, (StringID)StringID.Placeholder);
                }
                else
                {
                    __result = Activator.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileStats, LetsBuildAZoo"), new object[] { (StringID)definition.GetTileId(), (StringID)StringID.Placeholder });
                }

                foreach (var prod in definition.Productions)
                    AccessTools.Method(__result.GetType(), "SetProduction").Invoke(__result, new object[] { prod.Type, prod.Volume });

                foreach (var prod in definition.Consumptions)
                    AccessTools.Method(__result.GetType(), "SetConsumption").Invoke(__result, new object[] { prod.Type, prod.Volume });


                tstats.SetValue(__result, current);
                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tilestats").SetValue(null, tstats);

                return false;
            }

            return true;
        }

        public static object GetNewTextureHolder(Texture2D texture)
        {
            var th = Activator.CreateInstance(Type.GetType("TinyZoo.Utils.TextureHolder, LetsBuildAZoo"), new object[] { });
            Reflection.SetFieldValue("texture", th, texture);

            return th;
        }

        public static string GetBuildingString(int building)
        {
            if (building == 0)
                return "Floor";

            return "Building";
        }

        public static object GetNewTileInfo(
      Rectangle _BaseRect,
      int _buildingtype,
      bool _HasFullRotation,
      int _Variants = 1,
      int LockToThisRotation = -1,
      object _DrawTexture = null,
      bool _IsRepeatingLargeTexture = false,
      int TotalRotations = -1,
      object _LightsTexture = null)
        {
            var b = Enum.Parse(Type.GetType("TinyZoo.Tile_Data.BUILDINGTYPE, LetsBuildAZoo"), GetBuildingString(_buildingtype));
            var ti = Activator.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileInfo, LetsBuildAZoo"), new object[] { _BaseRect, b, _HasFullRotation, _Variants, LockToThisRotation, _DrawTexture, _IsRepeatingLargeTexture, TotalRotations, _LightsTexture });
            return ti;
        }

        internal static int GetBuildingType(string type)
        {
            switch (type)
            {
                case "Floor":
                    return 0;
                default:
                    return 2;
            }
        }

        internal static void AddBuilding(object tiledata, Rectangle baseRect, int rotation, int maxrotations, float X, float Y)
        {
            AccessTools.Method(tiledata.GetType(), "AddBuilding", new Type[] { typeof(Rectangle), typeof(Vector2), typeof(int), typeof(int) }).Invoke(tiledata, new object[] { baseRect, new Vector2(X, Y), rotation, maxrotations });
        }

        internal static void AddRotation(object tiledata, Rectangle baseRect, int rotation)
        {
            AccessTools.Method(tiledata.GetType(), "AddRotationVariant").Invoke(tiledata, new object[] { baseRect, 1, rotation });
        }



        public static void SetFloorVolumeTileData(TILETYPE type, object textureHolder)
        {

            AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "SetFloorVolumeTileData").Invoke(null, new object[] { 16, 34, type, 0, 0, textureHolder });
        }

        internal static void AddRotations(object tiledata, BuildingDefinition definition, int maxrotations)
        {
            if (maxrotations > 1)
                for (int i = 1; i < maxrotations; i++)
                {
                    if (definition.BasicBuilding == -1)
                        AddRotation(tiledata, new Rectangle(definition.TileWidth * i, 0, definition.TileWidth, definition.TileHeight), i);
                    else
                        AddRotation(tiledata, new Rectangle(definition.TileWidth * i, definition.TileHeight - definition.BasicBuilding, definition.TileWidth, definition.BasicBuilding), i);
                }

            if (definition.HasFront || definition.BasicBuilding >= 0)
            {
                for (int i = 0; i < maxrotations; i++)
                    if (definition.BasicBuilding == -1)
                        AddBuilding(tiledata, new Rectangle(definition.FrontTileWidth * i, definition.TileHeight, definition.FrontTileWidth, definition.FrontTileHeight), i, maxrotations, (definition.FrontTileWidth / 2f) + 8f, GetYOffset(definition));
                    else
                        AddBuilding(tiledata, new Rectangle(definition.FrontTileWidth * i, 0, definition.FrontTileWidth, definition.FrontTileHeight), i, maxrotations, (definition.FrontTileWidth / 2f) + 8f, definition.FrontTileHeight - definition.TileHeight / 2);
            }
        }
        
        public static float GetYOffset(BuildingDefinition definition)
        {
            float adjustment = - 8f + 8 * (int)(definition.TileHeight / 16);
            return adjustment - (definition.TileHeight / 2) + definition.FrontTileHeight;
        }

        internal static void GetEntriesInThisCategory()
        {
            if (!LoadedEntries)
            {
                RegisterTileInfos();
                LoadedEntries = true;
            }
        }
    }
}
