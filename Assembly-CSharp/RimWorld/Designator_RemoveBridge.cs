using UnityEngine;
using Verse;

namespace RimWorld;

public class Designator_RemoveBridge : Designator_RemoveFloor
{
	public override float Order => TerrainDefOf.Bridge.uiOrder + 1f;

	public Designator_RemoveBridge()
	{
		defaultLabel = "DesignatorRemoveBridge".Translate();
		defaultDesc = "DesignatorRemoveBridgeDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/RemoveBridge");
		soundSucceeded = SoundDefOf.Designate_RemoveBridge;
		hotKey = KeyBindingDefOf.Misc5;
	}

	public override AcceptanceReport CanDesignateCell(IntVec3 c)
	{
		if (c.InBounds(base.Map) && !c.GetTerrain(base.Map).bridge)
		{
			return false;
		}
		return base.CanDesignateCell(c);
	}
}
