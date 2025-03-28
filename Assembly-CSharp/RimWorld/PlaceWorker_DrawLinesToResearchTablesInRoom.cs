using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class PlaceWorker_DrawLinesToResearchTablesInRoom : PlaceWorker
{
	private static readonly List<Thing> tmpLinkedThings = new List<Thing>();

	public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
	{
		Room room = center.GetRoom(Find.CurrentMap);
		if (room == null || !room.ProperRoom || room.PsychologicallyOutdoors)
		{
			return;
		}
		room.DrawFieldEdges();
		foreach (Region region in room.Regions)
		{
			foreach (Thing item in region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
			{
				if (item is Building_ResearchBench)
				{
					GenDraw.DrawLineBetween(center.ToVector3Shifted(), item.TrueCenter());
				}
			}
		}
	}

	public override void DrawPlaceMouseAttachments(float curX, ref float curY, BuildableDef bdef, IntVec3 center, Rot4 rot)
	{
		tmpLinkedThings.Clear();
		Room room = center.GetRoom(Find.CurrentMap);
		if (room != null)
		{
			foreach (Region region in room.Regions)
			{
				foreach (Thing item in region.ListerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
				{
					if (!tmpLinkedThings.Contains(item) && item is Building_ResearchBench)
					{
						tmpLinkedThings.Add(item);
						if (tmpLinkedThings.Count == 1)
						{
							DrawTextLine(ref curY, "FacilityPotentiallyLinkedTo".Translate() + ":");
						}
						DrawTextLine(ref curY, "  - " + item.LabelCap);
					}
				}
			}
		}
		base.DrawPlaceMouseAttachments(curX, ref curY, bdef, center, rot);
		void DrawTextLine(ref float y, string text)
		{
			float lineHeight = Text.LineHeight;
			Widgets.Label(new Rect(curX, y, 999f, lineHeight), text);
			y += lineHeight;
		}
	}
}
