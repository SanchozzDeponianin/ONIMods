using static MechanicsStation.MechanicsStationConfig;

namespace MechanicsStation
{
    public class MechanicsStation : KMonoBehaviour
    {
        private static readonly KAnimHashedString oreSymbolHash = new KAnimHashedString("rock");
        private static readonly EventSystem.IntraObjectHandler<MechanicsStation> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<MechanicsStation>((MechanicsStation component, object data) => component.OnStorageChange(data));

#pragma warning disable CS0649
        [MyCmpReq]
        private Storage storage;

        [MyCmpReq]
        private KBatchedAnimController kbac;

        [MyCmpReq]
        private SymbolOverrideController soc;
#pragma warning restore CS0649

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            OnStorageChange(null);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            base.OnCleanUp();
        }

        private void OnStorageChange(object data)
        {
            var ore = storage.FindFirst(MATERIAL_FOR_TINKER);
            if (ore != null)
            {
                var oreAnimFile = ore.GetComponent<KBatchedAnimController>().CurrentAnim;
                var oreAnimSymbol = oreAnimFile.animFile.build.symbols[0];
                soc.AddSymbolOverride(oreSymbolHash, oreAnimSymbol, 5);
                kbac.SetSymbolVisiblity(oreSymbolHash, true);
            }
            else
            {
                kbac.SetSymbolVisiblity(oreSymbolHash, false);
            }
        }
    }
}
