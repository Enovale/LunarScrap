using BepInEx;
using System.Linq;
using RoR2;

namespace LunarScrap
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class LunarScrapPlugin : BaseUnityPlugin
    {
        public const string ModName = "LunarScrap";
        public const string ModGuid = "com.Windows10CE.LunarScrap";
        public const string ModVer = "1.0.0";

        public void Awake()
        {
            SceneDirector.onGenerateInteractableCardSelection += (director, selection) =>
            {
                var dupCategory = selection.categories.First(x => x.name == "Duplicator");

                if (dupCategory.cards.Length > 0)
                {
                    var copyCard = dupCategory.cards[0];
                    var copySpawnCard = copyCard.spawnCard;

                    var lunarCard = new DirectorCard()
                    {
                        spawnCard = new SpawnCard
                        {
                            name = "DuplicatorLunar",
                            directorCreditCost = 1,
                            forbiddenFlags = copySpawnCard.forbiddenFlags,
                            hullSize = copySpawnCard.hullSize,
                            nodeGraphType = copySpawnCard.nodeGraphType,
                            occupyPosition = copySpawnCard.occupyPosition,
                            prefab = UnityEngine.Object.Instantiate(copySpawnCard.prefab),
                            requiredFlags = copySpawnCard.requiredFlags,
                            sendOverNetwork = copySpawnCard.sendOverNetwork
                        },
                        selectionWeight = 1000,
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
        }
    }
}
