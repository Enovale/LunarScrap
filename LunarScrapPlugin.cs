using BepInEx;
using System.Linq;
using RoR2;
using R2API;
using R2API.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

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
        public const string ModVer = "1.0.0";

        public static ItemDef LunarScrapDef { get; private set; }
        public static ItemIndex LunarScrapIndex { get; private set; }

        public void Awake()
        {
            SceneDirector.onGenerateInteractableCardSelection += (director, selection) =>
            {
                DirectorCardCategorySelection.Category dupCategory = selection.categories.FirstOrDefault(x => x.name == "Duplicator");

                if ((dupCategory.cards?.Length ?? 0) > 0)
                {
                    var copyCard = dupCategory.cards[0];
                    var copySpawnCard = copyCard.spawnCard;

                    var lunarCard = new DirectorCard()
                    {
                        spawnCard = new SpawnCard
                        {
                            name = "DuplicatorLunar",
                            directorCreditCost = 10,
                            forbiddenFlags = copySpawnCard.forbiddenFlags,
                            hullSize = copySpawnCard.hullSize,
                            nodeGraphType = copySpawnCard.nodeGraphType,
                            occupyPosition = copySpawnCard.occupyPosition,
                            prefab = UnityEngine.Object.Instantiate(copySpawnCard.prefab),
                            requiredFlags = copySpawnCard.requiredFlags,
                            sendOverNetwork = copySpawnCard.sendOverNetwork
                        },
                        selectionWeight = 1,
                        spawnDistance = copyCard.spawnDistance,
                        allowAmbushSpawn = copyCard.allowAmbushSpawn,
                        preventOverhead = copyCard.preventOverhead,
                        minimumStageCompletions = copyCard.minimumStageCompletions,
                        requiredUnlockable = copyCard.requiredUnlockable,
                        forbiddenUnlockable = copyCard.forbiddenUnlockable
                    };

                    lunarCard.spawnCard.prefab.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.LunarItemOrEquipment;
                    lunarCard.spawnCard.prefab.GetComponent<ShopTerminalBehavior>().itemTier = ItemTier.Lunar;

                    var newCardArray = new DirectorCard[dupCategory.cards.Length + 1];
                    dupCategory.cards.CopyTo(newCardArray, 0);
                    newCardArray[newCardArray.Length - 1] = lunarCard;
                    dupCategory.cards = newCardArray;

                    for (int i = 0; i < selection.categories.Length; i++)
                    {
                        if (selection.categories[i].name == "Duplicator")
                            selection.categories[i] = dupCategory;
                    }
                }
            };

            var assetBundle = UnityEngine.AssetBundle.LoadFromMemory(Properties.Resources.lunarscrap);
            ResourcesAPI.AddProvider(new AssetBundleResourcesProvider("@LunarScrap", assetBundle));

            LanguageAPI.Add("ITEM_SCRAPLUNAR_NAME", "Item Scrap, Lunar");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_LORE", "No notes found.");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_DESC", "Does nothing. Prioritized when used with 3D printers.");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_PICKUP", "Does nothing. Prioritized when used with 3D printers.");

            LunarScrapDef = new ItemDef()
            {
                canRemove = true,
                descriptionToken = "ITEM_SCRAPLUNAR_DESC",
                hidden = false,
                loreToken = "ITEM_SCRAPLUNAR_LORE",
                name = "ScrapLunar",
                nameToken = "ITEM_SCRAPLUNAR_NAME",
                pickupIconPath = "@LunarScrap:Assets/LunarScrap.png",
                pickupModelPath = "Prefabs/PickupModels/PickupScrap",
                pickupToken = "ITEM_SCRAPLUNAR_PICKUP",
                tier = ItemTier.Lunar,
                tags = new ItemTag[] { ItemTag.Scrap, ItemTag.WorldUnique },
                unlockableName = ""
            };

            LunarScrapIndex = ItemAPI.Add(new CustomItem(LunarScrapDef, new ItemDisplayRule[] { }));
            
            IL.RoR2.PickupPickerController.SetOptionsFromInteractor += (ILContext il) =>
            {
                ILCursor c = new ILCursor(il);

                c.GotoNext(MoveType.Before,
                    x => x.MatchLdloc(4),
                    x => x.MatchLdfld<ItemDef>("tier"),
                    x => x.MatchLdcI4(3)
                );

                c.RemoveRange(4);
            };

            IL.EntityStates.Scrapper.ScrappingToIdle.OnEnter += (ILContext il) =>
            {
                ILCursor c = new ILCursor(il);

                c.GotoNext(MoveType.After,
                    x => x.MatchLdstr("ItemIndex.ScrapYellow"),
                    x => x.MatchCall(typeof(PickupCatalog), nameof(PickupCatalog.FindPickupIndex)),
                    x => x.MatchStloc(0)
                );
                c.Emit(OpCodes.Ldloc_1);
                var toLabel = c.Previous;
                c.Emit(OpCodes.Ldloc_0);
                c.EmitDelegate<Func<ItemDef, PickupIndex, PickupIndex>>((ItemDef item, PickupIndex orig) =>
                {
                    if (item.tier == ItemTier.Lunar)
                        return PickupCatalog.FindPickupIndex(LunarScrapIndex);
                    return orig;
                });
                c.Emit(OpCodes.Stloc_0);
                foreach (var inLabel in c.IncomingLabels)
                {
                    inLabel.Target = toLabel;
                }
            };

            On.RoR2.CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.PayCost += (orig, costTypeDef, context) =>
            {
                var inventory = context.activator.GetComponent<CharacterBody>().inventory;
                if (inventory.GetItemCount(LunarScrapIndex) > 0)
                {
                    inventory.RemoveItem(LunarScrapIndex, 1);
                    context.results.itemsTaken.Add(LunarScrapIndex);
                    return;
                }
                orig(costTypeDef, context);
            };

#if DEBUG
            On.RoR2.CharacterBody.OnInventoryChanged += (orig, self) =>
            {
                orig(self);
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(LunarScrapIndex), self.transform.position, new UnityEngine.Vector3());
            };
#endif
        }
    }
}
