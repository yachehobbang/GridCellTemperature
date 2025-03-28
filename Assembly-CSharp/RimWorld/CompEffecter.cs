using Verse;

namespace RimWorld;

public class CompEffecter : ThingComp
{
	private Effecter effecter;

	public CompProperties_Effecter Props => (CompProperties_Effecter)props;

	public override void CompTick()
	{
		base.CompTick();
		if (parent.Spawned && parent.MapHeld == Find.CurrentMap)
		{
			if (effecter == null)
			{
				effecter = Props.effecterDef.SpawnAttached(parent, parent.MapHeld);
			}
			effecter?.EffectTick(parent, parent);
		}
		else
		{
			effecter?.Cleanup();
			effecter = null;
		}
	}
}
