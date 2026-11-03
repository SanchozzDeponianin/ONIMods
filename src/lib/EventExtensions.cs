using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SanchozzONIMods.Lib
{
    internal static class EventSystemExtensions
    {
        private static readonly ConditionalWeakTable<KMonoBehaviour, List<int>> registry = new();

        public static void SubscribeX(this KMonoBehaviour cmp, GameHashes hash, Action<object> handler)
            => SubscribeX(cmp, (int)hash, handler);

        public static void SubscribeX(this KMonoBehaviour cmp, int hash, Action<object> handler)
        {
            int handle = cmp.Subscribe(hash, handler);
            registry.GetOrCreateValue(cmp).Add(handle);
        }

        public static void SubscribeX<T>(this T cmp, GameHashes hash, Action<T, object> exthandler) where T : KMonoBehaviour
            => SubscribeX(cmp, (int)hash, Unsafe.As<Action<object, object>>(exthandler), cmp);

        public static void SubscribeX(this KMonoBehaviour cmp, GameHashes hash, Action<object, object> exthandler)
            => SubscribeX(cmp, (int)hash, exthandler, cmp);

        public static void SubscribeX(this KMonoBehaviour cmp, GameHashes hash, Action<object, object> exthandler, object context)
            => SubscribeX(cmp, (int)hash, exthandler, context);

        public static void SubscribeX(this KMonoBehaviour cmp, int hash, Action<object, object> exthandler, object context)
        {
            int handle = cmp.Subscribe(hash, exthandler, context);
            registry.GetOrCreateValue(cmp).Add(handle);
        }

        public static void UnsubscribeAll(this KMonoBehaviour cmp)
        {
            if (registry.TryGetValue(cmp, out List<int> handles))
            {
                foreach (int handle in handles)
                    cmp.Unsubscribe(handle);
                registry.Remove(cmp);
            }
        }
    }
}