using Verse;
using Verse.AI.Group;

namespace RimWorld;

public class DeathActionWorker_BigExplosion : DeathActionWorker
{
	public override RulePackDef DeathRules => RulePackDefOf.Transition_DiedExplosive;

	public override bool DangerousInMelee => true;

	public override void PawnDied(Corpse corpse, Lord prevLord)
	{
		GenExplosion.DoExplosion(radius: (corpse.InnerPawn.ageTracker.CurLifeStageIndex == 0) ? 1.9f : ((corpse.InnerPawn.ageTracker.CurLifeStageIndex != 1) ? 4.9f : 2.9f), center: corpse.Position, map: corpse.Map, damType: DamageDefOf.Flame, instigator: corpse.InnerPawn, damAmount: -1, armorPenetration: -1f, explosionSound: null, weapon: null, projectile: null, intendedTarget: null, postExplosionSpawnThingDef: null, postExplosionSpawnChance: 0f, postExplosionSpawnThingCount: 1, postExplosionGasType: null, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0f, damageFalloff: false, direction: null, ignoredThings: null, affectedAngle: null);
	}
}
