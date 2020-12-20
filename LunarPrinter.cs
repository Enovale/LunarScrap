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

                    var lunarCard = new DirectorCard()
                    {
                        spawnCard = new SpawnCard
                        {
                            name = "DuplicatorLunar",
#if RELEASE
                            directorCreditCost = 10,
#endif
#if DEBUG
                            directorCreditCost = 1,
#endif
                            forbiddenFlags = copySpawnCard.forbiddenFlags,
                            hullSize = copySpawnCard.hullSize,
                            nodeGraphType = copySpawnCard.nodeGraphType,
                            occupyPosition = copySpawnCard.occupyPosition,
                            prefab = UnityEngine.Object.Instantiate(copySpawnCard.prefab),
                            requiredFlags = copySpawnCard.requiredFlags,
                            sendOverNetwork = copySpawnCard.sendOverNetwork
                        },
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
                        requiredUnlockable = copyCard.requiredUnlockable,
                        forbiddenUnlockable = copyCard.forbiddenUnlockable
                    };
                    lunarCard.spawnCard.prefab.transform.SetPositionAndRotation(new UnityEngine.Vector3(0, -1000, 0), new UnityEngine.Quaternion());

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
        }

        public class LunarDropTable : PickupDropTable
        {
            public override PickupIndex GenerateDrop(Xoroshiro128Plus rng)
            {
                var equip = Run.instance.availableLunarDropList.Where(x => PickupCatalog.GetPickupDef(x).equipmentIndex != EquipmentIndex.None).ToArray();
                return equip[rng.RangeInt(0, equip.Count())];
            }
        }
    }
}
