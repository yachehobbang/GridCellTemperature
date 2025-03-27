using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse.AI;

namespace Verse;

public class CleanRoomFilthUtility
{
	public static IEnumerable<Filth> GetRoomFilthCleanableByPawn(IntVec3 clickCell, Pawn pawn)
	{
		if (!clickCell.IsValid)
		{
			return null;
		}
		if (pawn.Map.IsPocketMap && pawn.Map.Parent is PocketMapParent { canBeCleaned: false })
		{
			return null;
		}
		Room room = RegionAndRoomQuery.RoomAt(clickCell, pawn.Map);
		if (room == null || room.Dereferenced || room.Fogged)
		{
			return null;
		}
		if (room.IsHuge || room.TouchesMapEdge)
		{
			return null;
		}
		return from f in room.ContainedAndAdjacentThings.OfType<Filth>()
			where !f.IsForbidden(pawn) && f.Map.areaManager.Home[f.Position] && pawn.CanReach(f, PathEndMode.Touch, pawn.NormalMaxDanger()) && pawn.CanReserve(f)
			select f;
	}
}
