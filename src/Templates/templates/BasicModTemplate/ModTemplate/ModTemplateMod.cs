using ModLoader;
using HarmonyLib;

namespace ModTemplate
{
    public class ModTemplateMod : IMod
    {   
        Harmony HarmonyInstance;
        IModHelper Helper;

        public void ModEntry(IModHelper helper)
        {
            Helper = helper;
            HarmonyInstance = new Harmony("{AUTHOR}.ModTemplate");
        }
    }
}
