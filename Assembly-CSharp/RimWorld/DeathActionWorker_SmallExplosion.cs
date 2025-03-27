using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class DeathActionWorker_SmallExplosion : DeathActionWorker
{
	public override RulePackDef DeathRules => RulePackDefOf.Transition_DiedExplosive;

	public override bool DangerousInMelee => true;

	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		GenExplosion.DoExplosion(corpse.Position, corpse.Map, 1.9f, DamageDefOf.Flame, corpse.InnerPawn, -1, -1f, null, null, null, null, null, 0f, 1, null, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, null);
	}
}
