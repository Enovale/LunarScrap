using System.Linq;
using RoR2;
using UnityEngine;

namespace LunarScrap
{
    public static class LunarPrinter
    {
        public static void Init()
        {
            SceneDirector.onGenerateInteractableCardSelection += (director, selection) =>
            {
                DirectorCardCategorySelection.Category dupCategory = selection.categories.FirstOrDefault(x => x.name == "Duplicator");

                if ((dupCategory.cards?.Length ?? 0) > 0)
                {
                    var copyCard = dupCategory.cards[0];
                    var copySpawnCard = copyCard.spawnCard;

                    SpawnCard spawnCard = ScriptableObject.CreateInstance<SpawnCard>();
                    spawnCard.name = "DuplicatorLunar";
#if RELEASE
                    spawnCard.directorCreditCost = 10;
#endif
#if DEBUG
                    spawnCard.directorCreditCost = 1;
#endif
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
#if RELEASE
                        selectionWeight = 1,
#endif
#if DEBUG
                        selectionWeight = 100000,
#endif
                        spawnDistance = copyCard.spawnDistance,
                        allowAmbushSpawn = copyCard.allowAmbushSpawn,
                        preventOverhead = copyCard.preventOverhead,
                        minimumStageCompletions = copyCard.minimumStageCompletions,
                        requiredUnlockableDef = copyCard.requiredUnlockableDef,
                        forbiddenUnlockableDef = copyCard.forbiddenUnlockableDef
                    };
                    lunarCard.spawnCard.prefab.transform.SetPositionAndRotation(new UnityEngine.Vector3(0, -1000, 0), new UnityEngine.Quaternion());

                    lunarCard.spawnCard.prefab.GetComponent<PurchaseInteraction>().costType = CostTypeIndex.LunarItemOrEquipment;

                    var shop = lunarCard.spawnCard.prefab.GetComponent<ShopTerminalBehavior>();
                    shop.itemTier = ItemTier.Lunar;
                    shop.dropTable = ScriptableObject.CreateInstance<LunarDropTable>();

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
        }

        public class LunarDropTable : PickupDropTable
        {
            public override PickupIndex GenerateDrop(Xoroshiro128Plus rng)
            {
                var items = Run.instance.availableLunarDropList.Where(x => PickupCatalog.GetPickupDef(x).itemIndex != ItemIndex.None).ToArray();
                return items[rng.RangeInt(0, items.Count())];
            }
        }
    }
}
