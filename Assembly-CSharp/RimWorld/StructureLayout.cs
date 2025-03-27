using System.Collections.Generic;
using System.Linq;
using DelaunatorSharp;
using UnityEngine;
using Verse;

namespace RimWorld;

public class StructureLayout : IExposable
{
	public CellRect container;

	private List<LayoutRoom> rooms = new List<LayoutRoom>();

	public IntVec3 offset;

	public RoomLayoutCellType[,] cellTypes;

	public int[,] roomIds;

	public Delaunator delaunator;

	public RelativeNeighborhoodGraph neighbours;

	private int currentRoomId;

	private const int MinAdjacencyForDisconnectedRoom = 3;

	private static readonly List<LayoutRoom> tmpRooms = new List<LayoutRoom>();

	private static readonly Queue<LayoutRoom> tmpRoomQueue = new Queue<LayoutRoom>();

	private static readonly HashSet<LayoutRoom> tmpSeenRooms = new HashSet<LayoutRoom>();

	public List<LayoutRoom> Rooms => rooms;

	public int Width => roomIds.GetLength(0);

	public int Height => roomIds.GetLength(1);

	public int Area
	{
		get
		{
			int num = 0;
			for (int i = 0; i < rooms.Count; i++)
			{
				num += rooms[i].Area;
			}
			return num;
		}
	}

	public void Init(CellRect rect)
	{
		container = rect;
		cellTypes = new RoomLayoutCellType[rect.Width, rect.Height];
		roomIds = new int[rect.Width, rect.Height];
		for (int i = 0; i < rect.Width; i++)
		{
			for (int j = 0; j < rect.Height; j++)
			{
				roomIds[i, j] = -1;
			}
		}
	}

	public bool HasRoomWithDef(LayoutRoomDef def)
	{
		return GetFirstRoomOfDef(def) != null;
	}

	public bool TryGetFirstRoomOfDef(LayoutRoomDef def, out LayoutRoom room)
	{
		room = GetFirstRoomOfDef(def);
		return room != null;
	}

	public LayoutRoom GetFirstRoomOfDef(LayoutRoomDef def)
	{
		foreach (LayoutRoom room in rooms)
		{
			if (room.defs != null && room.defs.Contains(def))
			{
				return room;
			}
		}
		return null;
	}

	public LayoutRoom AddRoom(List<CellRect> rects)
	{
		for (int i = 0; i < rects.Count; i++)
		{
			rects[i] = rects[i].ClipInsideRect(container);
		}
		LayoutRoom layoutRoom = new LayoutRoom(rects);
		rooms.Add(layoutRoom);
		return layoutRoom;
	}

	public void Add(IntVec3 position, RoomLayoutCellType cellType)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			cellTypes[position.x, position.z] = cellType;
		}
	}

	public bool IsGoodForHorizontalDoor(IntVec3 p)
	{
		if (IsWallAt(p + IntVec3.West) && IsWallAt(p + IntVec3.East) && !IsWallAt(p + IntVec3.North))
		{
			return !IsWallAt(p + IntVec3.South);
		}
		return false;
	}

	public bool IsGoodForVerticalDoor(IntVec3 p)
	{
		if (IsWallAt(p + IntVec3.North) && IsWallAt(p + IntVec3.South) && !IsWallAt(p + IntVec3.East))
		{
			return !IsWallAt(p + IntVec3.West);
		}
		return false;
	}

	public bool IsGoodForDoor(IntVec3 p)
	{
		if (!IsGoodForHorizontalDoor(p))
		{
			return IsGoodForVerticalDoor(p);
		}
		return true;
	}

	public bool IsWallAt(IntVec3 position)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			return cellTypes[position.x, position.z] == RoomLayoutCellType.Wall;
		}
		return false;
	}

	public bool IsFloorAt(IntVec3 position)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			return cellTypes[position.x, position.z] == RoomLayoutCellType.Floor;
		}
		return false;
	}

	public bool IsDoorAt(IntVec3 position)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			return cellTypes[position.x, position.z] == RoomLayoutCellType.Door;
		}
		return false;
	}

	public bool IsEmptyAt(IntVec3 position)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			return cellTypes[position.x, position.z] == RoomLayoutCellType.Empty;
		}
		return false;
	}

	public bool IsOutside(IntVec3 position)
	{
		if (cellTypes.InBounds(position.x, position.z))
		{
			return cellTypes[position.x, position.z] == RoomLayoutCellType.Empty;
		}
		return true;
	}

	public int GetRoomIdAt(IntVec3 position)
	{
		if (!roomIds.InBounds(position.x, position.z))
		{
			return -2;
		}
		return roomIds[position.x, position.z];
	}

	public bool TryMinimizeLayoutWithoutDisconnection()
	{
		if (rooms.Count == 1)
		{
			return false;
		}
		for (int num = rooms.Count - 1; num >= 0; num--)
		{
			if (IsAdjacentToLayoutEdge(rooms[num]) && !WouldDisconnectRoomsIfRemoved(rooms[num]))
			{
				rooms.RemoveAt(num);
				return true;
			}
		}
		return false;
	}

	private bool WouldDisconnectRoomsIfRemoved(LayoutRoom room)
	{
		tmpRooms.Clear();
		tmpRooms.AddRange(rooms);
		tmpRooms.Remove(room);
		tmpSeenRooms.Clear();
		tmpRoomQueue.Clear();
		tmpRoomQueue.Enqueue(tmpRooms.First());
		while (tmpRoomQueue.Count > 0)
		{
			LayoutRoom layoutRoom = tmpRoomQueue.Dequeue();
			tmpSeenRooms.Add(layoutRoom);
			foreach (LayoutRoom tmpRoom in tmpRooms)
			{
				if (layoutRoom != tmpRoom && !tmpSeenRooms.Contains(tmpRoom) && layoutRoom.IsAdjacentTo(tmpRoom, 3))
				{
					tmpRoomQueue.Enqueue(tmpRoom);
				}
			}
		}
		int count = tmpRooms.Count;
		int count2 = tmpSeenRooms.Count;
		tmpRooms.Clear();
		tmpSeenRooms.Clear();
		return count2 != count;
	}

	public bool IsAdjacentToLayoutEdge(LayoutRoom room)
	{
		for (int i = 0; i < room.rects.Count; i++)
		{
			if (room.rects[i].minX == container.minX || room.rects[i].maxX == container.maxX || room.rects[i].minZ == container.minZ || room.rects[i].maxZ == container.maxZ)
			{
				return true;
			}
		}
		return false;
	}

	public void FinalizeRooms(bool avoidDoubleWalls = true)
	{
		for (int i = 0; i < 4; i++)
		{
			Rot4 dir = new Rot4(i);
			foreach (LayoutRoom room in rooms)
			{
				for (int j = 0; j < room.rects.Count; j++)
				{
					foreach (IntVec3 edgeCell in room.rects[j].GetEdgeCells(dir))
					{
						IntVec3 facingCell = dir.FacingCell + edgeCell;
						if (avoidDoubleWalls && (IsWallAt(facingCell) || room.rects.Any((CellRect r) => r.Contains(facingCell))))
						{
							continue;
						}
						bool flag = false;
						foreach (CellRect rect in room.rects)
						{
							if (!(rect == room.rects[j]) && rect.Contains(edgeCell))
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							Add(edgeCell, RoomLayoutCellType.Wall);
						}
					}
				}
			}
		}
		foreach (LayoutRoom room2 in rooms)
		{
			foreach (CellRect rect2 in room2.rects)
			{
				foreach (IntVec3 cell in rect2.Cells)
				{
					roomIds[cell.x, cell.z] = currentRoomId;
					room2.id = currentRoomId;
					if (!IsWallAt(cell))
					{
						Add(cell, RoomLayoutCellType.Floor);
					}
				}
			}
			currentRoomId++;
		}
		for (int k = container.minX; k < container.maxX; k++)
		{
			for (int l = container.minZ; l < container.maxZ; l++)
			{
				IntVec3 intVec = new IntVec3(k, 0, l);
				if (IsWallAt(intVec) || !IsFloorAt(intVec))
				{
					continue;
				}
				int num = 0;
				IntVec3[] cardinalDirections = GenAdj.CardinalDirections;
				foreach (IntVec3 intVec2 in cardinalDirections)
				{
					if (IsWallAt(intVec + intVec2))
					{
						num++;
					}
				}
				int num2 = 0;
				int num3 = 0;
				cardinalDirections = GenAdj.DiagonalDirections;
				foreach (IntVec3 intVec3 in cardinalDirections)
				{
					if (IsWallAt(intVec + intVec3))
					{
						num2++;
					}
					else if (!IsFloorAt(intVec + intVec3))
					{
						num3++;
					}
				}
				if (num > 1 && (num2 < 2 || num3 > 0))
				{
					Add(intVec, RoomLayoutCellType.Wall);
				}
			}
		}
	}

	public IEnumerable<LayoutRoom> GetLogicalRoomConnections(LayoutRoom room)
	{
		List<Vector2> connections = null;
		foreach (KeyValuePair<Vector2, List<Vector2>> connection in neighbours.connections)
		{
			foreach (CellRect rect in room.rects)
			{
				if (rect.Contains(new IntVec3(Mathf.RoundToInt(connection.Key.x), 0, Mathf.RoundToInt(connection.Key.y))))
				{
					connections = connection.Value;
					break;
				}
			}
		}
		if (connections == null)
		{
			yield break;
		}
		foreach (LayoutRoom room2 in Rooms)
		{
			if (room2 == room)
			{
				continue;
			}
			bool flag = false;
			foreach (CellRect rect2 in room2.rects)
			{
				foreach (Vector2 item in connections)
				{
					if (rect2.Contains(new IntVec3(Mathf.RoundToInt(item.x), 0, Mathf.RoundToInt(item.y))))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					yield return room2;
					break;
				}
			}
		}
	}

	public bool TryGetRoom(IntVec3 pos, out LayoutRoom room)
	{
		foreach (LayoutRoom room2 in Rooms)
		{
			foreach (CellRect rect in room2.rects)
			{
				if (rect.Contains(pos))
				{
					room = room2;
					return true;
				}
			}
		}
		room = null;
		return false;
	}

	public void ExposeData()
	{
		Scribe_Values.Look(ref container, "container");
		Scribe_Values.Look(ref offset, "offset");
		Scribe_Collections.Look(ref rooms, "rooms", LookMode.Deep);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			OffsetRooms(-offset);
			Init(container);
			FinalizeRooms();
			OffsetRooms(offset);
		}
	}

	private void OffsetRooms(IntVec3 dist)
	{
		foreach (LayoutRoom room in rooms)
		{
			for (int i = 0; i < room.rects.Count; i++)
			{
				room.rects[i] = room.rects[i].MovedBy(dist);
			}
		}
	}
}
