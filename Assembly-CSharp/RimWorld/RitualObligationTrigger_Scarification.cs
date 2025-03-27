using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class RitualObligationTrigger_Scarification : RitualObligationTrigger_EveryMember
{
	private static List<Pawn> existingObligations = new List<Pawn>();

	public override string TriggerExtraDesc => "RitualScarificationTriggerExtraDesc".Translate(ritual.ideo.RequiredScars, ritual.ideo.memberName.Named("IDEOMEMBER"), ritual.ideo.MemberNamePlural.Named("IDEOMEMBERPLURAL"));

	protected override void Recache()
	{
		try
		{
			if (ritual.activeObligations != null)
			{
				ritual.activeObligations.RemoveAll((RitualObligation o) => o.targetA.Thing is Pawn pawn && pawn.Ideo != ritual.ideo);
				foreach (RitualObligation activeObligation in ritual.activeObligations)
				{
					existingObligations.Add(activeObligation.targetA.Thing as Pawn);
				}
			}
			foreach (Pawn allMapsCaravansAndTravelingTransportPods_Alive_Colonist in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
			{
				if (!existingObligations.Contains(allMapsCaravansAndTravelingTransportPods_Alive_Colonist) && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo != null && allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo == ritual.ideo && !allMapsCaravansAndTravelingTransportPods_Alive_Colonist.IsPrisoner)
				{
					int hediffCount = allMapsCaravansAndTravelingTransportPods_Alive_Colonist.health.hediffSet.GetHediffCount(HediffDefOf.Scarification);
					if (allMapsCaravansAndTravelingTransportPods_Alive_Colonist.Ideo.RequiredScars > hediffCount)
					{
						ritual.AddObligation(new RitualObligation(ritual, allMapsCaravansAndTravelingTransportPods_Alive_Colonist, expires: false));
					}
				}
			}
		}
		finally
		{
			existingObligations.Clear();
		}
	}
}
