using AnalyticsWrapper;
using ModLoader;
using Spring.Comms;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using HarmonyLib;
using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Run()
    {
        if(!APIEntryClass.loaded)
       APIEntryClass.InitApi();
    }

    
}

namespace AnalyticsWrapper
{
    public class AnalyticsEventLog
    {
        public static void TrackAnaylticsEvent(string eventName, Dictionary<string, string> properties)
        {
        }

        public static void TrackAnaylticsEvent(string EventName, params string[][] stringdata)
        {
           
        }

        public static void TrackAnaylticsEvent(string eventName, string itemname, string itemvalue)
        {
           
        }
    }

    public static class APIEntryClass
    {
        public static bool loaded = false;

        public static ModApi Api;

        public static void InitApi()
        {
            if (!loaded)
            {
                    Api = new ModApi();
                    Api.Start();
                    loaded = true;
            }
            loaded = true;
        }
    }
}