using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld;

public class JobDriver_EnterPortal : JobDriver
{
	private TargetIndex PortalInd = TargetIndex.A;

	private const int EnterDelay = 90;

	public MapPortal MapPortal => base.TargetThingA as MapPortal;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedOrNull(PortalInd);
		this.FailOn(() => !(base.TargetThingA as MapPortal).IsEnterable(out var _));
		yield return Toils_Goto.GotoThing(PortalInd, PathEndMode.Touch);
		Toil toil = Toils_General.Wait(90).FailOnCannotTouch(PortalInd, PathEndMode.Touch).WithProgressBarToilDelay(PortalInd, interpolateBetweenActorAndTarget: true);
		toil.tickAction = (Action)Delegate.Combine(toil.tickAction, (Action)delegate
		{
			pawn.rotationTracker.FaceTarget(base.TargetA);
		});
		toil.handlingFacing = true;
		yield return toil;
		Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
		toil2.initAction = delegate
		{
			MapPortal mapPortal = base.TargetThingA as MapPortal;
			Map otherMap = mapPortal.GetOtherMap();
			IntVec3 intVec = mapPortal.GetDestinationLocation();
			if (!intVec.Standable(otherMap))
			{
				intVec = CellFinder.StandableCellNear(intVec, otherMap, 5f);
			}
			if (intVec == IntVec3.Invalid)
			{
				Messages.Message("UnableToEnterPortal".Translate(base.TargetThingA.Label), base.TargetThingA, MessageTypeDefOf.NegativeEvent);
			}
			else
			{
				bool drafted = pawn.Drafted;
				pawn.DeSpawnOrDeselect();
				GenSpawn.Spawn(pawn, intVec, otherMap, Rot4.Random);
				mapPortal.OnEntered(pawn);
				if (!otherMap.IsPocketMap)
				{
					pawn.inventory.UnloadEverything = true;
				}
				if (drafted || mapPortal.AutoDraftOnEnter)
				{
					pawn.drafter.Drafted = true;
				}
				if (pawn.carryTracker.CarriedThing != null && !pawn.Drafted)
				{
					pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out var _);
				}
				pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ExitedMap, null);
			}
		};
		yield return toil2;
	}
}
