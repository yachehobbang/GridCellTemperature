namespace Verse;

public class Projectile_HellsphereCannon : Projectile
{
	private const float ExtraExplosionRadius = 4.9f;

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		Map map = base.Map;
		base.Impact(hitThing, blockedByShield);
		GenExplosion.DoExplosion(base.Position, map, 4.9f, def.projectile.damageDef, launcher, DamageAmount, ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing, null, 0f, 1, null, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, def.projectile.explosionChanceToStartFire, damageFalloff: false, null, null, null, doVisualEffects: true, def.projectile.damageDef.expolosionPropagationSpeed);
	}
}
