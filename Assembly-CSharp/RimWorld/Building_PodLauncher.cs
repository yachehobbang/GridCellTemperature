using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Building_PodLauncher : Building
{
	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		AcceptanceReport acceptanceReport = GenConstruct.CanPlaceBlueprintAt(ThingDefOf.TransportPod, FuelingPortUtility.GetFuelingPortCell(this), ThingDefOf.TransportPod.defaultPlacingRot, base.Map);
		Designator_Build designator_Build = BuildCopyCommandUtility.FindAllowedDesignator(ThingDefOf.TransportPod);
		if (designator_Build != null)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "BuildThing".Translate(ThingDefOf.TransportPod.label);
			command_Action.icon = designator_Build.icon;
			command_Action.defaultDesc = designator_Build.Desc;
			command_Action.action = delegate
			{
				IntVec3 fuelingPortCell = FuelingPortUtility.GetFuelingPortCell(this);
				GenConstruct.PlaceBlueprintForBuild(ThingDefOf.TransportPod, fuelingPortCell, base.Map, ThingDefOf.TransportPod.defaultPlacingRot, Faction.OfPlayer, null);
			};
			if (!acceptanceReport.Accepted)
			{
				command_Action.Disable(acceptanceReport.Reason);
			}
			yield return command_Action;
		}
	}
}
