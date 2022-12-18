using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace FlushBiotuning
{
    class FlushBiotuning_Mod : Mod
    {
        public FlushBiotuning_Mod(ModContentPack mcp) : base(mcp)
        {
            GetSettings<FlushBiotuning_Settings>();
        }

        public override string SettingsCategory()
        {
            return "Flush Biosculpter Tuning";
        }

        public override void DoSettingsWindowContents(Rect inRect) => FlushBiotuning_Settings.DoSettingsWindowContents(inRect);

        public static void StartFlushCycle(CompBiosculpterPod pod, CompBiosculpterPod_FlushCycle flushcycle)
        {
            var currentCycleKey = pod.GetType().GetField("currentCycleKey", BindingFlags.NonPublic | BindingFlags.Instance);
            currentCycleKey.SetValue(pod, flushcycle.Props.key);

            var currentCycleTicksRemaining = pod.GetType().GetField("currentCycleTicksRemaining", BindingFlags.NonPublic | BindingFlags.Instance);
            float num = flushcycle.Props.durationDays * 60000f;
            currentCycleTicksRemaining.SetValue(pod, num);

            pod.devFillPodLatch = false;
            pod.ClearQueuedInformation();

        }

        public static string CannotFlushReason(CompBiosculpterPod pod)
        {
            if (!pod.PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (pod.State == BiosculpterPodState.Occupied)
            {
                return "BiosculpterOccupied".Translate().CapitalizeFirst();
            }
            var biotunedTo = pod.GetType().GetField("biotunedTo", BindingFlags.NonPublic | BindingFlags.Instance);
            if (biotunedTo.GetValue(pod) == null)
            {
                return "Pod is not biotuned to anyone.";
            }
            return null;
        }
    }

    public class CompBiosculpterPod_FlushCycle : CompBiosculpterPod_Cycle
    {
        public override void CycleCompleted(Pawn occupant) { }
        public new CompProperties_BiosculpterPod_FlushCycle Props
        {
            get
            {
                return (CompProperties_BiosculpterPod_FlushCycle)this.props;
            }
        }
    }

    public class CompProperties_BiosculpterPod_FlushCycle : CompProperties_BiosculpterPod_BaseCycle
    {
        public CompProperties_BiosculpterPod_FlushCycle()
        {
            this.compClass = typeof(CompProperties_BiosculpterPod_FlushCycle);
        }
    }
}
