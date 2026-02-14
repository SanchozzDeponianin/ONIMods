using KSerialization;
using UnityEngine;

namespace NoManualDelivery
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class Automatable2 : Automatable
    {
        [Serialize]
        [SerializeField]
        private bool automationHold = true; // "умный" режим

        [SerializeField]
        public bool showInUI = true;        // показывать УИ

        [SerializeField]
        public bool allowHold = true;       // разрешить "умный" режим в УИ

        public override void OnPrefabInit()
        {
            SetAutomationOnly(false);
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            SetAutomation(GetAutomationOnly(), automationHold);
        }

        public override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
            base.OnCleanUp();
        }

        private static new readonly EventSystem.IntraObjectHandler<Automatable2> OnCopySettingsDelegate
            = new((cmp, data) => cmp.OnCopySettings(data));

        private new void OnCopySettings(object data)
        {
            if (((GameObject)data).TryGetComponent(out Automatable automatable))
            {
                if (automatable is Automatable2 other)
                    SetAutomation(other.GetAutomationOnly(), other.GetAutomationHold());
                else
                    SetAutomation(automatable.GetAutomationOnly(), false);
                Patches.UpdateSweepBotStationStorage(this);
            }
        }

        public bool GetAutomationHold() => automationHold;

        public void SetAutomation(bool only, bool hold)
        {
            SetAutomationOnly(only);
            automationHold = !only && allowHold && hold;
        }
    }
}
