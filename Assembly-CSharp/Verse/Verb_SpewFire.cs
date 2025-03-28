using RimWorld;
using UnityEngine;

namespace Verse;

public class Verb_SpewFire : Verb
{
	protected override bool TryCastShot()
	{
		if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
		{
			return false;
		}
		if (base.EquipmentSource != null)
		{
			base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
			base.EquipmentSource.GetComp<CompApparelReloadable>()?.UsedOnce();
		}
		IntVec3 position = caster.Position;
		float num = Mathf.Atan2(-(currentTarget.Cell.z - position.z), currentTarget.Cell.x - position.x) * 57.29578f;
		GenExplosion.DoExplosion(affectedAngle: new FloatRange(num - 13f, num + 13f), center: position, map: caster.MapHeld, radius: verbProps.range, damType: DamageDefOf.Flame, instigator: caster, damAmount: -1, armorPenetration: -1f, explosionSound: null, weapon: null, projectile: null, intendedTarget: null, postExplosionSpawnThingDef: ThingDefOf.Filth_FlammableBile, postExplosionSpawnChance: 1f, postExplosionSpawnThingCount: 1, postExplosionGasType: null, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 1f, damageFalloff: false, direction: null, ignoredThings: null, doVisualEffects: false, propagationSpeed: 0.6f, excludeRadius: 0f, doSoundEffects: false);
		AddEffecterToMaintain(EffecterDefOf.Fire_SpewShort.Spawn(caster.Position, currentTarget.Cell, caster.Map), caster.Position, currentTarget.Cell, 14, caster.Map);
		lastShotTick = Find.TickManager.TicksGame;
		return true;
	}

	public override bool Available()
	{
		if (!base.Available())
		{
			return false;
		}
		if (CasterIsPawn)
		{
			Pawn casterPawn = CasterPawn;
			if (casterPawn.Faction != Faction.OfPlayer && casterPawn.mindState.MeleeThreatStillThreat && casterPawn.mindState.meleeThreat.Position.AdjacentTo8WayOrInside(casterPawn.Position))
			{
				return false;
			}
		}
		return true;
	}
}
