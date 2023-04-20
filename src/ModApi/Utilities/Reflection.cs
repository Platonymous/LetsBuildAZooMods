using HarmonyLib;
using System;

namespace ModLoader.Utilities
{
    public static class Reflection
    {
        public static T GetStaticFieldValue<T>(string type, string field)
        {
            type = type.Replace("TinyZoo.", "");
            return (T)AccessTools.Field(Type.GetType("TinyZoo." + type + ", LetsBuildAZoo"), field).GetValue(null);
        }

        public static T GetFieldValue<T>(string field, object instance)
        {
            var type = instance.GetType();
            return (T)AccessTools.Field(type, field).GetValue(instance);
        }

        public static void SetStaticFieldValue(string type, string field, object value)
        {
            type = type.Replace("TinyZoo.", "");
            AccessTools.Field(Type.GetType("TinyZoo." + type + ", LetsBuildAZoo"), field).SetValue(null, value);
        }

        public static void SetFieldValue(string field, object instance, object value)
        {
            var type = instance.GetType();
            AccessTools.Field(type, field).SetValue(instance, value);
        }
    }
}
