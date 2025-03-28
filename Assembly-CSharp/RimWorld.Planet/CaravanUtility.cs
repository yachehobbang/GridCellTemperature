using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld.Planet;

public static class CaravanUtility
{
	public static bool IsOwner(Pawn pawn, Faction caravanFaction)
	{
		if (caravanFaction == null)
		{
			Log.Warning("Called IsOwner with null faction.");
			return false;
		}
		if (!pawn.NonHumanlikeOrWildMan() && pawn.Faction == caravanFaction && pawn.HostFaction == null)
		{
			return !pawn.IsSlave;
		}
		return false;
	}

	public static Caravan GetCaravan(this Pawn pawn)
	{
		return pawn.ParentHolder as Caravan;
	}

	public static bool IsCaravanMember(this Pawn pawn)
	{
		return pawn.GetCaravan() != null;
	}

	public static bool IsPlayerControlledCaravanMember(this Pawn pawn)
	{
		return pawn.GetCaravan()?.IsPlayerControlled ?? false;
	}

	public static int BestGotoDestNear(int tile, Caravan c)
	{
		Predicate<int> predicate = delegate(int t)
		{
			if (Find.World.Impassable(t))
			{
				return false;
			}
			return c.CanReach(t) ? true : false;
		};
		if (predicate(tile))
		{
			return tile;
		}
		GenWorldClosest.TryFindClosestTile(tile, predicate, out var foundTile, 50);
		return foundTile;
	}

	public static bool PlayerHasAnyCaravan()
	{
		List<Caravan> caravans = Find.WorldObjects.Caravans;
		for (int i = 0; i < caravans.Count; i++)
		{
			if (caravans[i].IsPlayerControlled)
			{
				return true;
			}
		}
		return false;
	}

	public static Pawn RandomOwner(this Caravan caravan)
	{
		return caravan.PawnsListForReading.Where((Pawn p) => caravan.IsOwner(p)).RandomElement();
	}

	public static bool ShouldAutoCapture(Pawn p, Faction caravanFaction)
	{
		if (p.RaceProps.Humanlike && !p.Dead && p.Faction != caravanFaction)
		{
			if (p.IsPrisoner)
			{
				return p.HostFaction != caravanFaction;
			}
			return true;
		}
		return false;
	}

	public static int GetTileCurrentlyOver(this Caravan caravan)
	{
		if (caravan.pather.Moving && caravan.pather.IsNextTilePassable() && 1f - caravan.pather.nextTileCostLeft / caravan.pather.nextTileCostTotal > 0.5f)
		{
			return caravan.pather.nextTile;
		}
		return caravan.Tile;
	}

	public static bool IsInCaravan(Thing thing)
	{
		if (thing.ParentHolder is Caravan)
		{
			return true;
		}
		if (thing.ParentHolder is Pawn_InventoryTracker pawn_InventoryTracker)
		{
			return IsInCaravan(pawn_InventoryTracker.pawn);
		}
		if (thing.ParentHolder is Thing thing2)
		{
			return IsInCaravan(thing2);
		}
		return false;
	}
}
