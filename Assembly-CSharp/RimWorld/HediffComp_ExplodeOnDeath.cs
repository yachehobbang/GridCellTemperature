using Verse;

namespace RimWorld;

public class HediffComp_ExplodeOnDeath : HediffComp
{
	public HediffCompProperties_ExplodeOnDeath Props => (HediffCompProperties_ExplodeOnDeath)props;

	public override void Notify_PawnKilled()
	{
		GenExplosion.DoExplosion(base.Pawn.Position, base.Pawn.Map, Props.explosionRadius, Props.damageDef, base.Pawn, Props.damageAmount, -1f, null, null, null, null, null, 0f, 1, null, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, null, null);
		if (Props.destroyGear)
		{
			base.Pawn.equipment.DestroyAllEquipment();
			base.Pawn.apparel.DestroyAll();
		}
	}

	public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
	{
		if (Props.destroyBody)
		{
			base.Pawn.Corpse.Destroy();
		}
	}
}
