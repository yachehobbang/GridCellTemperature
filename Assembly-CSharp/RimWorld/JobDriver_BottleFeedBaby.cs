using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld;

public class JobDriver_BottleFeedBaby : JobDriver_FeedBaby
{
	protected const TargetIndex BabyFoodInd = TargetIndex.B;

	private float bottleNutrition;

	private float totalBottleNutrition;

	private float initialNutritionNeeded;

	protected Thing BabyFood => base.TargetThingB;

	protected LocalTargetInfo BabyFoodTarget => base.TargetB;

	protected override IEnumerable<Toil> MakeNewToils()
	{
		AddFailCondition(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
		Toil failIfNoBabyFoodInInventory = FailIfNoBabyFoodInInventory();
		yield return Toils_Jump.JumpIf(failIfNoBabyFoodInInventory, () => !BabyFoodTarget.IsValid || pawn.inventory.Contains(BabyFood));
		yield return Toils_Ingest.ReserveFoodFromStackForIngesting(TargetIndex.B, base.Baby).FailOnDestroyedNullOrForbidden(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDestroyedNullOrForbidden(TargetIndex.B);
		yield return Toils_Haul.TakeToInventory(TargetIndex.B, delegate(Thing babyFood)
		{
			int b = FoodUtility.WillIngestStackCountOf(base.Baby, babyFood.def, FoodUtility.NutritionForEater(base.Baby, babyFood));
			return Mathf.Min(babyFood.stackCount, b);
		}).FailOnDestroyedNullOrForbidden(TargetIndex.B);
		yield return failIfNoBabyFoodInInventory;
		foreach (Toil item in base.MakeNewToils())
		{
			yield return item;
		}
	}

	private Toil FailIfNoBabyFoodInInventory()
	{
		Toil toil = ToilMaker.MakeToil("FailIfNoBabyFoodInInventory");
		toil.FailOn(() => FoodUtility.BestFoodInInventory(pawn, base.Baby) == null);
		return toil;
	}

	protected override IEnumerable<Toil> FeedBaby()
	{
		yield return FeedBabyFoodFromInventory();
	}

	private Toil FeedBabyFoodFromInventory()
	{
		Toil toil = ToilMaker.MakeToil("FeedBabyFoodFromInventory");
		toil.initAction = delegate
		{
			initialNutritionNeeded = base.Baby.needs.food.NutritionWanted;
			initialFoodPercentage = base.Baby.needs.food.CurLevelPercentage;
			base.Baby.jobs.StartJob(ChildcareUtility.MakeBabySuckleJob(pawn), JobCondition.InterruptForced, null, resumeCurJobAfterwards: false, cancelBusyStances: true, null, null, fromQueue: false, canReturnCurJobToPool: false, null);
		};
		toil.tickAction = delegate
		{
			float b = base.Baby.needs.food.MaxLevel / 5000f;
			float num = Mathf.Min(base.Baby.needs.food.NutritionWanted, b);
			bottleNutrition += num;
			totalBottleNutrition += num;
			pawn.GainComfortFromCellIfPossible();
			base.Baby.ideo?.IncreaseIdeoExposureIfBabyTick(pawn.Ideo);
			if (!pawn.Downed && pawn.Rotation == Rot4.North)
			{
				pawn.Rotation = Rot4.East;
			}
			while (true)
			{
				Thing thing = FoodUtility.BestFoodInInventory(pawn, base.Baby);
				if (thing == null)
				{
					ReadyForNextToil();
					break;
				}
				if (bottleNutrition >= base.Baby.needs.food.NutritionWanted)
				{
					float num2 = thing.Ingested(base.Baby, bottleNutrition);
					base.Baby.records.AddTo(RecordDefOf.NutritionEaten, num2);
					bottleNutrition -= num2;
					base.Baby.needs.food.CurLevel = Mathf.Clamp(base.Baby.needs.food.CurLevel + num2, 0f, base.Baby.needs.food.MaxLevel);
					if (base.Baby.needs.food.CurLevel >= base.Baby.needs.food.MaxLevel)
					{
						ReadyForNextToil();
						break;
					}
				}
				else
				{
					float num3 = FoodUtility.NutritionForEater(base.Baby, thing);
					if (!(bottleNutrition >= num3))
					{
						break;
					}
					float num4 = thing.Ingested(base.Baby, num3);
					base.Baby.records.AddTo(RecordDefOf.NutritionEaten, num4);
					bottleNutrition -= num4;
					base.Baby.needs.food.CurLevel = Mathf.Clamp(base.Baby.needs.food.CurLevel + num4, 0f, base.Baby.needs.food.MaxLevel);
				}
			}
		};
		toil.AddFinishAction(delegate
		{
			if (base.Baby.needs != null && base.Baby.needs.food.CurLevelPercentage - initialFoodPercentage > 0.6f)
			{
				base.Baby.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FedMe, pawn);
				pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.FedBaby, base.Baby);
			}
			if (base.Baby.CurJobDef == JobDefOf.BabySuckle)
			{
				base.Baby.jobs.EndCurrentJob(JobCondition.Succeeded);
			}
		});
		toil.handlingFacing = true;
		toil.WithProgressBar(TargetIndex.A, () => totalBottleNutrition / initialNutritionNeeded);
		toil.defaultCompleteMode = ToilCompleteMode.Never;
		toil.WithEffect(EffecterDefOf.Breastfeeding, TargetIndex.A, null);
		return toil;
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref bottleNutrition, "bottleNutrition", 0f);
		Scribe_Values.Look(ref totalBottleNutrition, "totalBottleNutrition", 0f);
		Scribe_Values.Look(ref initialNutritionNeeded, "initialNutritionNeeded", 0f);
	}
}
