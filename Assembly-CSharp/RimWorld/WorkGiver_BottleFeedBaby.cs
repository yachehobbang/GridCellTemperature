using Verse;
using Verse.AI;

namespace RimWorld;

public class WorkGiver_BottleFeedBaby : WorkGiver_FeedBabyManually
{
	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!CanCreateManualFeedingJob(pawn, t, forced))
		{
			return null;
		}
		Pawn baby = (Pawn)t;
		Thing foodSource;
		if ((foodSource = ChildcareUtility.FindBabyFoodForBaby(pawn, baby)) != null)
		{
			return ChildcareUtility.MakeBottlefeedJob(baby, foodSource);
		}
		JobFailReason.Is("NoBabyFood".Translate());
		return null;
	}
}
