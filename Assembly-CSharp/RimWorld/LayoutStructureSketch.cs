using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class LayoutStructureSketch : IExposable
{
	public LayoutSketch layoutSketch;

	public StructureLayout structureLayout;

	public List<Thing> thingsToSpawn = new List<Thing>();

	public string thingDiscoveredMessage;

	public LayoutDef layoutDef;

	public void ExposeData()
	{
		Scribe_Deep.Look(ref layoutSketch, "layoutSketch");
		Scribe_Deep.Look(ref structureLayout, "structureLayout");
		Scribe_Collections.Look(ref thingsToSpawn, "thingsToSpawn", LookMode.Deep);
		Scribe_Defs.Look(ref layoutDef, "layoutDef");
		Scribe_Values.Look(ref thingDiscoveredMessage, "thingDiscoveredMessage");
	}
}
