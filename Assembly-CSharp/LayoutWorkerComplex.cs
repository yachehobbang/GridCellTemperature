using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

public class LayoutWorkerComplex : LayoutWorker
{
	private static readonly FloatRange ThreatPointsFactorRange = new FloatRange(0.25f, 0.35f);

	private static readonly SimpleCurve EntranceCountOverAreaCurve = new SimpleCurve
	{
		new CurvePoint(0f, 1f),
		new CurvePoint(1000f, 1f),
		new CurvePoint(1500f, 2f),
		new CurvePoint(5000f, 3f),
		new CurvePoint(10000f, 4f)
	};

	private static readonly List<CellRect> tmpRoomMapRects = new List<CellRect>();

	private static readonly List<Thing> tmpSpawnedThreatThings = new List<Thing>();

	private static List<ComplexThreat> useableThreats = new List<ComplexThreat>();

	public new ComplexLayoutDef Def => (ComplexLayoutDef)base.Def;

	public LayoutWorkerComplex(LayoutDef def)
		: base(def)
	{
	}

	public virtual Faction GetFixedHostileFactionForThreats()
	{
		return null;
	}

	protected virtual void PreSpawnThreats(List<List<CellRect>> rooms, Map map, List<Thing> allSpawnedThings)
	{
	}

	protected override LayoutSketch GenerateSketch(StructureGenParams parms)
	{
		ThingDef thingDef = BaseGenUtility.RandomCheapWallStuff(parms.faction ?? Faction.OfAncients, notVeryFlammable: true);
		int entranceCount = GenMath.RoundRandom(EntranceCountOverAreaCurve.Evaluate(parms.size.Area));
		ComplexLayoutSketch complexLayoutSketch = new ComplexLayoutSketch();
		complexLayoutSketch.wallStuff = thingDef;
		complexLayoutSketch.doorStuff = thingDef;
		StructureLayout layout = RoomLayoutGenerator.GenerateRandomLayout(new CellRect(0, 0, parms.size.x, parms.size.z), 4, 4, 0.2f, null, null, entranceCount);
		complexLayoutSketch.layout = layout;
		complexLayoutSketch.FlushLayoutToSketch();
		return complexLayoutSketch;
	}

	public override void Spawn(LayoutStructureSketch structureSketch, Map map, IntVec3 center, float? threatPoints = null, List<Thing> allSpawnedThings = null, bool roofs = true)
	{
		List<Thing> list = allSpawnedThings ?? new List<Thing>();
		base.Spawn(structureSketch, map, center, threatPoints, list, roofs);
		List<List<CellRect>> list2 = new List<List<CellRect>>();
		List<LayoutRoom> rooms = structureSketch.structureLayout.Rooms;
		for (int i = 0; i < rooms.Count; i++)
		{
			tmpRoomMapRects.Clear();
			for (int j = 0; j < rooms[i].rects.Count; j++)
			{
				tmpRoomMapRects.Add(rooms[i].rects[j].MovedBy(center));
			}
			List<CellRect> list3 = new List<CellRect>();
			for (int k = 0; k < tmpRoomMapRects.Count; k++)
			{
				CellRect item = LargestAreaFinder.ExpandRect(tmpRoomMapRects[k], map, new HashSet<IntVec3>(), (IntVec3 c) => CanExpand(c, map));
				list3.Add(item);
			}
			list2.Add(list3);
			tmpRoomMapRects.Clear();
		}
		if (!structureSketch.thingsToSpawn.NullOrEmpty())
		{
			HashSet<List<CellRect>> usedRooms = new HashSet<List<CellRect>>();
			for (int num = structureSketch.thingsToSpawn.Count - 1; num >= 0; num--)
			{
				Thing thing = structureSketch.thingsToSpawn[num];
				List<CellRect> roomUsed;
				Rot4 rotUsed;
				IntVec3 loc = LayoutWorker.FindBestSpawnLocation(list2, thing.def, map, out roomUsed, out rotUsed, usedRooms);
				if (!loc.IsValid)
				{
					loc = LayoutWorker.FindBestSpawnLocation(list2, thing.def, map, out roomUsed, out rotUsed);
				}
				if (!loc.IsValid)
				{
					thing.Destroy();
					structureSketch.thingsToSpawn.RemoveAt(num);
				}
				else
				{
					GenSpawn.Spawn(thing, loc, map, rotUsed);
					list.Add(thing);
					structureSketch.thingsToSpawn.RemoveAt(num);
					if (!structureSketch.thingDiscoveredMessage.NullOrEmpty())
					{
						string signalTag = "ThingDiscovered" + Find.UniqueIDsManager.GetNextSignalTagID();
						foreach (CellRect item2 in roomUsed)
						{
							RectTrigger obj = (RectTrigger)ThingMaker.MakeThing(ThingDefOf.RectTrigger);
							obj.signalTag = signalTag;
							obj.Rect = item2;
							GenSpawn.Spawn(obj, item2.CenterCell, map);
						}
						SignalAction_Message obj2 = (SignalAction_Message)ThingMaker.MakeThing(ThingDefOf.SignalAction_Message);
						obj2.signalTag = signalTag;
						obj2.message = structureSketch.thingDiscoveredMessage;
						obj2.messageType = MessageTypeDefOf.PositiveEvent;
						obj2.lookTargets = thing;
						GenSpawn.Spawn(obj2, loc, map);
					}
				}
			}
		}
		if (threatPoints.HasValue && !Def.threats.NullOrEmpty())
		{
			PreSpawnThreats(list2, map, list);
			SpawnThreats(structureSketch, map, center, threatPoints.Value, list, list2);
		}
		PostSpawnStructure(list2, map, list);
		tmpSpawnedThreatThings.Clear();
		static bool CanExpand(IntVec3 c, Map m)
		{
			Building edifice = c.GetEdifice(m);
			if (edifice != null && (edifice.def == ThingDefOf.Wall || edifice.def == ThingDefOf.Door))
			{
				return true;
			}
			return false;
		}
	}

	protected virtual void PostSpawnStructure(List<List<CellRect>> rooms, Map map, List<Thing> allSpawnedThings)
	{
		if (ModsConfig.IdeologyActive)
		{
			SpawnRoomRewards(rooms, map, allSpawnedThings);
			SpawnCommsConsole(rooms, map);
		}
	}

	private static void SpawnCommsConsole(List<List<CellRect>> rooms, Map map)
	{
		foreach (List<CellRect> item in rooms.InRandomOrder())
		{
			foreach (IntVec3 item2 in item.SelectMany((CellRect r) => r.Cells).InRandomOrder())
			{
				if (CanPlaceCommsConsoleAt(item2, map))
				{
					GenSpawn.Spawn(ThingDefOf.AncientCommsConsole, item2, map);
					return;
				}
			}
		}
	}

	private static bool CanPlaceCommsConsoleAt(IntVec3 cell, Map map)
	{
		foreach (IntVec3 item in GenAdj.OccupiedRect(cell, Rot4.North, ThingDefOf.AncientCommsConsole.Size).ExpandedBy(1))
		{
			if (item.GetEdifice(map) != null)
			{
				return false;
			}
		}
		return true;
	}

	private void SpawnRoomRewards(List<List<CellRect>> rooms, Map map, List<Thing> allSpawnedThings)
	{
		if (!(Def.roomRewardCrateFactor > 0f))
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < allSpawnedThings.Count; i++)
		{
			if (allSpawnedThings[i] is Building_Crate)
			{
				num++;
			}
		}
		int num2 = Mathf.RoundToInt((float)rooms.Count * Def.roomRewardCrateFactor) - num;
		if (num2 <= 0)
		{
			return;
		}
		ThingSetMakerDef thingSetMakerDef = Def.rewardThingSetMakerDef ?? ThingSetMakerDefOf.Reward_ItemsStandard;
		foreach (List<CellRect> item in rooms.InRandomOrder())
		{
			bool flag = true;
			List<IntVec3> list = item.SelectMany((CellRect r) => r.Cells).ToList();
			foreach (IntVec3 item2 in list)
			{
				Building edifice = item2.GetEdifice(map);
				if (edifice != null && edifice is Building_Crate)
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				continue;
			}
			if (ComplexUtility.TryFindRandomSpawnCell(ThingDefOf.AncientHermeticCrate, list, map, out var spawnPosition, 1, Rot4.South))
			{
				Building_Crate building_Crate = (Building_Crate)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.AncientHermeticCrate), spawnPosition, map, Rot4.South);
				List<Thing> list2 = thingSetMakerDef.root.Generate(default(ThingSetMakerParams));
				for (int num3 = list2.Count - 1; num3 >= 0; num3--)
				{
					Thing thing = list2[num3];
					if (!building_Crate.TryAcceptThing(thing, allowSpecialEffects: false))
					{
						thing.Destroy();
					}
				}
				num2--;
			}
			if (num2 <= 0)
			{
				break;
			}
		}
	}

	private void SpawnThreats(LayoutStructureSketch structureSketch, Map map, IntVec3 center, float threatPoints, List<Thing> spawnedThings, List<List<CellRect>> roomRects)
	{
		ComplexResolveParams threatParams = new ComplexResolveParams
		{
			map = map,
			complexRect = structureSketch.layoutSketch.OccupiedRect.MovedBy(center),
			hostileFaction = GetFixedHostileFactionForThreats(),
			allRooms = roomRects,
			points = threatPoints
		};
		StringBuilder stringBuilder = null;
		if (DebugViewSettings.logComplexGenPoints)
		{
			stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("----- Logging points for " + Def.defName + ". -----");
			stringBuilder.AppendLine($"Total threat points: {threatPoints}");
			stringBuilder.AppendLine($"Room count: {roomRects.Count}");
			stringBuilder.AppendLine($"Approx points per room: {threatParams.points}");
			if (threatParams.hostileFaction != null)
			{
				stringBuilder.AppendLine($"Faction: {threatParams.hostileFaction}");
			}
		}
		useableThreats.Clear();
		useableThreats.AddRange(Def.threats.Where((ComplexThreat t) => Rand.Chance(t.chancePerComplex)));
		float num = 0f;
		int num2 = 100;
		Dictionary<List<CellRect>, List<ComplexThreatDef>> usedThreatsByRoom = new Dictionary<List<CellRect>, List<ComplexThreatDef>>();
		while (num < threatPoints && num2 > 0)
		{
			num2--;
			List<CellRect> room = roomRects.RandomElement();
			threatParams.room = room;
			threatParams.spawnedThings = spawnedThings;
			float b = threatPoints - num;
			threatParams.points = Mathf.Min(ThreatPointsFactorRange.RandomInRange * threatPoints, b);
			if (useableThreats.Where(delegate(ComplexThreat t)
			{
				int num3 = 0;
				foreach (KeyValuePair<List<CellRect>, List<ComplexThreatDef>> item in usedThreatsByRoom)
				{
					num3 += item.Value.Count((ComplexThreatDef td) => td == t.def);
				}
				if (num3 >= t.maxPerComplex)
				{
					return false;
				}
				return (!usedThreatsByRoom.ContainsKey(room) || usedThreatsByRoom[room].Count((ComplexThreatDef td) => td == t.def) < t.maxPerRoom) && t.def.Worker.CanResolve(threatParams);
			}).TryRandomElementByWeight((ComplexThreat t) => t.selectionWeight, out var result))
			{
				if (stringBuilder != null)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("-> Resolving threat " + result.def.defName);
				}
				float threatPointsUsed = 0f;
				result.def.Worker.Resolve(threatParams, ref threatPointsUsed, tmpSpawnedThreatThings, stringBuilder);
				num += threatPointsUsed;
				if (!usedThreatsByRoom.ContainsKey(room))
				{
					usedThreatsByRoom[room] = new List<ComplexThreatDef>();
				}
				usedThreatsByRoom[room].Add(result.def);
			}
		}
		if (stringBuilder != null)
		{
			stringBuilder.AppendLine($"Total threat points spent: {num}");
			Log.Message(stringBuilder.ToString());
		}
	}
}
