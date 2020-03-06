using System;

// ReSharper disable UseNullPropagation

namespace GServer
{
    internal static class ServerTimer
    {
        internal static void Tick() {
            if (OnTick != null) OnTick.Invoke();
        }

        public static event Action OnTick;
    }
}