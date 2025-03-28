using Verse;

namespace RimWorld;

public abstract class CompRitualEffect_Constant : RitualVisualEffectComp
{
	protected bool spawned;

	public override void OnSetup(RitualVisualEffect parent, LordJob_Ritual ritual, bool loading)
	{
		base.OnSetup(parent, ritual, loading);
		Spawn(ritual);
	}

	protected virtual void Spawn(LordJob_Ritual ritual)
	{
		Mote mote = SpawnMote(ritual, null);
		if (mote != null)
		{
			parent.AddMoteToMaintain(mote);
			if (props.colorOverride.HasValue)
			{
				mote.instanceColor = props.colorOverride.Value;
			}
			else
			{
				mote.instanceColor = parent.def.tintColor;
			}
			spawned = true;
		}
	}

	public override void Tick()
	{
		base.Tick();
		if (!spawned)
		{
			Spawn(parent.ritual);
		}
	}
}
