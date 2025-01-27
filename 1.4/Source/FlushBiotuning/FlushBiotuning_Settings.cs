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

namespace FlushBiotuning
{
    public class FlushBiotuning_Settings : ModSettings
    {
        public static float flushCycleDuration = 7f;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref flushCycleDuration, "flushCycleDuration", flushCycleDuration, true);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };

            l.Begin(rect);

            l.Label("Flush Cycle Duration (days): " + flushCycleDuration.ToString("F1"));
            flushCycleDuration = l.Slider(flushCycleDuration, 0.1f, 20f);

            l.End();

            Settings_Init.ApplySettingsToDefs();

        }
    }

    [StaticConstructorOnStartup]
    public static class Settings_Init
    {
        static Settings_Init()
        {
            ApplySettingsToDefs();
        }

        public static void ApplySettingsToDefs()
        {
            CompProperties_BiosculpterPod_BaseCycle props = DefDatabase<ThingDef>.GetNamed("BiosculpterPod").CompDefFor<CompBiosculpterPod_FlushCycle>() as CompProperties_BiosculpterPod_BaseCycle;
            props.durationDays = FlushBiotuning_Settings.flushCycleDuration;
            //props.durationDays = 0.05f;
        }
    }

}
