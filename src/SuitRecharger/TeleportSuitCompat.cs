using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using PeterHan.PLib.Core;

namespace SuitRecharger
{
    public static class TeleportSuitCompat
    {
        public static Type TankType;
        public static FieldInfo BatteryCharge;
        public static Func<Component, bool> NeedsRecharging;

        public static void Init()
        {
            try
            {
                TankType = PPatchTools.GetTypeSafe("TeleportSuitMod.TeleportSuitTank");
                if (TankType != null)
                {
                    BatteryCharge = TankType.GetFieldSafe("batteryCharge", false);
                    NeedsRecharging = Unsafe.As<Func<Component, bool>>(TankType.GetMethodSafe("NeedsRecharging", false)
                        ?.CreateDelegate(typeof(Func<,>).MakeGenericType(TankType, typeof(bool))));
                }
            }
            catch (Exception e)
            {
                PUtil.LogExcWarn(e);
            }
        }
    }
}
