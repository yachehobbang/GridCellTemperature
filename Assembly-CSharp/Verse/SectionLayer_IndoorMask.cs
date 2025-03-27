using RimWorld;
using UnityEngine;

namespace Verse;

public class SectionLayer_IndoorMask : SectionLayer
{
	public override bool Visible => DebugViewSettings.drawShadows;

	public SectionLayer_IndoorMask(Section section)
		: base(section)
	{
		relevantChangeTypes = (ulong)MapMeshFlagDefOf.Buildings | (ulong)MapMeshFlagDefOf.Roofs | (ulong)MapMeshFlagDefOf.FogOfWar;
	}

	private bool HideRainFogOverlay(IntVec3 c)
	{
		Building edifice = c.GetEdifice(base.Map);
		if (edifice?.def.building != null && edifice.def.building.isNaturalRock)
		{
			return true;
		}
		return false;
	}

	private bool HideCommon(IntVec3 c)
	{
		if (base.Map.fogGrid.IsFogged(c))
		{
			return true;
		}
		if (c.Roofed(base.Map))
		{
			Building edifice = c.GetEdifice(base.Map);
			if (edifice == null)
			{
				return true;
			}
			if (edifice.def.Fillage != FillCategory.Full)
			{
				return true;
			}
			if (edifice.def.size.x > 1 || edifice.def.size.z > 1)
			{
				return true;
			}
			if (edifice.def.holdsRoof)
			{
				return true;
			}
			if (edifice.def.blockWeather)
			{
				return true;
			}
		}
		return false;
	}

	public override void Regenerate()
	{
		bool drawIndoorMask = DebugViewSettings.drawIndoorMask;
		bool drawOutRoofedMask = DebugViewSettings.drawOutRoofedMask;
		CellRect rect = new CellRect(section.botLeft.x, section.botLeft.z, 17, 17);
		rect.ClipInsideMap(base.Map);
		LayerSubMesh subMesh = GetSubMesh(MatBases.IndoorMask);
		LayerSubMesh subMesh2 = GetSubMesh(MatBases.RoofedOutdoorMask);
		LayerSubMesh subMesh3 = GetSubMesh(MatBases.DebugOverlay);
		ClearSubMesh(subMesh, rect);
		ClearSubMesh(subMesh2, rect);
		ClearSubMesh(subMesh3, rect);
		CellIndices cellIndices = base.Map.cellIndices;
		for (int i = rect.minX; i <= rect.maxX; i++)
		{
			for (int j = rect.minZ; j <= rect.maxZ; j++)
			{
				IntVec3 intVec = new IntVec3(i, 0, j);
				bool flag = HideCommon(intVec);
				bool flag2 = HideRainFogOverlay(intVec);
				if (!flag && !flag2)
				{
					continue;
				}
				Building building = base.Map.edificeGrid.InnerArray[cellIndices.CellToIndex(i, j)];
				float overage = ((building == null || (building.def.passability != Traversability.Impassable && !building.def.IsDoor)) ? 0.16f : 0f);
				if (flag && !flag2)
				{
					Room room = intVec.GetRoom(base.Map);
					if (room == null || !room.ProperRoom)
					{
						Building edifice = intVec.GetEdifice(base.Map);
						RoofDef roof = intVec.GetRoof(base.Map);
						if ((edifice == null || edifice.def.Fillage != FillCategory.Full) && (roof == null || !roof.isThickRoof))
						{
							AppendQuadsToMesh(subMesh2, i, j, overage);
							if (!drawIndoorMask && drawOutRoofedMask)
							{
								AppendQuadsToMesh(subMesh3, i, j, overage);
							}
						}
					}
				}
				AppendQuadsToMesh(subMesh, i, j, overage);
				if (drawIndoorMask)
				{
					AppendQuadsToMesh(subMesh3, i, j, overage);
				}
			}
		}
		if (subMesh.verts.Count > 0)
		{
			subMesh.FinalizeMesh(MeshParts.Verts | MeshParts.Tris);
		}
		if (subMesh2.verts.Count > 0)
		{
			subMesh2.FinalizeMesh(MeshParts.Verts | MeshParts.Tris);
		}
		if (subMesh3.verts.Count > 0)
		{
			subMesh3.FinalizeMesh(MeshParts.Verts | MeshParts.Tris);
		}
	}

	private static void ClearSubMesh(LayerSubMesh mesh, CellRect rect)
	{
		mesh.Clear(MeshParts.All);
		if (DebugViewSettings.drawIndoorMask || DebugViewSettings.drawOutRoofedMask)
		{
			mesh.verts.Capacity = rect.Area * 2;
			mesh.tris.Capacity = rect.Area * 4;
		}
	}

	private static void AppendQuadsToMesh(LayerSubMesh mesh, int x, int z, float overage)
	{
		float y = AltitudeLayer.MetaOverlays.AltitudeFor();
		mesh.verts.Add(new Vector3((float)x - overage, y, (float)z - overage));
		mesh.verts.Add(new Vector3((float)x - overage, y, (float)(z + 1) + overage));
		mesh.verts.Add(new Vector3((float)(x + 1) + overage, y, (float)(z + 1) + overage));
		mesh.verts.Add(new Vector3((float)(x + 1) + overage, y, (float)z - overage));
		int count = mesh.verts.Count;
		mesh.tris.Add(count - 4);
		mesh.tris.Add(count - 3);
		mesh.tris.Add(count - 2);
		mesh.tris.Add(count - 4);
		mesh.tris.Add(count - 2);
		mesh.tris.Add(count - 1);
	}
}
