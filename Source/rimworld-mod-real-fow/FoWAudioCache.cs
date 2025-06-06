﻿using Verse;
using Verse.Sound;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RimWorldRealFoW
{
    public static class FoW_AudioCache
    {
        private static readonly Dictionary<Thing, List<Sustainer>> _map = new(); // Dictionary should run in average case O(1) time


        // Adds the Thing, and its sustainer, to the dictionary. Called by HarmonyPatches.Patch_SustainerRegister
        public static void Register(Thing thing, Sustainer sus)
        {
            if (thing == null || sus == null) return;

            if (!_map.TryGetValue(thing, out var list))
            {
                list = new List<Sustainer>();
                _map[thing] = list;
            }
            list.Add(sus);
        }

        // If the sustainer is removed on its own but the Thing is left behind, the dictionary entry must be removed to prevent holding a stale reference. HarmonyPatches.Patch_Sustainer_End
        public static void Unregister(Sustainer sus)
        {
            foreach (var kv in _map)
            {
                if (kv.Value.Remove(sus))
                {
                    if (kv.Value.Count == 0) _map.Remove(kv.Key);
                    break;
                }
            }
        }

        // Finds this Thing's sustainer in the dictionary, and ends it. Called by CompHiddenable.hide()
        public static void StopAll(Thing thing)
        {
            if (!_map.TryGetValue(thing, out var list)) return;

            foreach (var s in list.ToList())
                if (!s.Ended) s.End();

            _map.Remove(thing);
        }

        public static float GetAudibilityFactor(TargetInfo maker, int maxDist)
        {

            var comp = maker.Map.getMapComponentSeenFog();
            var origin = maker.Cell;
            var known = comp.knownCells;
            var indices = maker.Map.cellIndices;

            for (int d = 0; d <= maxDist; d++)
            {
                foreach (var c in GenRadial.RadialCellsAround(origin, d, true))
                {
                    if (c.InBounds(maker.Map))
                    {
                        int idx = indices.CellToIndex(c);
                        if (known[idx])
                        {
                            float raw = 1f - (d / (float)maxDist);
                            float m = RFOWSettings.volumeMufflingModifier;
                            float f = (1f - m) + raw * m;
                            return Mathf.Clamp01(f);
                        }
                    }
                }
            }
            return 0f;
        }
    }

}
