using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using ModLoader.Events;
using System.Linq;
using System.Collections.Generic;
using ModLoader.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks;
using TinyZoo.Tile_Data;

namespace ModLoader
{
    public class ModApi
    {
      

        public static ModApi Singleton { get; private set; }

        internal EventManager EventManager { get; private set; }

        internal Harmony harmony;
        internal ModApiConfig config;
        internal ModHelper modHelper;

        internal static ModHelper ApiHelper => Singleton.modHelper;

        const string CONFIG_FILENAME = "0ModApi.json";
        const string MODFOLDER = "Mods";
        const string APIVERSION = "1.1.0";

        internal ModVersion ApiVersion = new ModVersion(APIVERSION);
        public static bool Initialized = false;

        public static bool LoadedMods = false;

        public static Game Game1 = null;

        private const UInt32 StdOutputHandle = 0xFFFFFFF5;
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

        [DllImport("kernel32.dll")]
        static extern int FreeConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [STAThread]
        public static void CreateConsole()
        {
            AllocConsole();
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = new StreamWriter(Console.OpenStandardOutput());

              ostrm = new FileStream("ModApi.log", FileMode.OpenOrCreate, FileAccess.Write);
              writer = new LogWriter(ostrm, oldOut);
           
            Console.SetOut(writer);
            Console.Title = "LBAM - Mod API - Lets Build A Mod " + APIVERSION;

            Thread inputThread = new Thread(() =>
            {
                while (true)
                {
                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    ApiHelper.Console.Log(input);
                    EventManager.TriggerConsoleEvent(input);
                }
            });
            inputThread.Start();
        }

        public void ExportTextures()
        {
            Game __instance = (Game) AccessTools.Field(typeof(Game), "_instance").GetValue(null);


            foreach (var f in Type.GetType("TinyZoo.AssetContainer, LetsBuildAZoo").GetFields(BindingFlags.NonPublic | BindingFlags.Static))
            {

                if (f.FieldType == typeof(Texture2D))
                {
                    var texture = f.GetValue(null);

                    try
                    {
                        if (!(texture is Texture2D))
                            texture = __instance.Content.Load<Texture2D>(f.Name);
                    }
                    catch
                    {

                    }

                    if (texture is Texture2D tex)
                    {

                        string filename = f.Name + ".png";
                        ApiHelper.Console.Info(f.Name + ":" + tex.Width + ":" + tex.Height);
                        using (var fs = File.Create(filename))
                            tex.SaveAsPng(fs, tex.Width, tex.Height);

                        modHelper.Console.Success("Exported.. " + f.Name, "Success");
                    }
                    else
                        modHelper.Console.Success("Exporting.. " + f.Name, "Failed");
                }
            }
        }

        public void Start()
        {
            modHelper = new ModHelper(new ModManifest()
            {
                Author = "Platonymous",
                Name = "Api",
                Id = "0ModApi",
                Folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            });

            /*
            UnlockAllResearchOnGridRenderer
            UnlockAllBuildingsHack
                        */
            config = modHelper.Content.LoadJson<ModApiConfig>(CONFIG_FILENAME, new ModApiConfig(), true);
            bool ingoreConsole = false;
            if (!ingoreConsole && config.Console)
                CreateConsole();
                
            EventManager.Singleton.Init();
            EventManager.Singleton.GameInitialized += Singleton_GameInitialized;

            if (config.Verbose)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    modHelper.Console.Warn("Verbose Log active");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception ex)
                {
                    modHelper.Console.Error(ex.Message);
                    modHelper.Console.Trace(ex.StackTrace);
                }
            }

            modHelper.Console.Announcement("Loading Mods...");

            modHelper.Console.AddConsoleCommand("export", (s, p) =>
            {
                modHelper.Console.Info("Exporting Textures");
                ExportTextures();
            });

            EventManager.Initialize();

            harmony.Patch(
                original: AccessTools.Method(Type.GetType("TinyZoo.Game1, LetsBuildAZoo"), "Update"),
                prefix: new HarmonyMethod(this.GetType(),nameof(StartLoad)));
       }

        public static void StartLoad(Game __instance)
        {
            if (LoadedMods)
                return;

            Game1 = __instance;
            Singleton.LoadMods();

            LoadedMods = true;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if(args.Name.Contains("LBAM"))
                return Assembly.GetExecutingAssembly();

            return null;
        }


        internal string GetRelativePath(string path)
        {
            return new Uri(path).AbsolutePath.Replace(new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).AbsolutePath, "").Substring(1);
        }

        public void LoadMods()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve; ;

            JsonSerializer serializer = new JsonSerializer();

            if (!Directory.Exists(MODFOLDER))
                Directory.CreateDirectory(MODFOLDER);

            DirectoryInfo modInfo = new DirectoryInfo(MODFOLDER);
            List<ModManifest> mods = new List<ModManifest>();
            List<ModManifest> contentPacks = new List<ModManifest>();

            foreach (var directory in modInfo.EnumerateDirectories())
            {
                if (directory.GetFiles("manifest.json") is FileInfo[] files && files.Length > 0 && files[0] is FileInfo manifest)
                {
                    ModManifest m;
                    using (TextReader textreader = new StreamReader(manifest.OpenRead()))
                    using (JsonReader reader = new JsonTextReader(textreader))
                        m = serializer.Deserialize<ModManifest>(reader);

                    if (m == null)
                        continue;

                    if (string.IsNullOrEmpty(m.Id))
                        m.Id = m.Name;

                    if (m.IsMod)
                        mods.Add(m);

                    if (m.IsContentPack)
                        contentPacks.Add(m);

                    m.Folder = GetRelativePath(Path.GetDirectoryName(manifest.FullName));
                }
            }

            foreach (var cp in contentPacks.Where(c => c.ContentPackFor.Equals("Content", StringComparison.OrdinalIgnoreCase)))
            {
                modHelper.ContentPacks.Add(new ModHelper(cp));
                LogContentModLoad(modHelper.Manifest, true);
            }

            foreach (var m in mods)
            {
                var file = Path.Combine(m.Folder, m.EntryFile);

                Assembly a = Assembly.LoadFrom(file);

                if (a == null)
                {
                    LogModLoad(m, false);
                    continue;
                }

                AppDomain.CurrentDomain.Load(a.GetName());

                Type entryType = a.GetTypes().FirstOrDefault(t => t is IMod);

                if (entryType == null && !string.IsNullOrWhiteSpace(m.EntryMethod))
                    entryType = a.GetTypes().FirstOrDefault(t => t.GetMethod(m.EntryMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) is MethodInfo);

                if (entryType == null)
                {
                    LogModLoad(m, false);
                    continue;
                }

                var v = new ModVersion(m.MinimumApiVersion);

                if (!v.IsLowerOrEqualTo(ApiVersion))
                {
                    LogModLoad(m, false, m.Name + " requires Api Version " + m.MinimumApiVersion + " or higher.");
                    continue;
                }

                ModHelper h = new ModHelper(m);

                foreach (var cp in contentPacks.Where(c => c.ContentPackFor.Equals(m.Id, StringComparison.OrdinalIgnoreCase)))
                    h.ContentPacks.Add(new ModHelper(cp));

                if (entryType.GetMethod(m.EntryMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) is MethodInfo entryMethod)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(entryType);
                        var modp = typeof(IModHelper).FullName;
                        var entryParam = entryMethod.GetParameters().Select(p => p.ParameterType.FullName).FirstOrDefault();
                        object usep = null;
                        if (entryParam != null && entryParam == modp)
                            usep = h;

                        entryMethod.Invoke(instance, new object[] { usep });
                    }
                    catch (Exception ex)
                    {
                        h.Console.Error(ex.Message);
                        h.Console.Trace(ex.StackTrace);
                        LogModLoad(m, false);
                        continue;
                    }
                }

                LogModLoad(m, true);

                foreach (var c in h.ContentPacks)
                    LogContentModLoad(c.Manifest, true);

            }

           
        }

        internal void LogModLoad(IModManifest m, bool success, string message = "")
        {
            string modString = $"{m.Name} ({m.Version}) by {m.Author}";

            if (success)
                modHelper.Console.Success(modString, "Success");
            else
                modHelper.Console.Failure(modString, "Failed");

            if(!string.IsNullOrEmpty(message))
                modHelper.Console.Error(message);
        }

        internal void LogContentModLoad(IModManifest m, bool success)
        {
            string modString = $"ContentPack: {m.Name} ({m.Version}) by {m.Author}";

            if (success)
                modHelper.Console.Log(modString, "Loaded");
            else
                modHelper.Console.Failure(modString, "Failed");
        }

        public ModApi()
        {
            Singleton = this;
            
            var game1 = Type.GetType("TinyZoo.Game1, LetsBuildAZoo");
            
            harmony = new Harmony("LBAM.Main");

            harmony.Patch(
                    original: AccessTools.Method(game1, "Update"),
                    prefix: new HarmonyMethod(GetType().GetMethod(nameof(FlushConsole), BindingFlags.Public | BindingFlags.Static))
                    );


            
            
        }

        public static void FlushConsole()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Out?.Flush();
            Console.ForegroundColor = ConsoleColor.White;

        }


        private void Singleton_GameInitialized(object sender, Events.GameInitializedEventArgs e)
        {
            modHelper.Console.Info("Game Initialized");
        }

    }
}
