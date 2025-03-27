using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld;

public class Building_ArchonexusCore : Building
{
	private const int ObservationDistance = 8;

	private List<Pawn> tmpPawnsCanReach = new List<Pawn>();

	private List<IntVec3> tmpObservationSpots = new List<IntVec3>();

	public bool CanActivateNow => !ArchonexusCountdown.CountdownActivated;

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.action = Activate;
			command_Action.Disabled = !CanActivateNow;
			command_Action.defaultLabel = "DEV: Activate archonexus core";
			yield return command_Action;
		}
	}

	public void Activate()
	{
		if (CanActivateNow)
		{
			ArchonexusCountdown.InitiateCountdown(this);
		}
	}

	public override IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(List<Pawn> selPawns)
	{
		if (!CanActivateNow)
		{
			yield return new FloatMenuOption("CannotInvoke".Translate("Power".Translate()) + ": " + "AlreadyInvoked".Translate(), null);
			yield break;
		}
		tmpPawnsCanReach.Clear();
		for (int i = 0; i < selPawns.Count; i++)
		{
			if (selPawns[i].CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
			{
				tmpPawnsCanReach.Add(selPawns[i]);
			}
		}
		if (tmpPawnsCanReach.NullOrEmpty())
		{
			yield return new FloatMenuOption("CannotInvoke".Translate("Power".Translate()) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
			yield break;
		}
		yield return new FloatMenuOption("Invoke".Translate("Power".Translate()), delegate
		{
			tmpPawnsCanReach[0].jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.ActivateArchonexusCore, this), JobTag.Misc);
			tmpObservationSpots.Clear();
			IntVec3 intVec = base.Position + ((InteractionCell - base.Position).ToVector3().normalized * 8f).ToIntVec3();
			for (int j = 1; j < tmpPawnsCanReach.Count; j++)
			{
				IntVec3 intVec2 = RCellFinder.BestOrderedGotoDestNear(intVec, tmpPawnsCanReach[j], (IntVec3 c) => !tmpObservationSpots.Contains(c));
				FloatMenuMakerMap.PawnGotoAction(intVec, tmpPawnsCanReach[j], intVec2);
				tmpObservationSpots.Add(intVec2);
			}
			tmpObservationSpots.Clear();
		});
	}
}
