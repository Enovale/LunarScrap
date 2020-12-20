using BepInEx;
using System.Linq;
using RoR2;
using R2API;
using R2API.Utils;

namespace LunarScrap
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod)]

    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(ResourcesAPI), nameof(LanguageAPI))]
    public class LunarScrapPlugin : BaseUnityPlugin
    {
        public const string ModName = "LunarScrap";
        public const string ModGuid = "com.Windows10CE.LunarScrap";
        public const string ModVer = "1.1.0";



        public void Awake()
        {
            LunarScrap.Init();
            LunarPrinter.Init();

#if DEBUG
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(LunarScrap.LunarScrapIndex), self.transform.position, new UnityEngine.Vector3());
            };
#endif
        }
    }
}
