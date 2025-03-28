using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class CompFleshmass : ThingComp, ISizeReporter
{
	private static readonly IntRange CascadeCountRange = new IntRange(4, 8);

	public Thing source;

	private Sustainer sustainer;

	public CompProperties_Fleshmass Props => (CompProperties_Fleshmass)props;

	public float CurrentSize()
	{
		return 1f;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_References.Look(ref source, "source");
	}

	public override void CompTickRare()
	{
		if (sustainer == null && !parent.Position.Fogged(parent.Map))
		{
			SoundInfo info = SoundInfo.InMap(new TargetInfo(parent.Position, parent.Map), MaintenanceType.PerTickRare);
			sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, SoundDefOf.FleshmassAmbience, info);
		}
		sustainer?.Maintain();
	}

	public override void PostDeSpawn(Map map)
	{
		if (sustainer != null)
		{
			if (sustainer.externalParams.sizeAggregator == null)
			{
				sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
			}
			sustainer.externalParams.sizeAggregator.RemoveReporter(this);
		}
	}

	public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
	{
		if (dinfo.HasValue && dinfo.Value.Instigator.HasComp<CompFleshmass>())
		{
			return;
		}
		int randomInRange = CascadeCountRange.RandomInRange;
		int num = 0;
		Queue<Thing> queue = new Queue<Thing>();
		foreach (Thing item in AdjacentFleshmass(parent, prevMap))
		{
			queue.Enqueue(item);
		}
		bool flag = dinfo?.Instigator?.Faction == null || dinfo.Value.Instigator.Faction == Faction.OfPlayer;
		if (flag && source != null && source.Spawned)
		{
			source.TryGetComp<CompGrowsFleshmassTendrils>()?.Notify_FleshmassDestroyedByPlayer(parent);
		}
		while (num < randomInRange && !queue.Empty())
		{
			Thing thing = queue.Dequeue();
			foreach (Thing item2 in AdjacentFleshmass(thing, prevMap))
			{
				if (!queue.Contains(item2))
				{
					queue.Enqueue(item2);
				}
			}
			thing.Kill(new DamageInfo(null, 99999f, 0f, -1f, parent));
			num++;
			if (flag && source != null && source.Spawned)
			{
				source.TryGetComp<CompGrowsFleshmassTendrils>()?.Notify_FleshmassDestroyedByPlayer(thing);
			}
		}
	}

	public override void PostDestroy(DestroyMode mode, Map previousMap)
	{
		source.TryGetComp<CompGrowsFleshmassTendrils>()?.Notify_FleshmassDestroyed(parent);
	}

	private IEnumerable<Thing> AdjacentFleshmass(Thing mass, Map map)
	{
		for (int i = 0; i < 4; i++)
		{
			IntVec3 c = mass.Position + GenAdj.CardinalDirections[i];
			if (c.InBounds(map))
			{
				Building edifice = c.GetEdifice(map);
				if (edifice != null && (edifice.def == ThingDefOf.Fleshmass || edifice.def == ThingDefOf.Fleshmass_Active))
				{
					yield return edifice;
				}
			}
		}
	}

	public override string CompInspectStringExtra()
	{
		CompGrowsFleshmassTendrils compGrowsFleshmassTendrils = source?.TryGetComp<CompGrowsFleshmassTendrils>();
		if (parent.def == ThingDefOf.Fleshmass_Active && compGrowsFleshmassTendrils != null && compGrowsFleshmassTendrils.Props.fleshbeastBirthThresholdRange.HasValue)
		{
			return "ActiveFleshmassInspect".Translate();
		}
		return "";
	}
}
