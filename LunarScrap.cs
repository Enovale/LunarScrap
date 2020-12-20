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
    public static class LunarScrap
    {
        public static ItemDef LunarScrapDef { get; private set; }
        public static ItemIndex LunarScrapIndex { get; private set; }

        public static void Init()
        {
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
        }
    }
}
