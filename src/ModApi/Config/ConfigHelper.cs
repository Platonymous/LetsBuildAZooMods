using System;

namespace ModLoader.Config
{
    internal class ConfigHelper : IConfigHelper
    {
        IModHelper Helper;

        static bool initialized = false;

        public ConfigHelper(IModHelper helper)
        {
            Helper = helper;
            Init();
        }

        public T LoadConfig<T>() where T : class
        {
            return Helper.Content.LoadJson<T>("config.json", (T) Activator.CreateInstance(typeof(T)), true);
        }

        public void SaveConfig<T>(T config) where T : class
        {
            Helper.Content.SaveJson(config, "config.json");
        }

        
        internal static void Init()
        {
            if (initialized)
                return;


            initialized = true;
        }

    }
}
