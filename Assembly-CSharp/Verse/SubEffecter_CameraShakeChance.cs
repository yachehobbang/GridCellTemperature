namespace Verse;

public class SubEffecter_CameraShakeChance : SubEffecter_CameraShake
{
	private int lastEffectTicks;

	private int ticksUntilEffect;

	private int lifespanMaxTicks;

	public SubEffecter_CameraShakeChance(SubEffecterDef subDef, Effecter parent)
		: base(subDef, parent)
	{
		ticksUntilEffect = def.initialDelayTicks;
		lifespanMaxTicks = Find.TickManager.TicksGame + def.lifespanMaxTicks + ticksUntilEffect;
	}

	public override void SubEffectTick(TargetInfo A, TargetInfo B)
	{
		ticksUntilEffect--;
		float effectiveChancePerTick = base.EffectiveChancePerTick;
		if (Find.TickManager.TicksGame >= lastEffectTicks + def.chancePeriodTicks && ticksUntilEffect <= 0 && Find.TickManager.TicksGame < lifespanMaxTicks && Rand.Chance(effectiveChancePerTick))
		{
			lastEffectTicks = Find.TickManager.TicksGame;
			DoShake(A);
		}
	}
}
