using System;

namespace ModLoader.Config
{
    public interface IConfigHelper
    {
        T LoadConfig<T>() where T : class;

        void SaveConfig<T>(T config) where T : class;

    }
}
