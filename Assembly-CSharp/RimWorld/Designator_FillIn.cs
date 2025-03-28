using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Designator_FillIn : Designator
{
	protected override DesignationDef Designation => DesignationDefOf.FillIn;

	public Designator_FillIn()
	{
		soundSucceeded = SoundDefOf.Tick_Low;
		defaultLabel = "DesignatorFillIn".Translate();
		defaultDesc = "DesignatorFillInDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/FillInPitBurrow");
		showReverseDesignatorDisabledReason = true;
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (!ModsConfig.AnomalyActive)
		{
			return false;
		}
		if (!c.InBounds(base.Map))
		{
			return false;
		}
		if (c.Fogged(base.Map))
		{
			return false;
		}
		if (!(from t in c.GetThingList(base.Map)
			where CanDesignateThing(t).Accepted
			select t).Any())
		{
			return false;
		}
		return true;
	}

	public override void DesignateSingleCell(IntVec3 c)
	{
		List<Thing> thingList = c.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (CanDesignateThing(thingList[i]).Accepted)
			{
				DesignateThing(thingList[i]);
			}
		}
	}

	public override AcceptanceReport CanDesignateThing(Thing t)
	{
		return t is PitBurrow && base.Map.designationManager.DesignationOn(t, Designation) == null;
	}

	public override void DesignateThing(Thing t)
	{
		base.Map.designationManager.AddDesignation(new Designation(t, Designation));
	}
}
