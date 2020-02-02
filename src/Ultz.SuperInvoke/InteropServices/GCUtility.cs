﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ultz.SuperInvoke.InteropServices
{
    public class GcUtility
    {
        public Dictionary<int, List<GCHandle>> Pins { get; } = new Dictionary<int, List<GCHandle>>();

        public void PinUntilNextCall(object obj, int slot)
        {
            if (!Pins.ContainsKey(slot))
            {
                Pins[slot] = new List<GCHandle>();
            }

            Pins[slot].Clear();
            Pins[slot].Add(GCHandle.Alloc(obj));
        }

        public void Pin(object obj, int slot = -1)
        {
            if (!Pins.ContainsKey(slot))
            {
                Pins[slot] = new List<GCHandle>();
            }

            Pins[slot].Add(GCHandle.Alloc(obj));
        }

        [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
        public void Unpin(object obj, int? slot = null)
        {
            if (slot == null)
            {
                foreach (var list in Pins.Values)
                {
                    foreach (var handle in list)
                    {
                        if (handle.Target == obj)
                        {
                            handle.Free();
                        }
                    }
                }
            }
            else
            {
                foreach (var handle in Pins[slot.Value])
                {
                    if (handle.Target == obj)
                    {
                        handle.Free();
                    }
                }
            }
        }
    }
}