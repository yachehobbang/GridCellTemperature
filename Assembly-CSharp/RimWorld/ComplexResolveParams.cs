using System.Collections.Generic;
using Verse;

namespace RimWorld;

public struct ComplexResolveParams
{
	public List<Thing> spawnedThings;

	public List<CellRect> room;

	public List<List<CellRect>> allRooms;

	public float points;

	public Map map;

	public CellRect complexRect;

	public Faction hostileFaction;

	public string triggerSignal;

	public int? delayTicks;

	public bool passive;
}
