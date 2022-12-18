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

    [StaticConstructorOnStartup]
    public static class HarmonyPatching
    {
        static HarmonyPatching()
        {
            var harmony = new Harmony("com.cyanobot.flushbiotuning");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod),nameof(CompBiosculpterPod.CompFloatMenuOptions))]
    class Patch_CompFloatMenuOptions
    {
        static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> optionsIn, CompBiosculpterPod __instance)
        {
            CompBiosculpterPod_FlushCycle flushcycle = __instance.parent.GetComp<CompBiosculpterPod_FlushCycle>();
            string flushCycleLabel = flushcycle.Props.label;
            //Log.Message("flushCycleLabel : " + flushCycleLabel);

            foreach (FloatMenuOption option in optionsIn)
            {
                //Log.Message("optionLabel : " + option.Label);
                bool labelCheck = option.Label.IndexOf(flushCycleLabel, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!labelCheck)
                {
                    yield return option;
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod),nameof(CompBiosculpterPod.EjectContents))]
    class Patch_EjectContents
    {
        static void Prefix(CompBiosculpterPod __instance, ref float ___liquifiedNutrition, ref float __state)
        {
            if (__instance.Occupant == null)
            {
                //Log.Message("Attempting to save to state nutrition " + ___liquifiedNutrition.ToString());
                __state = ___liquifiedNutrition;
                //Log.Message("state set to " + __state.ToString());
            }
        }

        static void Postfix(CompBiosculpterPod __instance, ref float ___liquifiedNutrition, ref float __state)
        {
            if (__instance.Occupant == null)
            {
                //Log.Message("Attempting to load from state nutrition " + __state.ToString());
                ___liquifiedNutrition = __state;
                //Log.Message("nutrition read as " + ___liquifiedNutrition.ToString());
            }

        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod), nameof(CompBiosculpterPod.State), MethodType.Getter)]
    class Patch_State
    {
        static BiosculpterPodState Postfix(BiosculpterPodState state, CompBiosculpterPod __instance, float ___currentCycleTicksRemaining)
        {
            //if there's no occupant but there is a currentCycleTicksRemaining, infer that we are running a flush cycle and mark the pod as occupied
            if (__instance.Occupant == null && ___currentCycleTicksRemaining != null && ___currentCycleTicksRemaining != 0)
            {
                state = BiosculpterPodState.Occupied;
            }
            return state;
        }
    }


    [HarmonyPatch(typeof(CompBiosculpterPod), nameof(CompBiosculpterPod.PostDraw))]
    class Patch_PostDraw
    {
        static bool Prefix(CompBiosculpterPod __instance, Material ___BackgroundMat)
        {
            //if the pod's on but there's no pawn inside (ie we're running a flush cycle) don't try to draw one
            if (__instance.State == BiosculpterPodState.Occupied && __instance.Occupant == null)
            {
                //do draw the other stuff that's usually drawn in this method
                //(ie the black background)

                //note that this doesn't call base.PostDraw()
                //because that's very awkward to implement
                //and it's an empty method anyway

                Rot4 rotation = __instance.parent.Rotation;
                Vector3 s = new Vector3(__instance.parent.def.graphicData.drawSize.x * 0.9f, 1f, __instance.parent.def.graphicData.drawSize.y * 0.9f);
                Vector3 drawPos = __instance.parent.DrawPos;
                drawPos.y -= 0.08108108f;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, rotation.AsQuat, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, ___BackgroundMat, 0);

                return false;
            }
            else return true;
        }
    }


    [HarmonyPatch(typeof(CompBiosculpterPod), nameof(CompBiosculpterPod.CompInspectStringExtra))]
    class Patch_CompInspectStringExtra
    {
        static bool Prefix(ref string __result, CompBiosculpterPod __instance, float ___currentCycleTicksRemaining, int ___currentCyclePowerCutTicks)
        {
            if (__instance.State == BiosculpterPodState.Occupied && __instance.Occupant == null)
            {
                StringBuilder builder = new StringBuilder();
                if (__instance.parent.Spawned)
                {
                    if (__instance.CurrentCycle != null)
                    {
                        builder.AppendLineIfNotEmpty().Append("BiosculpterPodCycleLabel".Translate()).Append(": ").Append(__instance.CurrentCycle.Props.LabelCap);
                    }

                    float num = ___currentCycleTicksRemaining / __instance.CycleSpeedFactor;

                    if (!__instance.PowerOn)
                    {
                        builder.AppendLine().Append("BiosculpterCycleNoPowerInterrupt".Translate((60000 - ___currentCyclePowerCutTicks).ToStringTicksToPeriod(true, false, true, true, false).Named("TIME")).Colorize(ColorLibrary.RedReadable));
                    }

                    builder.AppendLine().Append("BiosculpterCycleTimeRemaining".Translate()).Append(": ").Append(((int)num).ToStringTicksToPeriod(true, false, true, true, false).Colorize(ColoredText.DateTimeColor));

                    var cleanlinessSpeedFactor = __instance.GetType().GetProperty("CleanlinessSpeedFactor", BindingFlags.NonPublic | BindingFlags.Instance);
                    float factor = float.Parse(cleanlinessSpeedFactor.GetValue(__instance).ToString());
                    builder.AppendLine().Append("BiosculpterCleanlinessSpeedFactor".Translate()).Append(": ").Append(factor.ToStringPercent());

                }

                if (builder.Length <= 0)
                {
                    __result = null;
                }
                else
                {
                    __result = builder.ToString();
                }
                return false;
            }
            else return true;
        }
    }


    [HarmonyPatch(typeof(CompBiosculpterPod), "CycleDescription")]
    class Patch_CycleDescription
    {
        static bool Prefix(ref string __result, CompBiosculpterPod __instance, CompBiosculpterPod_Cycle cycle, Pawn ___biotunedTo)
        {
            if (cycle is CompBiosculpterPod_FlushCycle)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(cycle.Description(___biotunedTo));
                float num = cycle.Props.durationDays / __instance.CycleSpeedFactor;
                builder.AppendLine("\n\n" + "BiosculpterPodCycleDuration".Translate() + ": " + ((int)(num * 60000f)).ToStringTicksToDays("F1"));
                __result = builder.ToString();
                return false;
            }
            else return true;
        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod), nameof(CompBiosculpterPod.CompGetGizmosExtra))]
    class Patch_CompGetGizmosExtra
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> existingGizmos, CompBiosculpterPod __instance, List<CompBiosculpterPod_Cycle> ___cachedAvailableCycles, Pawn ___biotunedTo)
        {

            CompBiosculpterPod_FlushCycle flushcycle = __instance.parent.GetComp<CompBiosculpterPod_FlushCycle>();
            Texture2D referenceIcon = flushcycle.Props.Icon;

            //pass through all the gizmos already created
            foreach (Gizmo gizmo in existingGizmos)
            {

                //command actions don't seem to have unique identifiers
                //so using the icon as a way to identify the command action for the flush cycle

                //first check if the gizmo is a Command_Action
                //so that we can attempt to read its icon
                //if not, pass it through unchanged
                Command_Action commandaction = gizmo as Command_Action;
                if (commandaction == null)
                {
                    yield return gizmo;
                }

                else if (commandaction.icon == referenceIcon)
                {
                    commandaction.defaultLabel = flushcycle.Props.label + ((___biotunedTo != null) ? (" (" + ___biotunedTo.LabelShort + ")") : "");
                    commandaction.action = delegate ()
                    {
                        FlushBiotuning_Mod.StartFlushCycle(__instance, flushcycle);
                    };

                    string text = FlushBiotuning_Mod.CannotFlushReason(__instance);
                    commandaction.disabled = (text != null);
                    if (text != null)
                    {
                        commandaction.Disable(text);
                    }

                    yield return (Gizmo)commandaction;
                }

                //pass all other gizmos through unchanged
                else yield return gizmo;

            }
        }
    }
}
