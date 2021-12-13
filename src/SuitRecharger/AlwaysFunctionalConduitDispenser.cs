using System;
using PeterHan.PLib.Core;

namespace SuitRecharger
{
    public class AlwaysFunctionalConduitDispenser : ConduitDispenser
    {
        // оригинальный ConduitDispenser каждый раз проверяет подключение трубы
        // и выставляет флаг operational.IsFunctional
        // что нас не устраивает, так как не дает работать без трубы
        // поэтому наследуемся и подавляем вызов оригинального ConduitUpdate
        // заменяя его собственным

        private static readonly IntPtr ConduitUpdate_Ptr;
        private static readonly IntPtr Dispense_Ptr;
        private readonly Action<float> ConduitUpdate;
        private readonly Action<float> Dispense;

#pragma warning disable CS0649
        [MyCmpReq]
        private Operational _operational;
#pragma warning restore CS0649

        static AlwaysFunctionalConduitDispenser()
        {
            var methodInfo = typeof(ConduitDispenser).GetMethodSafe(nameof(ConduitUpdate), false, PPatchTools.AnyArguments);
            if (methodInfo == null)
                PUtil.LogError("ConduitDispenser.ConduitUpdate method not found.");
            else
                ConduitUpdate_Ptr = methodInfo.MethodHandle.GetFunctionPointer();
            methodInfo = typeof(ConduitDispenser).GetMethodSafe(nameof(Dispense), false, PPatchTools.AnyArguments);
            if (methodInfo == null)
                PUtil.LogError("ConduitDispenser.Dispense method not found.");
            else
                Dispense_Ptr = methodInfo.MethodHandle.GetFunctionPointer();
        }

        AlwaysFunctionalConduitDispenser()
        {
            ConduitUpdate = (Action<float>)Activator.CreateInstance(typeof(Action<float>), this, ConduitUpdate_Ptr);
            Dispense = (Action<float>)Activator.CreateInstance(typeof(Action<float>), this, Dispense_Ptr);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            // удалить вызов base.ConduitUpdate
            GetConduitManager().RemoveConduitUpdater(ConduitUpdate);
            GetConduitManager().AddConduitUpdater(FunctionalConduitUpdate, ConduitFlowPriority.Dispense);
        }

        protected override void OnCleanUp()
        {
            GetConduitManager().RemoveConduitUpdater(FunctionalConduitUpdate);
            base.OnCleanUp();
        }

        private void FunctionalConduitUpdate(float dt)
        {
            blocked = false;
            if (isOn && _operational.IsFunctional)
            {
                Dispense(dt);
            }
        }
    }
}
