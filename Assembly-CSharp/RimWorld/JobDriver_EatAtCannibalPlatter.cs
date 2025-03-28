using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class JobDriver_EatAtCannibalPlatter : JobDriver
{
	private const TargetIndex PlatterIndex = TargetIndex.A;

	private const TargetIndex CellIndex = TargetIndex.B;

	private const int BloodFilthIntervalTick = 40;

	private const float ChanceToProduceBloodFilth = 0.25f;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.ReserveSittableOrSpot(job.targetB.Cell, job, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		if (!ModLister.CheckIdeology("Cannibal eat job"))
		{
			yield break;
		}
		this.EndOnDespawnedOrNull(TargetIndex.A);
		yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell);
		float totalBuildingNutrition = base.TargetA.Thing.def.CostList.Sum((ThingDefCountClass x) => x.thingDef.GetStatValueAbstract(StatDefOf.Nutrition) * (float)x.count);
		Toil eat = ToilMaker.MakeToil("MakeNewToils");
		eat.tickAction = delegate
		{
			pawn.rotationTracker.FaceCell(base.TargetA.Thing.OccupiedRect().ClosestCellTo(pawn.Position));
			pawn.GainComfortFromCellIfPossible();
			if (pawn.needs.food != null)
			{
				pawn.needs.food.CurLevel += totalBuildingNutrition / (float)pawn.GetLord().ownedPawns.Count / (float)eat.defaultDuration;
			}
			if (pawn.IsHashIntervalTick(40) && Rand.Value < 0.25f)
			{
				IntVec3 c = (Rand.Bool ? pawn.Position : pawn.RandomAdjacentCellCardinal());
				if (c.InBounds(pawn.Map))
				{
					FilthMaker.TryMakeFilth(c, pawn.Map, ThingDefOf.Human.race.BloodDef);
				}
			}
		};
		eat.AddFinishAction(delegate
		{
			if (pawn.mindState != null)
			{
				pawn.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
			}
			Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteHumanMeat, pawn.Named(HistoryEventArgsNames.Doer)));
		});
		eat.WithEffect(EffecterDefOf.EatMeat, TargetIndex.A, null);
		eat.PlaySustainerOrSound(SoundDefOf.RawMeat_Eat);
		eat.handlingFacing = true;
		eat.defaultCompleteMode = ToilCompleteMode.Delay;
		eat.defaultDuration = (job.doUntilGatheringEnded ? job.expiryInterval : job.def.joyDuration);
		yield return eat;
	}
}
