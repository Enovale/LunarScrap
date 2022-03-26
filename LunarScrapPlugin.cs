using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using R2API.Utils;
using RoR2.ContentManagement;

namespace LunarScrap
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [NetworkCompatibility]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]
    [HarmonyPatch]
    public class LunarScrapPlugin : BaseUnityPlugin
    {
        public const string ModName = "LunarScrap";
        public const string ModGuid = "com.Windows10CE.LunarScrap";
        public const string ModVer = "2.1.0";

        new internal static ManualLogSource Logger;

        internal static Harmony HarmonyInstance;

        internal static ConfigEntry<bool> BypassRemovableCheck;
        internal static ConfigEntry<int> CreditCost;
        internal static ConfigEntry<int> SelectionWeight;

        public void Awake()
        {
            Logger = base.Logger;

            BypassRemovableCheck = Config.Bind("Workarounds", nameof(BypassRemovableCheck), false, "Some lunar items say they can't be removed by scrappers, this will bypass that for lunar items. (Don't use this if you aren't sure you need to.)");
            CreditCost = Config.Bind("LunarPrinter", nameof(CreditCost), 10, "Credit cost for the director, change this if you think the printer spawns too little/too much");
            SelectionWeight = Config.Bind("LunarPrinter", nameof(SelectionWeight), 1, "Selection weight for the director, change this if you think the printer spawns too little/too much");

            HarmonyInstance = Harmony.CreateAndPatchAll(typeof(LunarScrapPlugin).Assembly, ModGuid);

            ContentManager.collectContentPackProviders += (Add) => Add(new LunarScrapProvider());
            LunarPrinter.Init();
        }

#if DEBUG
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.OnInventoryChanged))]
        public static void DebugDropLunarPostfix(CharacterBody __instance)
        {
            var def = LunarScrapProvider.LunarScrapDef;
            var otherIndex = ItemCatalog.FindItemIndex("ScrapLunar");
            var index = PickupCatalog.FindPickupIndex(def.itemIndex);
            PickupDropletController.CreatePickupDroplet(index, __instance.transform.position, new UnityEngine.Vector3());
        }
#endif
    }
}
