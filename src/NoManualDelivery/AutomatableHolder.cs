using UnityEngine;

namespace NoManualDelivery
{
    // todo: это можно оптимизировать для избегания лишней аллокации
    public class AutomatableHolder
    {
        private const float ShortTimeout = 2f;
        internal static float LongTimeout = 8f;
        internal static float CurrentTime;

        private float timeout = 0f;
        private float timestamp = CurrentTime;

        public void SetShortTimeout() => timeout = Mathf.Max(timeout, ShortTimeout);
        public void SetLongTimeout() => timeout = LongTimeout;
        public void SetZeroTimeout() => timeout = 0f;

        public void RefreshTimestamp()
        {
            timeout = LongTimeout;
            timestamp = CurrentTime;
        }

        public bool IsTimeOut() => CurrentTime - timestamp >= timeout;
    }
}
