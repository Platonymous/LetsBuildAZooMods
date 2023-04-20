using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System;
using System.IO;

namespace ModLoader.Content
{
    internal class ContentHelper : IContentHelper
    {
        internal ModHelper helper;

        internal static bool initialized = false;

        internal ContentManager modContent;

        public ContentHelper(ModHelper mod)
        {
            helper = mod;
            Init();
        }

        public void SaveJson<T>(T data, string filename) where T : class
        {
            string file = Path.Combine(helper.Manifest.Folder, filename);
            var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            File.WriteAllText(file, JsonConvert.SerializeObject(data, settings));
        }

            public T LoadJson<T>(string filename, T fallback = null, bool createFileIfMissing = false) where T : class
        {
            string file = Path.Combine(helper.Manifest.Folder, filename);
            JsonSerializer serializer = new JsonSerializer();
            var settings = new JsonSerializerSettings() { Formatting = Formatting.Indented };
            T result = null;
            try
            {
                if (File.Exists(file))
                    using (TextReader textreader = new StreamReader(File.OpenRead(file)))
                    using (JsonReader reader = new JsonTextReader(textreader))
                        result = serializer.Deserialize<T>(reader);
                else
                    result = fallback;

                if (createFileIfMissing && result is T)
                    SaveJson(result, filename);
            }
            catch (Exception ex)
            {
                helper.Console.Error(ex.Message);
                helper.Console.Trace(ex.StackTrace);
            }

            return result;
        }

        public T LoadContent<T>(string assetName, bool fromModFolder = true)
        {
            ContentManager manager = (ContentManager) AccessTools.Field(Type.GetType("TinyZoo.Game1, LetsBuildAZoo"), "News_ContentManager").GetValue(null);
            if (fromModFolder)
            {
                if (modContent == null)
                    modContent = new ModContentManager(manager.ServiceProvider, helper.Manifest.Folder);

                return modContent.Load<T>(assetName);
            }

            try
            {
                return manager.Load<T>(assetName);
            }
            catch (Exception ex)
            {
                helper.Console.Error(ex.Message);
                helper.Console.Trace(ex.StackTrace);
            }

            return default(T);
        }

        public static void Init()
        {
            if (initialized)
                return;

            initialized = true;
        }

    }
}
