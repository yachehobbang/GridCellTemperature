using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace Verse;

public class SectionLayer_TerrainScatter : SectionLayer
{
	private class Scatterable
	{
		private Map map;

		private ScatterableDef def;

		private Vector3 loc;

		private float size;

		private float rotation;

		public bool IsOnValidTerrain
		{
			get
			{
				IntVec3 intVec = loc.ToIntVec3();
				if (def.scatterType != map.terrainGrid.TerrainAt(intVec).scatterType || intVec.Filled(map))
				{
					return false;
				}
				foreach (IntVec3 item in CellRect.CenteredOn(intVec, Mathf.FloorToInt(size)).ClipInsideMap(map))
				{
					if (map.terrainGrid.TerrainAt(item).IsFloor)
					{
						return false;
					}
				}
				return true;
			}
		}

		public Scatterable(ScatterableDef def, Vector3 loc, Map map)
		{
			this.def = def;
			this.loc = loc;
			this.map = map;
			size = Rand.Range(def.minSize, def.maxSize);
			rotation = Rand.Range(0f, 360f);
		}

		public void PrintOnto(SectionLayer layer)
		{
			Material material = def.mat;
			Graphic.TryGetTextureAtlasReplacementInfo(material, TextureAtlasGroup.Terrain, flipUv: false, vertexColors: false, out material, out var uvs, out var _);
			Printer_Plane.PrintPlane(layer, loc, Vector2.one * size, material, rotation, flipUv: false, uvs);
		}
	}

	private List<Scatterable> scats = new List<Scatterable>();

	public override bool Visible => DebugViewSettings.drawTerrain;

	public SectionLayer_TerrainScatter(Section section)
		: base(section)
	{
		relevantChangeTypes = MapMeshFlagDefOf.Terrain;
	}

	public override void Regenerate()
	{
		ClearSubMeshes(MeshParts.All);
		scats.RemoveAll((Scatterable scat) => !scat.IsOnValidTerrain);
		int num = 0;
		TerrainDef[] topGrid = base.Map.terrainGrid.topGrid;
		CellRect cellRect = section.CellRect;
		CellIndices cellIndices = base.Map.cellIndices;
		for (int i = cellRect.minZ; i <= cellRect.maxZ; i++)
		{
			for (int j = cellRect.minX; j <= cellRect.maxX; j++)
			{
				if (topGrid[cellIndices.CellToIndex(j, i)].scatterType != null)
				{
					num++;
				}
			}
		}
		num /= 40;
		int num2 = 0;
		while (scats.Count < num && num2 < 200)
		{
			num2++;
			IntVec3 randomCell = section.CellRect.RandomCell;
			string terrScatType = base.Map.terrainGrid.TerrainAt(randomCell).scatterType;
			if (terrScatType != null && !randomCell.Filled(base.Map) && DefDatabase<ScatterableDef>.AllDefs.Where((ScatterableDef def) => def.scatterType == terrScatType).TryRandomElement(out var result))
			{
				Scatterable scatterable = new Scatterable(loc: new Vector3((float)randomCell.x + Rand.Value, randomCell.y, (float)randomCell.z + Rand.Value), def: result, map: base.Map);
				scats.Add(scatterable);
				scatterable.PrintOnto(this);
			}
		}
		for (int k = 0; k < scats.Count; k++)
		{
			scats[k].PrintOnto(this);
		}
		FinalizeMesh(MeshParts.All);
	}
}
