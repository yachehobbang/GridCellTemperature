using Verse;

namespace RimWorld;

public abstract class CompRitualEffectSpawner : ThingComp
{
	protected LordJob_Ritual ritual;

	private const int RitualCheckInterval = 30;

	public override void CompTick()
	{
		base.CompTick();
		if (parent.IsHashIntervalTick(30))
		{
			ritual = parent.TargetOfRitual();
		}
		if (ritual != null)
		{
			Tick_InRitual(ritual);
		}
		else
		{
			Tick_OutOfRitual();
		}
	}

	protected abstract void Tick_InRitual(LordJob_Ritual ritual);

	protected abstract void Tick_OutOfRitual();
}
