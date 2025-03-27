using UnityEngine;
using Verse;

namespace RimWorld;

public class Designator_AreaSnowClearExpand : Designator_AreaSnowClear
{
	public Designator_AreaSnowClearExpand()
		: base(DesignateMode.Add)
	{
		defaultLabel = "DesignatorAreaSnowClearExpand".Translate();
		defaultDesc = "DesignatorAreaSnowClearExpandDesc".Translate();
		icon = ContentFinder<Texture2D>.Get("UI/Designators/SnowClearAreaOn");
		soundDragSustain = SoundDefOf.Designate_DragAreaAdd;
		soundDragChanged = SoundDefOf.Designate_DragZone_Changed;
		soundSucceeded = SoundDefOf.Designate_ZoneAdd_RemoveSnow;
	}
}
