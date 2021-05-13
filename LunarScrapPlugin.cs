using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using RoR2.ContentManagement;

namespace LunarScrap
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [HarmonyPatch]

    [BepInDependency(ModCommon.ModCommonPlugin.ModGUID, BepInDependency.DependencyFlags.HardDependency)]
    [ModCommon.NetworkModlistInclude]
    public class LunarScrapPlugin : BaseUnityPlugin
    {
        public const string ModName = "LunarScrap";
        public const string ModGuid = "com.Windows10CE.LunarScrap";
        public const string ModVer = "2.0.0";

        new internal static ManualLogSource Logger;

        internal static Harmony HarmonyInstance;

        public void Awake()
        {
            LunarScrapPlugin.Logger = base.Logger;

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
