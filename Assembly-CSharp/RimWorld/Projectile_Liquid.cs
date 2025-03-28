using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class Projectile_Liquid : Projectile
{
	private Material materialResolved;

	public override Material DrawMat => materialResolved;

	protected override void Impact(Thing hitThing, bool blockedByShield = false)
	{
		DoImpact(hitThing, base.Position);
		if (!blockedByShield && !def.projectile.soundImpact.NullOrUndefined())
		{
			def.projectile.soundImpact.PlayOneShot(SoundInfo.InMap(this));
		}
		for (int i = 0; i < def.projectile.numExtraHitCells; i++)
		{
			IntVec3 intVec = base.Position + GenAdj.AdjacentCellsAndInside[i];
			if (intVec.InBounds(base.Map))
			{
				DoImpact(hitThing, intVec);
			}
		}
		base.Impact(hitThing, blockedByShield);
	}

	private void DoImpact(Thing hitThing, IntVec3 cell)
	{
		if (def.projectile.filth != null && def.projectile.filthCount.TrueMax > 0 && !cell.Filled(base.Map))
		{
			FilthMaker.TryMakeFilth(cell, base.Map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
		}
		List<Thing> thingList = cell.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing thing = thingList[i];
			if (!(thing is Mote) && !(thing is Filth) && thing != hitThing)
			{
				Find.BattleLog.Add(new BattleLogEntry_RangedImpact(launcher, thing, thing, equipmentDef, def, targetCoverDef));
				DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, def.projectile.GetDamageAmount(null), def.projectile.GetArmorPenetration(null), -1f, launcher);
				thing.TakeDamage(dinfo);
			}
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		if (materialResolved == null)
		{
			materialResolved = def.DrawMatSingle;
		}
		base.DrawAt(drawLoc, flip);
	}
}
