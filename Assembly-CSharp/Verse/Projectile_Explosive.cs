namespace Verse;

public class Projectile_Explosive : Projectile
{
	private int ticksToDetonation;

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0);
	}

	public override void Tick()
	{
		base.Tick();
		if (ticksToDetonation > 0)
		{
			ticksToDetonation--;
			if (ticksToDetonation <= 0)
			{
				Explode();
			}
		}
	}

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		if (blockedByShield || def.projectile.explosionDelay == 0)
		{
			Explode();
			return;
		}
		landed = true;
		ticksToDetonation = def.projectile.explosionDelay;
		GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, def.projectile.damageDef, launcher.Faction, launcher);
	}

	protected virtual void Explode()
	{
		Map map = base.Map;
		Destroy();
		if (def.projectile.explosionEffect != null)
		{
			Effecter effecter = def.projectile.explosionEffect.Spawn();
			if (def.projectile.explosionEffectLifetimeTicks != 0)
			{
				map.effecterMaintainer.AddEffecterToMaintain(effecter, base.Position.ToVector3().ToIntVec3(), def.projectile.explosionEffectLifetimeTicks);
			}
			else
			{
				effecter.Trigger(new TargetInfo(base.Position, map), new TargetInfo(base.Position, map));
				effecter.Cleanup();
			}
		}
		IntVec3 position = base.Position;
		float explosionRadius = def.projectile.explosionRadius;
		DamageDef damageDef = def.projectile.damageDef;
		Thing instigator = launcher;
		int damageAmount = DamageAmount;
		float armorPenetration = ArmorPenetration;
		SoundDef soundExplode = def.projectile.soundExplode;
		ThingDef weapon = equipmentDef;
		ThingDef projectile = def;
		Thing thing = intendedTarget.Thing;
		ThingDef postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef ?? def.projectile.filth;
		ThingDef postExplosionSpawnThingDefWater = def.projectile.postExplosionSpawnThingDefWater;
		float postExplosionSpawnChance = def.projectile.postExplosionSpawnChance;
		int postExplosionSpawnThingCount = def.projectile.postExplosionSpawnThingCount;
		GasType? postExplosionGasType = def.projectile.postExplosionGasType;
		ThingDef preExplosionSpawnThingDef = def.projectile.preExplosionSpawnThingDef;
		float preExplosionSpawnChance = def.projectile.preExplosionSpawnChance;
		int preExplosionSpawnThingCount = def.projectile.preExplosionSpawnThingCount;
		GenExplosion.DoExplosion(applyDamageToExplosionCellsNeighbors: def.projectile.applyDamageToExplosionCellsNeighbors, preExplosionSpawnThingDef: preExplosionSpawnThingDef, preExplosionSpawnChance: preExplosionSpawnChance, preExplosionSpawnThingCount: preExplosionSpawnThingCount, chanceToStartFire: def.projectile.explosionChanceToStartFire, damageFalloff: def.projectile.explosionDamageFalloff, direction: origin.AngleToFlat(destination), ignoredThings: null, affectedAngle: null, propagationSpeed: def.projectile.damageDef.expolosionPropagationSpeed, screenShakeFactor: def.projectile.screenShakeFactor, center: position, map: map, radius: explosionRadius, damType: damageDef, instigator: instigator, damAmount: damageAmount, armorPenetration: armorPenetration, explosionSound: soundExplode, weapon: weapon, projectile: projectile, intendedTarget: thing, postExplosionSpawnThingDef: postExplosionSpawnThingDef, postExplosionSpawnChance: postExplosionSpawnChance, postExplosionSpawnThingCount: postExplosionSpawnThingCount, postExplosionGasType: postExplosionGasType, doVisualEffects: def.projectile.doExplosionVFX, excludeRadius: 0f, doSoundEffects: true, postExplosionSpawnThingDefWater: postExplosionSpawnThingDefWater);
	}
}
