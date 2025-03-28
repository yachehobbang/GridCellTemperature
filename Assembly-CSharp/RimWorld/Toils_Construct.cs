using Verse;
using Verse.AI;

namespace RimWorld;

public static class Toils_Construct
{
	public static Toil MakeSolidThingFromBlueprintIfNecessary(TargetIndex blueTarget, TargetIndex targetToUpdate = TargetIndex.None)
	{
		Toil toil = ToilMaker.MakeToil("MakeSolidThingFromBlueprintIfNecessary");
		toil.initAction = delegate
		{
			Pawn actor = toil.actor;
			Job curJob = actor.jobs.curJob;
			if (curJob.GetTarget(blueTarget).Thing is Blueprint { Destroyed: false } blueprint)
			{
				bool flag = targetToUpdate != 0 && curJob.GetTarget(targetToUpdate).Thing == blueprint;
				if (blueprint.TryReplaceWithSolidThing(actor, out var createdThing, out var _))
				{
					curJob.SetTarget(blueTarget, createdThing);
					if (flag)
					{
						curJob.SetTarget(targetToUpdate, createdThing);
					}
					if (createdThing is Frame)
					{
						actor.Reserve(createdThing, curJob, 5, 1);
					}
				}
			}
		};
		return toil;
	}

	public static Toil UninstallIfMinifiable(TargetIndex thingInd)
	{
		Toil uninstallIfMinifiable = ToilMaker.MakeToil("UninstallIfMinifiable").FailOnDestroyedNullOrForbidden(thingInd);
		uninstallIfMinifiable.initAction = delegate
		{
			Pawn actor = uninstallIfMinifiable.actor;
			JobDriver curDriver = actor.jobs.curDriver;
			Thing thing = actor.CurJob.GetTarget(thingInd).Thing;
			if (thing.def.Minifiable)
			{
				curDriver.uninstallWorkLeft = thing.def.building.uninstallWork;
			}
			else
			{
				curDriver.ReadyForNextToil();
			}
		};
		uninstallIfMinifiable.tickAction = delegate
		{
			Pawn actor2 = uninstallIfMinifiable.actor;
			JobDriver curDriver2 = actor2.jobs.curDriver;
			Job curJob = actor2.CurJob;
			float num = (StatDefOf.ConstructionSpeed.Worker.IsDisabledFor(actor2) ? 0.1f : actor2.GetStatValue(StatDefOf.ConstructionSpeed));
			curDriver2.uninstallWorkLeft -= num * 1.7f;
			if (curDriver2.uninstallWorkLeft <= 0f)
			{
				Thing thing2 = curJob.GetTarget(thingInd).Thing;
				bool num2 = Find.Selector.IsSelected(thing2);
				MinifiedThing minifiedThing = thing2.MakeMinified();
				Thing thing3 = GenSpawn.Spawn(minifiedThing, thing2.Position, uninstallIfMinifiable.actor.Map);
				if (num2 && thing3 != null)
				{
					Find.Selector.Select(thing3, playSound: false, forceDesignatorDeselect: false);
				}
				curJob.SetTarget(thingInd, minifiedThing);
				actor2.jobs.curDriver.ReadyForNextToil();
			}
		};
		uninstallIfMinifiable.defaultCompleteMode = ToilCompleteMode.Never;
		uninstallIfMinifiable.WithProgressBar(thingInd, () => 1f - uninstallIfMinifiable.actor.jobs.curDriver.uninstallWorkLeft / uninstallIfMinifiable.actor.CurJob.targetA.Thing.def.building.uninstallWork);
		return uninstallIfMinifiable;
	}
}
