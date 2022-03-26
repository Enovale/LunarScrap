using System;
using System.Collections;
using RoR2;
using RoR2.ContentManagement;
using HarmonyLib;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API;

namespace LunarScrap
{
    [HarmonyPatch]
    public class LunarScrapProvider : IContentPackProvider
    {
        public string identifier => "LunarScrap";

        public static ItemDef LunarScrapDef { get; private set; }

        private static AssetBundle assetBundle;

        internal static ContentPack ContentPack = new ContentPack();

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.lunarscrap);

            LunarScrapDef = ScriptableObject.CreateInstance<ItemDef>();

            LanguageAPI.Add("ITEM_SCRAPLUNAR_NAME", "Item Scrap, Lunar");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_LORE", "No notes found.");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_DESC", "Does nothing. Prioritized when used with 3D printers.");
            LanguageAPI.Add("ITEM_SCRAPLUNAR_PICKUP", "Does nothing. Prioritized when used with 3D printers.");

            LunarScrapDef.canRemove = true;
            LunarScrapDef.descriptionToken = "ITEM_SCRAPLUNAR_DESC";
            LunarScrapDef.hidden = false;
            LunarScrapDef.loreToken = "ITEM_SCRAPLUNAR_LORE";
            LunarScrapDef.name = "ScrapLunar";
            LunarScrapDef.nameToken = "ITEM_SCRAPLUNAR_NAME";
            LunarScrapDef.pickupIconSprite = assetBundle.LoadAsset<Sprite>("Assets/LunarScrap.png");
            LunarScrapDef.pickupModelPrefab = Resources.Load<GameObject>("Prefabs/PickupModels/PickupScrap");
            LunarScrapDef.pickupToken = "ITEM_SCRAPLUNAR_PICKUP";
            LunarScrapDef.tier = ItemTier.Lunar;
            LunarScrapDef.tags = new ItemTag[] { ItemTag.Scrap, ItemTag.WorldUnique };
            LunarScrapDef.unlockableDef = null;

            ContentPack.itemDefs.Add(new ItemDef[] { LunarScrapDef });

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(ContentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(PickupPickerController), nameof(PickupPickerController.SetOptionsFromInteractor))]
        private static void FixPickerMenuIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<ItemTierDef>(nameof(ItemTierDef.canScrap))))
            {
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Ldc_I4_1);
            }

            if (LunarScrapPlugin.BypassRemovableCheck.Value)
            {
                if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld<ItemDef>(nameof(ItemDef.canRemove))))
                {
                    c.Emit(OpCodes.Pop);
                    c.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    LunarScrapPlugin.Logger.LogWarning("Wasn't able to bypass the canRemove check");
                }
            }
            
            LunarScrapPlugin.Logger.LogInfo(il);
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(EntityStates.Scrapper.ScrappingToIdle),
            nameof(EntityStates.Scrapper.ScrappingToIdle.OnEnter))]
        private static void FixScrapperIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdstr("ItemIndex.ScrapYellow"),
                x => x.MatchCall(typeof(PickupCatalog), nameof(PickupCatalog.FindPickupIndex)),
                x => x.MatchStloc(0)
            );
            c.Emit(OpCodes.Ldloc_1);
            var toLabel = c.Previous;
            foreach (var inLabel in c.IncomingLabels)
            {
                inLabel.Target = toLabel;
            }

            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<ItemDef, PickupIndex, PickupIndex>>((ItemDef item, PickupIndex orig) =>
            {
                if (item.tier == ItemTier.Lunar)
                    return PickupCatalog.FindPickupIndex(LunarScrapDef.itemIndex);
                return orig;
            });
            c.Emit(OpCodes.Stloc_0);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper),
            nameof(CostTypeCatalog.LunarItemOrEquipmentCostTypeHelper.PayCost))]
        private static bool FixLunarCostDef(ref CostTypeDef.PayCostContext context)
        {
            var inventory = context.activator.GetComponent<CharacterBody>().inventory;
            if (inventory.GetItemCount(LunarScrapDef) > 0)
            {
                inventory.RemoveItem(LunarScrapDef, 1);
                context.results.itemsTaken.Add(LunarScrapDef.itemIndex);
                return false;
            }

            return true;
        }
    }
}