using Verse;

namespace RimWorld;

public class Building_AncientMechRemains : Building
{
	private Effecter effecter;

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		Find.History.Notify_MechanoidDatacoreOppurtunityAvailable();
	}

	public override void Tick()
	{
		base.Tick();
		if (effecter == null)
		{
			effecter = EffecterDefOf.AncientExostriderRemainsPulse.SpawnAttached(this, base.Map);
		}
		effecter?.EffectTick(this, this);
	}
}
