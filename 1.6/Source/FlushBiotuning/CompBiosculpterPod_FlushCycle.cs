using RimWorld;
using Verse;
using System.Reflection;

namespace FlushBiotuning
{
    public class CompBiosculpterPod_FlushCycle : CompBiosculpterPod_Cycle
    {
        CompBiosculpterPod Pod => parent.TryGetComp<CompBiosculpterPod>();

        public override void CycleCompleted(Pawn occupant) 
        { 
            if (Pod != null)
            {
                typeof(CompBiosculpterPod).GetField("tickEntered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Pod, -99999);
            }
        }
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
