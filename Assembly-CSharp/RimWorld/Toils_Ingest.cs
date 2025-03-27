using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld;

public class Toils_Ingest
{
	public const int MaxPawnReservations = 10;

	private static List<IntVec3> spotSearchList = new List<IntVec3>();

	private static List<IntVec3> cardinals = GenAdj.CardinalDirections.ToList();

	private static List<IntVec3> diagonals = GenAdj.DiagonalDirections.ToList();

	public static Toil TakeMealFromDispenser(TargetIndex ind, Pawn eater)
	{
		Toil toil = ToilMaker.MakeToil("TakeMealFromDispenser");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Thing thing = ((Building_NutrientPasteDispenser)actor.jobs.curJob.GetTarget(ind).Thing).TryDispenseFood();
			if (thing == null)
			{
				actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
			}
			else
			{
				actor.carryTracker.TryStartCarry(thing);
				actor.CurJob.SetTarget(ind, actor.carryTracker.CarriedThing);
			}
		};
		toil.FailOnCannotTouch(ind, PathEndMode.Touch);
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = Building_NutrientPasteDispenser.CollectDuration;
		return toil;
	}

	public static Toil PickupIngestible(TargetIndex ind, Pawn eater)
	{
		Toil toil = ToilMaker.MakeToil("PickupIngestible");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Job curJob = actor.jobs.curJob;
			Thing thing = curJob.GetTarget(ind).Thing;
			if (curJob.count <= 0)
			{
				Log.Error("Tried to do PickupIngestible toil with job.count = " + curJob.count);
				actor.jobs.EndCurrentJob(JobCondition.Errored);
			}
			else
			{
				int count = Mathf.Min(thing.stackCount, curJob.count);
				actor.carryTracker.TryStartCarry(thing, count);
				if (thing != actor.carryTracker.CarriedThing && actor.Map.reservationManager.ReservedBy(thing, actor, curJob))
				{
					actor.Map.reservationManager.Release(thing, actor, curJob);
				}
				actor.jobs.curJob.targetA = actor.carryTracker.CarriedThing;
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}

	public static Toil CarryIngestibleToChewSpot(Pawn pawn, TargetIndex ingestibleInd)
	{
		Toil toil = ToilMaker.MakeToil("CarryIngestibleToChewSpot");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			IntVec3 cell = IntVec3.Invalid;
			Thing thing = null;
			Thing thing2 = actor.CurJob.GetTarget(ingestibleInd).Thing;
			if (thing2.def.ingestible.chairSearchRadius > 0f)
			{
				thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), thing2.def.ingestible.chairSearchRadius, (Thing t) => BaseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);
			}
			if (thing == null)
			{
				cell = RCellFinder.SpotToChewStandingNear(actor, actor.CurJob.GetTarget(ingestibleInd).Thing, (IntVec3 c) => actor.CanReserveSittableOrSpot(c));
				Danger chewSpotDanger = cell.GetDangerFor(pawn, actor.Map);
				if (chewSpotDanger != Danger.None)
				{
					thing = GenClosest.ClosestThingReachable(actor.Position, actor.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), thing2.def.ingestible.chairSearchRadius, (Thing t) => BaseChairValidator(t) && (int)t.Position.GetDangerFor(pawn, t.Map) <= (int)chewSpotDanger);
				}
			}
			if (thing != null && !TryFindFreeSittingSpotOnThing(thing, actor, out cell))
			{
				Log.Error("Could not find sitting spot on chewing chair! This is not supposed to happen - we looked for a free spot in a previous check!");
			}
			actor.ReserveSittableOrSpot(cell, actor.CurJob);
			actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, cell);
			actor.pather.StartPath(cell, PathEndMode.OnCell);
			bool BaseChairValidator(Thing t)
			{
				if (t.def.building == null || !t.def.building.isSittable)
				{
					return false;
				}
				if (!TryFindFreeSittingSpotOnThing(t, actor, out var cell2))
				{
					return false;
				}
				if (t.IsForbidden(pawn))
				{
					return false;
				}
				if (actor.IsColonist && t.Position.Fogged(t.Map))
				{
					return false;
				}
				if (!actor.CanReserve(t))
				{
					return false;
				}
				if (!t.IsSociallyProper(actor))
				{
					return false;
				}
				if (t.IsBurning())
				{
					return false;
				}
				if (t.HostileTo(pawn))
				{
					return false;
				}
				bool flag = false;
				for (int i = 0; i < 4; i++)
				{
					Building edifice = (cell2 + GenAdj.CardinalDirections[i]).GetEdifice(t.Map);
					if (edifice != null && edifice.def.surfaceType == SurfaceType.Eat)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
				return true;
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
		return toil;
	}

	public static bool TryFindFreeSittingSpotOnThing(Thing t, Pawn pawn, out IntVec3 cell)
	{
		foreach (IntVec3 item in t.OccupiedRect())
		{
			if (pawn.CanReserveSittableOrSpot(item))
			{
				cell = item;
				return true;
			}
		}
		cell = IntVec3.Invalid;
		return false;
	}

	public static Toil ReserveFoodFromStackForIngesting(TargetIndex ind, Pawn ingester = null)
	{
		Toil toil = ToilMaker.MakeToil("ReserveFoodFromStackForIngesting");
		toil.initAction = delegate
		{
			if (ingester == null)
			{
				ingester = toil.actor;
			}
			int stackCount = -1;
			LocalTargetInfo target = toil.actor.jobs.curJob.GetTarget(ind);
			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.IngestibleNow)
			{
				int b = FoodUtility.WillIngestStackCountOf(ingester, target.Thing.def, FoodUtility.NutritionForEater(ingester, target.Thing));
				stackCount = Mathf.Min(target.Thing.stackCount, b);
			}
			if (!target.HasThing || !toil.actor.CanReserve(target, 10, stackCount))
			{
				toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
			}
			toil.actor.Reserve(target, toil.actor.CurJob, 10, stackCount);
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		toil.atomicWithPrevious = true;
		return toil;
	}

	public static bool TryFindAdjacentIngestionPlaceSpot(IntVec3 root, ThingDef ingestibleDef, Pawn pawn, out IntVec3 placeSpot)
	{
		placeSpot = IntVec3.Invalid;
		for (int i = 0; i < 4; i++)
		{
			IntVec3 intVec = root + GenAdj.CardinalDirections[i];
			if (intVec.HasEatSurface(pawn.Map) && !pawn.Map.thingGrid.ThingsAt(intVec).Any((Thing t) => t.def == ingestibleDef) && !intVec.IsForbidden(pawn))
			{
				placeSpot = intVec;
				return true;
			}
		}
		if (!placeSpot.IsValid)
		{
			spotSearchList.Clear();
			cardinals.Shuffle();
			for (int j = 0; j < 4; j++)
			{
				spotSearchList.Add(cardinals[j]);
			}
			diagonals.Shuffle();
			for (int k = 0; k < 4; k++)
			{
				spotSearchList.Add(diagonals[k]);
			}
			spotSearchList.Add(IntVec3.Zero);
			for (int l = 0; l < spotSearchList.Count; l++)
			{
				IntVec3 intVec2 = root + spotSearchList[l];
				if (intVec2.Walkable(pawn.Map) && !intVec2.IsForbidden(pawn) && !pawn.Map.thingGrid.ThingsAt(intVec2).Any((Thing t) => t.def == ingestibleDef))
				{
					placeSpot = intVec2;
					return true;
				}
			}
		}
		return false;
	}

	public static Toil FindAdjacentEatSurface(TargetIndex eatSurfaceInd, TargetIndex foodInd)
	{
		Toil toil = ToilMaker.MakeToil("FindAdjacentEatSurface");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			IntVec3 position = actor.Position;
			Map map = actor.Map;
			for (int i = 0; i < 4; i++)
			{
				IntVec3 intVec = position + new Rot4(i).FacingCell;
				if (intVec.HasEatSurface(map))
				{
					toil.actor.CurJob.SetTarget(eatSurfaceInd, intVec);
					toil.actor.jobs.curDriver.rotateToFace = eatSurfaceInd;
					Thing thing = toil.actor.CurJob.GetTarget(foodInd).Thing;
					if (thing.def.rotatable)
					{
						thing.Rotation = Rot4.FromIntVec3(intVec - toil.actor.Position);
					}
					break;
				}
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}

	public static Toil ChewIngestible(Pawn chewer, float durationMultiplier, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd = TargetIndex.None)
	{
		Toil toil = ToilMaker.MakeToil("ChewIngestible");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Thing thing = actor.CurJob.GetTarget(ingestibleInd).Thing;
			if (!thing.IngestibleNow)
			{
				chewer.jobs.EndCurrentJob(JobCondition.Incompletable);
			}
			else
			{
				toil.actor.pather.StopDead();
				actor.jobs.curDriver.ticksLeftThisToil = Mathf.RoundToInt((float)thing.def.ingestible.baseIngestTicks * durationMultiplier);
				if (thing.Spawned)
				{
					thing.Map.physicalInteractionReservationManager.Reserve(chewer, actor.CurJob, thing);
				}
			}
		};
		toil.tickAction = delegate
		{
			if (chewer != toil.actor)
			{
				toil.actor.rotationTracker.FaceCell(chewer.Position);
			}
			else
			{
				Thing thing2 = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
				if (thing2 != null && thing2.Spawned)
				{
					toil.actor.rotationTracker.FaceCell(thing2.Position);
				}
				else if (eatSurfaceInd != 0 && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid)
				{
					toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(eatSurfaceInd).Cell);
				}
			}
			toil.actor.GainComfortFromCellIfPossible();
		};
		toil.WithProgressBar(ingestibleInd, delegate
		{
			Thing thing3 = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
			return (thing3 == null) ? 1f : (1f - (float)toil.actor.jobs.curDriver.ticksLeftThisToil / Mathf.Round((float)thing3.def.ingestible.baseIngestTicks * durationMultiplier));
		});
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.FailOnDestroyedOrNull(ingestibleInd);
		toil.AddFinishAction(delegate
		{
			Thing thing4 = chewer?.CurJob?.GetTarget(ingestibleInd).Thing;
			if (thing4 != null && chewer.Map.physicalInteractionReservationManager.IsReservedBy(chewer, thing4))
			{
				chewer.Map.physicalInteractionReservationManager.Release(chewer, toil.actor.CurJob, thing4);
			}
		});
		toil.handlingFacing = true;
		AddIngestionEffects(toil, chewer, ingestibleInd, eatSurfaceInd);
		return toil;
	}

	public static Toil AddIngestionEffects(Toil toil, Pawn chewer, TargetIndex ingestibleInd, TargetIndex eatSurfaceInd)
	{
		toil.WithEffect(delegate
		{
			LocalTargetInfo target = toil.actor.CurJob.GetTarget(ingestibleInd);
			if (!target.HasThing)
			{
				return (EffecterDef)null;
			}
			EffecterDef result = target.Thing.def.ingestible.ingestEffect;
			if ((int)chewer.RaceProps.intelligence < 1 && target.Thing.def.ingestible.ingestEffectEat != null)
			{
				result = target.Thing.def.ingestible.ingestEffectEat;
			}
			return result;
		}, delegate
		{
			if (!toil.actor.CurJob.GetTarget(ingestibleInd).HasThing)
			{
				return (LocalTargetInfo)null;
			}
			Thing thing = toil.actor.CurJob.GetTarget(ingestibleInd).Thing;
			if (chewer != toil.actor)
			{
				return chewer;
			}
			return (eatSurfaceInd != 0 && toil.actor.CurJob.GetTarget(eatSurfaceInd).IsValid) ? toil.actor.CurJob.GetTarget(eatSurfaceInd) : ((LocalTargetInfo)thing);
		}, null);
		toil.PlaySustainerOrSound(delegate
		{
			if (!chewer.RaceProps.Humanlike)
			{
				return chewer.RaceProps.soundEating;
			}
			LocalTargetInfo target2 = toil.actor.CurJob.GetTarget(ingestibleInd);
			return (!target2.HasThing) ? null : target2.Thing.def.ingestible.ingestSound;
		});
		return toil;
	}

	public static Toil FinalizeIngest(Pawn ingester, TargetIndex ingestibleInd)
	{
		Toil toil = ToilMaker.MakeToil("FinalizeIngest");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Job curJob = actor.jobs.curJob;
			Thing thing = curJob.GetTarget(ingestibleInd).Thing;
			if (ingester.needs.mood != null && thing.def.IsNutritionGivingIngestible && thing.def.ingestible.chairSearchRadius > 10f)
			{
				if (!(ingester.Position + ingester.Rotation.FacingCell).HasEatSurface(actor.Map) && ingester.GetPosture() == PawnPosture.Standing && !ingester.IsWildMan() && thing.def.ingestible.tableDesired)
				{
					ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.AteWithoutTable);
				}
				Room room = ingester.GetRoom();
				if (room != null)
				{
					int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness));
					if (ThoughtDefOf.AteInImpressiveDiningRoom.stages[scoreStageIndex] != null)
					{
						ingester.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(ThoughtDefOf.AteInImpressiveDiningRoom, scoreStageIndex));
					}
				}
			}
			float num = ingester.needs?.food?.NutritionWanted ?? (thing.GetStatValue(StatDefOf.Nutrition) * (float)thing.stackCount);
			if (curJob.ingestTotalCount)
			{
				num = thing.GetStatValue(StatDefOf.Nutrition) * (float)thing.stackCount;
			}
			else if (curJob.overeat)
			{
				num = Mathf.Max(num, 0.75f);
			}
			float num2 = thing.Ingested(ingester, num);
			if (!ingester.Dead && ingester.needs?.food != null)
			{
				ingester.needs.food.CurLevel += num2;
				ingester.records.AddTo(RecordDefOf.NutritionEaten, num2);
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}
}
