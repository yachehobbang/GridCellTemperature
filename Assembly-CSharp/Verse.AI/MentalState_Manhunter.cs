using RimWorld;

namespace Verse.AI;

public class MentalState_Manhunter : MentalState
{
	public override void PostStart(string reason)
	{
		base.PostStart(reason);
		LessonAutoActivator.TeachOpportunity(ConceptDefOf.AnimalsDontAttackDoors, OpportunityType.Critical);
	}

	public override bool ForceHostileTo(Thing t)
	{
		if (t is Pawn pawn && pawn.RaceProps.Roamer)
		{
			return false;
		}
		if (t.Faction != null)
		{
			return ForceHostileTo(t.Faction);
		}
		return false;
	}

	public override bool ForceHostileTo(Faction f)
	{
		if (ModsConfig.AnomalyActive && f == Faction.OfEntities)
		{
			return true;
		}
		if (!f.def.humanlikeFaction)
		{
			return f == Faction.OfMechanoids;
		}
		return true;
	}

	public override RandomSocialMode SocialModeMax()
	{
		return RandomSocialMode.Off;
	}
}
