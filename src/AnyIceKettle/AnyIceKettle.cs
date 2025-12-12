using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSerialization;
using HarmonyLib;
using SanchozzONIMods.Lib;
using PeterHan.PLib.Detours;
using STRINGS;

namespace AnyIceKettle
{
    using static Patches;
    using option = FewOptionSideScreen.IFewOptionSideScreen.Option;
    using handler = EventSystem.IntraObjectHandler<AnyIceKettle>;

    [SerializationConfig(MemberSerialization.OptIn)]
    public class AnyIceKettle : KMonoBehaviour, FewOptionSideScreen.IFewOptionSideScreen
    {
        private static readonly handler OnCopySettingsDelegate = new((cmp, data) => cmp.OnCopySettings(data));

        internal static readonly IDetouredField<IceKettle.Instance, Element> ElementToMelt
            = PDetours.DetourField<IceKettle.Instance, Element>("elementToMelt");

        private static readonly System.Action<IceKettleWorkable, List<GameObject>> RestoreStoredItemsInteractions
            = typeof(IceKettleWorkable).Detour<System.Action<IceKettleWorkable, List<GameObject>>>("RestoreStoredItemsInteractions");

        private static Element[] IceOres;
        private static SimHashes[] outputLiquids;

        [Serialize]
        private Tag chosenIce;

        private Storage fuelStorage;
        private Storage kettleStorage;
        private Storage outputStorage;
        private ManualDeliveryKG fuel_mdkg;
        private ManualDeliveryKG ice_mdkg;

#pragma warning disable CS0649
        [MySmiReq]
        private IceKettle.Instance kettle;

        [MyCmpReq]
        private IceKettleWorkable workable;

        [MyCmpAdd]
        private CopyBuildingSettings copy;
#pragma warning restore CS0649

        protected override void OnPrefabInit()
        {
            if (IceOres == null)
            {
                var ores = ElementLoader.FindElements(element => element.IsSolid && element.HasTag(GameTags.IceOre));
                if (ModOptions.Instance.melt_resin)
                    ores.Add(ElementLoader.FindElementByHash(SimHashes.SolidResin));
                if (ModOptions.Instance.melt_gunk)
                    ores.Add(ElementLoader.FindElementByHash(SimHashes.Gunk));
                if (ModOptions.Instance.melt_phytooil)
                    ores.Add(ElementLoader.FindElementByHash(SimHashes.FrozenPhytoOil));
                ores.RemoveAll(element => element == null);
                IceOres = ores.ToArray();
                outputLiquids = ores.Select(ore => ore.highTempTransitionTarget).Distinct().ToArray();
            }
            base.OnPrefabInit();
            chosenIce = IceKettleConfig.TARGET_ELEMENT_TAG;
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // same as IceKettle.Instance constructor
            var storages = GetComponents<Storage>();
            fuelStorage = storages[0];
            kettleStorage = storages[1];
            outputStorage = storages[2];
            var mdkgS = GetComponents<ManualDeliveryKG>();
            foreach (var mdkg in mdkgS)
            {
                if (mdkg.DebugStorage == fuelStorage)
                    fuel_mdkg = mdkg;
                else if (mdkg.DebugStorage == kettleStorage)
                    ice_mdkg = mdkg;
            }
            ice_mdkg.RequestedItemTag = chosenIce;
            var ice = ElementLoader.GetElement(chosenIce);
            ElementToMelt.Set(kettle, ice);
            SetPipedEverythingConsumer();
            SetPipedEverythingDispenser();
            // если юзер отключил настройки
            if (!IceOres.Contains(ice))
                SetChosenIce(IceKettleConfig.TARGET_ELEMENT_TAG);
        }

        private void OnCopySettings(object data)
        {
            var go = data as GameObject;
            if (go != null && go.TryGetComponent(out AnyIceKettle other))
                SetChosenIce(other.chosenIce);
        }

        private void SetChosenIce(Tag newChosenIce)
        {
            if (chosenIce != newChosenIce)
            {
                chosenIce = newChosenIce;
                ice_mdkg.RequestedItemTag = chosenIce;
                kettleStorage.DropAll();
                ElementToMelt.Set(kettle, ElementLoader.GetElement(chosenIce));
                SetPipedEverythingConsumer();
                if (kettle.IsInsideState(kettle.sm.operational.melting.working))
                {
                    kettle.GoTo(kettle.sm.operational.melting.exit);
                    IceKettle.ResetMeltingTimer(kettle);
                }
                // не дропать воду если сейчас наливают
                if (!kettle.IsInsideState(kettle.sm.inUse))
                    DropExcessLiquid();
            }
        }

        public void DropExcessLiquid()
        {
            if (outputStorage.items.Count > 0)
            {
                if (outputStorage.items[0].PrefabID() != ElementToMelt.Get(kettle).highTempTransition.tag)
                {
                    var dropped = ListPool<GameObject, Storage>.Allocate();
                    outputStorage.DropAll(collect_dropped_items: dropped);
                    RestoreStoredItemsInteractions(workable, dropped);
                    dropped.Recycle();
                }
            }
        }

        public option[] GetOptions()
        {
            var list = new List<option>();
            foreach (var ice in IceOres)
            {
                if (ice.tag == IceKettleConfig.TARGET_ELEMENT_TAG || DiscoveredResources.Instance.IsDiscovered(ice.tag))
                {
                    var tooltip = string.Format(CODEX.FORMAT_STRINGS.TRANSITION_LABEL_TO_ONE_ELEMENT,
                        ice.tag.ProperName(), ice.highTempTransition.tag.ProperName());
                    list.Add(new option(ice.tag, ice.tag.ProperName(), Def.GetUISprite(ice.tag), tooltip));
                }
            }
            return list.ToArray();
        }

        public Tag GetSelectedOption() => chosenIce;

        public void OnOptionSelected(option option) => SetChosenIce(option.tag);

        private void SetPipedEverythingConsumer()
        {
            if (PipedEverythingConsumerS != null)
            {
                try
                {
                    foreach (var consumer in GetComponents(PipedEverythingConsumerS))
                    {
                        var traverse = Traverse.Create(consumer);
                        var storage = traverse.Property<Storage>("Storage").Value;
                        if (storage == kettleStorage)
                        {
                            traverse.Field<Tag[]>("tagFilter").Value[0] = chosenIce;
                            traverse.Field<float>("capacityKG").Value = ice_mdkg.capacity;
                            kettleStorage.capacityKg = ice_mdkg.capacity;
                        }
                        else if (storage == fuelStorage)
                        {
                            traverse.Field<Tag[]>("tagFilter").Value[0] = fuel_mdkg.RequestedItemTag;
                            traverse.Field<float>("capacityKG").Value = fuel_mdkg.capacity;
                            fuelStorage.capacityKg = fuel_mdkg.capacity;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
        }

        private void SetPipedEverythingDispenser()
        {
            if (PipedEverythingDispenser != null)
            {
                try
                {
                    foreach (var dispenser in GetComponents(PipedEverythingDispenser))
                    {
                        var traverse = Traverse.Create(dispenser);
                        if (traverse.Property<Storage>("Storage").Value == outputStorage)
                        {
                            traverse.Field<SimHashes[]>("elementFilter").Value = outputLiquids;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Utils.LogExcWarn(e);
                }
            }
        }
    }
}
