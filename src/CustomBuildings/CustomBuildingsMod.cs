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

        public void ModEntry(IModHelper helper)
        {
            Helper = helper;
            Config = helper.Config.LoadConfig<Config>();
            HarmonyInstance = new Harmony("Platonymous.CustomBuildings");

            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.CategoryData, LetsBuildAZoo"), "GetEntriesInThisCategory"),
                postfix: new HarmonyMethod(this.GetType(), nameof(GetEntriesInThisCategory)));


            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "GetTileInfo"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetTileInfo)));


            HarmonyInstance.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "GetTileStats"),
                prefix: new HarmonyMethod(this.GetType(), nameof(GetTileStats)));

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
               original: AccessTools.Constructor(Type.GetType("TinyZoo.Z_PenInfo.MainBar.SimpleBuildingRenderer, LetsBuildAZoo"), new Type[]{ typeof(TILETYPE), typeof(int)}),
               postfix: new HarmonyMethod(this.GetType(), nameof(SimpleBuildingRenderer)));


            HarmonyInstance.Patch(
               original: AccessTools.Method(Type.GetType("TinyZoo.Z_OverWorld.SpawnAnimations.CascadeSpawner, LetsBuildAZoo"), "DoCascadeForBuildingorTree"),
               prefix: new HarmonyMethod(this.GetType(), nameof(DoCascadeForBuildingorTree)));


            //DoCascadeForBuildingorTree

            //
            LoadContentPacks(helper.GetContentPacks());
        }


        internal static bool DoCascadeForBuildingorTree(object tilerenderer)
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
                        topLayer.DrawOrigin.Y = topLayer.DrawOrigin.Y / 2f;
                    }
                }
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
                    if(!hs.Contains((TILETYPE)definition.GetTileId()))
                        hs.Add((TILETYPE)definition.GetTileId());
            }
        }

        public static bool TileStatsName(object __instance, ref string __result)
        {
            int id = (int) Reflection.GetFieldValue<StringID>("__Name", __instance);

            if (TryGetBuildingDefinitionFromTileId(id, out BuildingDefinition definition))
            {
                __result = definition.Name;
                return false;
            }

            return true;
        }

        public void LoadContentPacks(IEnumerable<IModHelper> packs)
        {
            foreach(var pack in packs)
            {
                var content = pack.Content.LoadJson<Content>("content.json");
                foreach (var building in content.Buildings)
                    Buildings.Add(ReserveID(building, pack));
            }
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
                id =  Config.LastId;
            }

            d.SetTileId(id);

            return d;
        }

        internal static bool GetRenderComponent(TILETYPE tiletype,object parent,bool IsConstructionPreview,bool IsAChild, ref object __result)
        {
            int tileid = (int)tiletype;
            if (Buildings.FirstOrDefault(b => b.GetTileId() == tileid) is BuildingDefinition bd)
            {
                __result = null;
                return false;
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
            if(Buildings.FirstOrDefault(b => b.GetTileId() == tileid) is BuildingDefinition bd)
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

                if(definition.Cost != -1)
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
            var th = Activator.CreateInstance(Type.GetType("TinyZoo.Utils.TextureHolder, LetsBuildAZoo"), new object[] {});
            Reflection.SetFieldValue("texture",th, texture);

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
            var ti = Activator.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileInfo, LetsBuildAZoo"), new object[] { _BaseRect, b,_HasFullRotation,_Variants,LockToThisRotation,_DrawTexture,_IsRepeatingLargeTexture,TotalRotations, _LightsTexture });
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
            AccessTools.Method(tiledata.GetType(), "AddBuilding", new Type[] {typeof(Rectangle),typeof(Vector2),typeof(int),typeof(int)}).Invoke(tiledata, new object[] { baseRect, new Vector2(X,Y), rotation, maxrotations });
        }

        internal static void AddRotation(object tiledata,Rectangle baseRect, int rotation)
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
                    if(definition.BasicBuilding == -1)
                        AddRotation(tiledata, new Rectangle(definition.TileWidth * i, 0, definition.TileWidth, definition.TileHeight), i);
                    else
                        AddRotation(tiledata, new Rectangle(definition.TileWidth * i, definition.TileHeight - definition.BasicBuilding, definition.TileWidth, definition.BasicBuilding), i);
                }

            if (definition.HasFront || definition.BasicBuilding >= 0)
            {
                for (int i = 0; i < maxrotations; i++)
                    if (definition.BasicBuilding == -1)
                        AddBuilding(tiledata, new Rectangle(definition.FrontTileWidth * i, definition.TileHeight, definition.FrontTileWidth, definition.FrontTileHeight), i, maxrotations, (definition.FrontTileWidth / 2f) + 8f, definition.FrontTileHeight - definition.TileHeight / 2);
                    else
                        AddBuilding(tiledata, new Rectangle(definition.FrontTileWidth * i, 0, definition.FrontTileWidth, definition.FrontTileHeight), i, maxrotations, (definition.FrontTileWidth / 2f) + 8f, definition.FrontTileHeight - definition.TileHeight / 2);
            }
        }

        internal static bool GetTileInfo(TILETYPE tiletype, ref object __result)
        {
            Array tinfoCheck = (Array) AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").GetValue(null);

            if (tinfoCheck == null || tinfoCheck.Length < MinimumID * 2)
            {
                var newArray = Array.CreateInstance(Type.GetType("TinyZoo.Tile_Data.TileInfo, LetsBuildAZoo"), MinimumID * 2);
                
                if(tinfoCheck != null)
                for (int i = 0; i < tinfoCheck.Length; i++)
                    newArray.SetValue(tinfoCheck.GetValue(i), i);

                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, newArray);
            }

            int current = (int)tiletype;

            if (TryGetBuildingDefinitionFromTileId(current, out BuildingDefinition definition))
            {
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
                    __result = GetNewTileInfo(new Rectangle(0, 0, definition.TileWidth, definition.TileHeight), GetBuildingType(definition.BuildingType), false, 1, 0, texture, false, definition.Rotations, null);
                else
                    __result = GetNewTileInfo(new Rectangle(0, definition.FrontTileHeight, definition.TileWidth, definition.TileHeight), GetBuildingType(definition.BuildingType), false, 1, 0, texture, false, definition.Rotations, null);

                if (!definition.IsVolumeFloor)
                {
                    AddRotations(__result, definition, definition.Rotations);
                }

                tinfo.SetValue(__result, current);
                AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, tinfo);

                if (definition.IsVolumeFloor)
                {
                    SetFloorVolumeTileData((TILETYPE)definition.GetTileId(), texture);
                    tinfo = (Array)AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").GetValue(null);
                    AccessTools.Field(Type.GetType("TinyZoo.Tile_Data.TileData, LetsBuildAZoo"), "tileinfo").SetValue(null, tinfo);
                    __result = tinfo.GetValue(current);
                }

                return false;
            }

            return true;
        }

        internal static void GetEntriesInThisCategory(object category, ref List<TILETYPE> __result)
        {
            foreach (var building in Buildings.Where(b => b.Category == category.ToString()))
                if (!__result.Contains((TILETYPE)building.GetTileId()))
                    __result.Add((TILETYPE)building.GetTileId());
        }
   }
}
