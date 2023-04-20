using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using ModLoader.Content;

namespace ModLoader.Events
{
    internal class EventManager : IEventHelper
    {
        internal static EventManager _singleton;
        public static EventManager Singleton
        {
            get
            {
                if (_singleton == null)
                    _singleton = new EventManager();
                return _singleton;
            }
        }

        public event EventHandler<GameInitializedEventArgs> GameInitialized;

        public event EventHandler<UpdateTickEventArgs> UpdateTick;

        public event EventHandler<UpdateTickEventArgs> UpdateTicked;

        public event EventHandler<DrawEventArgs> BeforeDraw;

        public event EventHandler<DrawEventArgs> AfterDraw;

        public event EventHandler<ConsoleInputReceivedEventArgs> ConsoleInputReceived;

        internal void Init()
        {
            try
            {

                Harmony harmony = new Harmony("LBAM.EventManager");
                var game1 = Type.GetType("TinyZoo.Game1, LetsBuildAZoo");
            
                harmony.Patch(
                    original: AccessTools.Method(typeof(Game), "Tick"),
                    prefix: new HarmonyMethod(GetType().GetMethod(nameof(Tick), BindingFlags.NonPublic | BindingFlags.Static)),
                    postfix: new HarmonyMethod(GetType().GetMethod(nameof(Tick2), BindingFlags.NonPublic | BindingFlags.Static))
                    );

                harmony.Patch(
                    original: AccessTools.Method(game1, "Draw"),
                    prefix: new HarmonyMethod(GetType().GetMethod(nameof(BeforeDrawPatch), BindingFlags.NonPublic | BindingFlags.Static)),
                    postfix: new HarmonyMethod(GetType().GetMethod(nameof(AfterDrawPatch), BindingFlags.NonPublic | BindingFlags.Static))
                    );

            }
            catch(Exception e)
            {
                ModApi.Singleton.modHelper.Console.Error("2:" + e.Message);
                ModApi.Singleton.modHelper.Console.Trace(e.StackTrace);
            }


        }

        internal static void BeforeDrawPatch()
        {
            Singleton.BeforeDraw?.Invoke(null, new DrawEventArgs());
        }

        internal static void AfterDrawPatch()
        {
            Singleton.AfterDraw?.Invoke(null, new DrawEventArgs());
        }

        internal static void Tick(Game __instance)
        {
            Singleton.UpdateTick?.Invoke(null, new UpdateTickEventArgs(__instance));
        }

        internal static void Tick2(Game __instance)
        {
            Singleton.UpdateTicked?.Invoke(null, new UpdateTickEventArgs(__instance));
        }


        internal static void Initialize()
        {
            Singleton.GameInitialized?.Invoke(null, new GameInitializedEventArgs());
        }
        
        internal static void TriggerConsoleEvent(string input) => Singleton.ConsoleInputReceived?.Invoke(null, new ConsoleInputReceivedEventArgs(input));

    }
}
