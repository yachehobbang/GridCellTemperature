using System;
using System.Collections.Generic;
using RimWorld;

namespace Verse;

public sealed class ListerThings
{
	private Dictionary<ThingDef, List<Thing>> listsByDef = new Dictionary<ThingDef, List<Thing>>(ThingDefComparer.Instance);

	private List<Thing>[] listsByGroup;

	private int[] stateHashByGroup;

	public ListerThingsUse use;

	public ThingListChangedCallbacks thingListChangedCallbacks;

	private static readonly List<Thing> EmptyList = new List<Thing>();

	private static List<Thing> tmpThingsMatchingFilter = new List<Thing>(1024);

	public List<Thing> AllThings => listsByGroup[2];

	public ListerThings(ListerThingsUse use, ThingListChangedCallbacks thingListChangedCallbacks = null)
	{
		this.use = use;
		this.thingListChangedCallbacks = thingListChangedCallbacks;
		listsByGroup = new List<Thing>[ThingListGroupHelper.AllGroups.Length];
		stateHashByGroup = new int[ThingListGroupHelper.AllGroups.Length];
		listsByGroup[2] = new List<Thing>();
	}

	public List<Thing> ThingsInGroup(ThingRequestGroup group)
	{
		return ThingsMatching(ThingRequest.ForGroup(group));
	}

	public int StateHashOfGroup(ThingRequestGroup group)
	{
		if (use == ListerThingsUse.Region && !group.StoreInRegion())
		{
			Log.ErrorOnce(string.Concat("Tried to get state hash of group ", group, " in a region, but this group is never stored in regions. Most likely a global query should have been used."), 1968738832);
			return -1;
		}
		return Gen.HashCombineInt(85693994, stateHashByGroup[(uint)group]);
	}

	public List<Thing> ThingsOfDef(ThingDef def)
	{
		return ThingsMatching(ThingRequest.ForDef(def));
	}

	public bool AnyThingWithDef(ThingDef def)
	{
		if (listsByDef.ContainsKey(def))
		{
			return listsByDef[def].Count > 0;
		}
		return false;
	}

	public List<Thing> ThingsMatching(ThingRequest req)
	{
		if (req.singleDef != null)
		{
			if (!listsByDef.TryGetValue(req.singleDef, out var value))
			{
				return EmptyList;
			}
			return value;
		}
		if (req.group != 0)
		{
			if (use == ListerThingsUse.Region && !req.group.StoreInRegion())
			{
				Log.ErrorOnce(string.Concat("Tried to get things in group ", req.group, " in a region, but this group is never stored in regions. Most likely a global query should have been used."), 1968735132);
				return EmptyList;
			}
			return listsByGroup[(uint)req.group] ?? EmptyList;
		}
		throw new InvalidOperationException("Invalid ThingRequest " + req);
	}

	public List<Thing> ThingsMatchingFilter(ThingFilter filter)
	{
		tmpThingsMatchingFilter.Clear();
		foreach (ThingDef allowedThingDef in filter.AllowedThingDefs)
		{
			tmpThingsMatchingFilter.AddRange(ThingsOfDef(allowedThingDef));
		}
		return tmpThingsMatchingFilter;
	}

	public void GetThingsOfType<T>(List<T> list) where T : Thing
	{
		if (typeof(T) == typeof(Thing))
		{
			Log.Error("Do not call this method with type 'Thing' directly, as it will return all things currently registered.");
			return;
		}
		List<Thing> allThings = AllThings;
		for (int i = 0; i < AllThings.Count; i++)
		{
			if (allThings[i] is T item)
			{
				list.Add(item);
			}
		}
	}

	public IEnumerable<T> GetThingsOfType<T>() where T : Thing
	{
		if (typeof(T) == typeof(Thing))
		{
			Log.Error("Do not call this method with type 'Thing' directly, as it will return all things currently registered.");
			yield break;
		}
		List<Thing> things = AllThings;
		for (int i = 0; i < AllThings.Count; i++)
		{
			if (things[i] is T val)
			{
				yield return val;
			}
		}
	}

	public IEnumerable<Thing> GetAllThings(Predicate<Thing> validator = null, bool lookInHaulSources = false)
	{
		foreach (Thing thing in AllThings)
		{
			if (validator == null || validator(thing))
			{
				yield return thing;
			}
			if (!lookInHaulSources || !(thing is IHaulSource haulSource))
			{
				continue;
			}
			foreach (Thing item in (IEnumerable<Thing>)haulSource.GetDirectlyHeldThings())
			{
				if (validator == null || validator(item))
				{
					yield return item;
				}
			}
		}
	}

	public void GetAllThings(in List<Thing> list, Predicate<Thing> validator = null, bool lookInHaulSources = false)
	{
		foreach (Thing allThing in AllThings)
		{
			if (validator == null || validator(allThing))
			{
				list.Add(allThing);
			}
			if (!lookInHaulSources || !(allThing is IHaulSource haulSource))
			{
				continue;
			}
			foreach (Thing item in (IEnumerable<Thing>)haulSource.GetDirectlyHeldThings())
			{
				if (validator == null || validator(item))
				{
					list.Add(item);
				}
			}
		}
	}

	public bool Contains(Thing t)
	{
		return AllThings.Contains(t);
	}

	public void Add(Thing t)
	{
		if (!EverListable(t.def, use))
		{
			return;
		}
		if (!listsByDef.TryGetValue(t.def, out var value))
		{
			value = new List<Thing>();
			listsByDef.Add(t.def, value);
		}
		value.Add(t);
		ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
		foreach (ThingRequestGroup thingRequestGroup in allGroups)
		{
			if ((use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(t.def))
			{
				List<Thing> list = listsByGroup[(uint)thingRequestGroup];
				if (list == null)
				{
					list = new List<Thing>();
					listsByGroup[(uint)thingRequestGroup] = list;
					stateHashByGroup[(uint)thingRequestGroup] = 0;
				}
				list.Add(t);
				stateHashByGroup[(uint)thingRequestGroup]++;
			}
		}
		thingListChangedCallbacks?.onThingAdded?.Invoke(t);
	}

	public void Remove(Thing t)
	{
		if (!EverListable(t.def, use))
		{
			return;
		}
		listsByDef[t.def].Remove(t);
		ThingRequestGroup[] allGroups = ThingListGroupHelper.AllGroups;
		for (int i = 0; i < allGroups.Length; i++)
		{
			ThingRequestGroup thingRequestGroup = allGroups[i];
			if ((use != ListerThingsUse.Region || thingRequestGroup.StoreInRegion()) && thingRequestGroup.Includes(t.def))
			{
				listsByGroup[i].Remove(t);
				stateHashByGroup[(uint)thingRequestGroup]++;
			}
		}
		thingListChangedCallbacks?.onThingRemoved?.Invoke(t);
	}

	public static bool EverListable(ThingDef def, ListerThingsUse use)
	{
		if (def.category == ThingCategory.Mote && (!def.drawGUIOverlay || use == ListerThingsUse.Region))
		{
			return false;
		}
		if (def.category == ThingCategory.Projectile && use == ListerThingsUse.Region)
		{
			return false;
		}
		return true;
	}

	public void Clear()
	{
		listsByDef.Clear();
		for (int i = 0; i < listsByGroup.Length; i++)
		{
			if (listsByGroup[i] != null)
			{
				listsByGroup[i].Clear();
			}
			stateHashByGroup[i] = 0;
		}
	}
}
