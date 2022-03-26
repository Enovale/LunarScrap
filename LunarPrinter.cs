using System.Linq;
using RoR2;
using UnityEngine;

namespace LunarScrap
{
    public static class LunarPrinter
    {
        public static void Init()
        {
            SceneDirector.onGenerateInteractableCardSelection += OnSceneDirectorOnonGenerateInteractableCardSelection;
        }

        private static void OnSceneDirectorOnonGenerateInteractableCardSelection(SceneDirector director, DirectorCardCategorySelection selection)
        {
            var dupCategory = selection.categories.FirstOrDefault(x => x.name == "Duplicator");

            if ((dupCategory.cards?.Length ?? 0) > 0)
            {
                var copyCard = dupCategory.cards[0];
                var copySpawnCard = copyCard.spawnCard;

                var spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                spawnCard.name = "DuplicatorLunar";
                spawnCard.directorCreditCost = LunarScrapPlugin.CreditCost.Value;
                spawnCard.forbiddenFlags = copySpawnCard.forbiddenFlags;
                spawnCard.hullSize = copySpawnCard.hullSize;
                spawnCard.nodeGraphType = copySpawnCard.nodeGraphType;
                spawnCard.occupyPosition = copySpawnCard.occupyPosition;
                spawnCard.prefab = Object.Instantiate(copySpawnCard.prefab);
                spawnCard.requiredFlags = copySpawnCard.requiredFlags;
                spawnCard.sendOverNetwork = copySpawnCard.sendOverNetwork;

                var lunarCard = new DirectorCard()
                {
                    spawnCard = spawnCard,
                    selectionWeight = LunarScrapPlugin.SelectionWeight.Value,
                    spawnDistance = copyCard.spawnDistance,
                    preventOverhead = copyCard.preventOverhead,
                    minimumStageCompletions = copyCard.minimumStageCompletions,
                    requiredUnlockableDef = copyCard.requiredUnlockableDef,
                    forbiddenUnlockableDef = copyCard.forbiddenUnlockableDef
                };
                lunarCard.spawnCard.prefab.transform.SetPositionAndRotation(new Vector3(0, -1000, 0), new Quaternion());

                lunarCard.spawnCard.prefab.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.LunarItemOrEquipment;

                var shop = lunarCard.spawnCard.prefab.GetComponent<ShopTerminalBehavior>();
                shop.itemTier = ItemTier.Lunar;
                shop.dropTable = ScriptableObject.CreateInstance<LunarDropTable>();

                var newCardArray = new DirectorCard[dupCategory.cards.Length + 1];
                dupCategory.cards.CopyTo(newCardArray, 0);
                newCardArray[newCardArray.Length - 1] = lunarCard;
                dupCategory.cards = newCardArray;

                for (var i = 0; i < selection.categories.Length; i++)
                {
                    if (selection.categories[i].name == "Duplicator") 
                        selection.categories[i] = dupCategory;
                }
            }
        }

        public class LunarDropTable : PickupDropTable
        {
            public override int GetPickupCount() =>
                Run.instance.availableLunarItemDropList.Count(x => PickupCatalog.GetPickupDef(x).itemIndex != ItemIndex.None);

            public override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
            {
                var items = Run.instance.availableLunarItemDropList.Where(x => PickupCatalog.GetPickupDef(x).itemIndex != ItemIndex.None).ToArray();
                return items[rng.RangeInt(0, items.Length)];
            }

            public override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
            {
                var items = Run.instance.availableLunarItemDropList.Where(x => PickupCatalog.GetPickupDef(x).itemIndex != ItemIndex.None).ToArray();
                var drops = new PickupIndex[maxDrops];
                var i = 0;
                while (i < maxDrops)
                {
                    var item = items[rng.RangeInt(0, items.Length)];
                    if(!drops.Contains(item)) 
                        drops[i++] = item;
                }

                return drops;
            }
        }
    }
}
