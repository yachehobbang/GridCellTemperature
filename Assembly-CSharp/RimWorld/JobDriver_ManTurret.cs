using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld;

public class JobDriver_ManTurret : JobDriver
{
	private const float SearchRadius = 40f;

	private const int MaxPawnReservations = 10;

	private static bool GunNeedsLoading(Building b)
	{
		if (!(b is Building_TurretGun building_TurretGun))
		{
			return false;
		}
		CompChangeableProjectile compChangeableProjectile = building_TurretGun.gun.TryGetComp<CompChangeableProjectile>();
		if (compChangeableProjectile == null || compChangeableProjectile.Loaded)
		{
			return false;
		}
		return true;
	}

	private static bool GunNeedsRefueling(Building b)
	{
		if (!(b is Building_TurretGun thing))
		{
			return false;
		}
		CompRefuelable compRefuelable = thing.TryGetComp<CompRefuelable>();
		if (compRefuelable == null || compRefuelable.HasFuel || !compRefuelable.Props.fuelIsMortarBarrel || Find.Storyteller.difficulty.classicMortars)
		{
			return false;
		}
		return true;
	}

	public static Thing FindAmmoForTurret(Pawn pawn, Building_TurretGun gun)
	{
		StorageSettings allowedShellsSettings = ((pawn.IsColonist || pawn.IsColonyMech) ? gun.gun.TryGetComp<CompChangeableProjectile>().allowedShellsSettings : null);
		return GenClosest.ClosestThingReachable(gun.Position, gun.Map, ThingRequest.ForGroup(ThingRequestGroup.Shell), PathEndMode.OnCell, TraverseParms.For(pawn), 40f, ShellValidator);
		bool ShellValidator(Thing t)
		{
			if (t.IsForbidden(pawn))
			{
				return false;
			}
			if (!pawn.CanReserve(t, 10, 1))
			{
				return false;
			}
			if (allowedShellsSettings != null && !allowedShellsSettings.AllowedToAccept(t))
			{
				return false;
			}
			if (pawn.Faction != Faction.OfPlayer && t.def.projectileWhenLoaded?.projectile != null && !t.def.projectileWhenLoaded.projectile.damageDef.harmsHealth)
			{
				return false;
			}
			return true;
		}
	}

	public static Thing FindFuelForTurret(Pawn pawn, Building_TurretGun gun)
	{
		CompRefuelable refuelableComp = gun.TryGetComp<CompRefuelable>();
		if (refuelableComp == null)
		{
			return null;
		}
		return GenClosest.ClosestThingReachable(gun.Position, gun.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.OnCell, TraverseParms.For(pawn), 40f, FuelValidator);
		bool FuelValidator(Thing t)
		{
			if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 10, 1))
			{
				return false;
			}
			return refuelableComp.Props.fuelFilter.Allows(t);
		}
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
		Toil gotoTurret = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
		Toil refuelIfNeeded = ToilMaker.MakeToil("MakeNewToils");
		refuelIfNeeded.initAction = delegate
		{
			Pawn actor = refuelIfNeeded.actor;
			Building building = (Building)actor.CurJob.targetA.Thing;
			Building_TurretGun building_TurretGun = building as Building_TurretGun;
			if (!GunNeedsRefueling(building))
			{
				JumpToToil(gotoTurret);
			}
			else
			{
				Thing thing = FindFuelForTurret(pawn, building_TurretGun);
				if (thing == null)
				{
					CompRefuelable compRefuelable = building.TryGetComp<CompRefuelable>();
					if (actor.Faction == Faction.OfPlayer && compRefuelable != null)
					{
						Messages.Message("MessageOutOfNearbyFuelFor".Translate(actor.LabelShort, building_TurretGun.Label, actor.Named("PAWN"), building_TurretGun.Named("GUN"), compRefuelable.Props.fuelFilter.Summary.Named("FUEL")).CapitalizeFirst(), building_TurretGun, MessageTypeDefOf.NegativeEvent);
					}
					actor.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				actor.CurJob.targetB = thing;
				actor.CurJob.count = 1;
			}
		};
		yield return refuelIfNeeded;
		yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
			.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
			.WithProgressBarToilDelay(TargetIndex.A);
		yield return Toils_Refuel.FinalizeRefueling(TargetIndex.A, TargetIndex.B);
		Toil loadIfNeeded = ToilMaker.MakeToil("MakeNewToils");
		loadIfNeeded.initAction = delegate
		{
			Pawn actor2 = loadIfNeeded.actor;
			Building obj = (Building)actor2.CurJob.targetA.Thing;
			Building_TurretGun building_TurretGun2 = obj as Building_TurretGun;
			if (!GunNeedsLoading(obj))
			{
				JumpToToil(gotoTurret);
			}
			else
			{
				Thing thing2 = FindAmmoForTurret(pawn, building_TurretGun2);
				if (thing2 == null)
				{
					if (actor2.Faction == Faction.OfPlayer)
					{
						Messages.Message("MessageOutOfNearbyShellsFor".Translate(actor2.LabelShort, building_TurretGun2.Label, actor2.Named("PAWN"), building_TurretGun2.Named("GUN")).CapitalizeFirst(), building_TurretGun2, MessageTypeDefOf.NegativeEvent);
					}
					actor2.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				actor2.CurJob.targetB = thing2;
				actor2.CurJob.count = 1;
			}
		};
		yield return loadIfNeeded;
		yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
		yield return Toils_Haul.StartCarryThing(TargetIndex.B);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
		Toil loadShell = ToilMaker.MakeToil("MakeNewToils");
		loadShell.initAction = delegate
		{
			Pawn actor3 = loadShell.actor;
			Building_TurretGun building_TurretGun3 = ((Building)actor3.CurJob.targetA.Thing) as Building_TurretGun;
			SoundDefOf.Artillery_ShellLoaded.PlayOneShot(new TargetInfo(building_TurretGun3.Position, building_TurretGun3.Map));
			building_TurretGun3.gun.TryGetComp<CompChangeableProjectile>().LoadShell(actor3.CurJob.targetB.Thing.def, 1);
			actor3.carryTracker.innerContainer.ClearAndDestroyContents();
		};
		yield return loadShell;
		yield return gotoTurret;
		Toil man = ToilMaker.MakeToil("MakeNewToils");
		man.tickAction = delegate
		{
			Pawn actor4 = man.actor;
			Building building2 = (Building)actor4.CurJob.targetA.Thing;
			if (GunNeedsLoading(building2))
			{
				JumpToToil(loadIfNeeded);
			}
			else if (GunNeedsRefueling(building2))
			{
				JumpToToil(refuelIfNeeded);
			}
			else
			{
				building2.GetComp<CompMannable>().ManForATick(actor4);
				man.actor.rotationTracker.FaceCell(building2.Position);
			}
		};
		man.handlingFacing = true;
		man.defaultCompleteMode = ToilCompleteMode.Never;
		man.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
		yield return man;
	}
}
