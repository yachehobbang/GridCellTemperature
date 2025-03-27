using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace GridCellTemperature.Core
{
	public class TemperatureCellBoolDrawer : CellBoolDrawer
	{
		private bool _isDirtyTemperatureValues = false;

		private FieldInfo _meshesField = AccessTools.Field(typeof(CellBoolDrawer), "meshes");
		private FieldInfo _mapSizeXField = AccessTools.Field(typeof(CellBoolDrawer), "mapSizeX");
		private FieldInfo _mapSizeZField = AccessTools.Field(typeof(CellBoolDrawer), "mapSizeZ");
		private FieldInfo _cellBoolGetterField = AccessTools.Field(typeof(CellBoolDrawer), "cellBoolGetter");
		private FieldInfo _extraColorGetterField = AccessTools.Field(typeof(CellBoolDrawer), "extraColorGetter");
		private FieldInfo _vertsField = AccessTools.Field(typeof(CellBoolDrawer), "verts");
		private FieldInfo _trisField = AccessTools.Field(typeof(CellBoolDrawer), "tris");
		private FieldInfo _colorsField = AccessTools.Field(typeof(CellBoolDrawer), "colors");
		private FieldInfo _dirtyField = AccessTools.Field(typeof(CellBoolDrawer), "dirty");

		private MethodInfo _FinalizeWorkingDataIntoMeshMethod = AccessTools.Method(typeof(CellBoolDrawer), "FinalizeWorkingDataIntoMesh");
		private MethodInfo _CreateMaterialIfNeededMeshMethod = AccessTools.Method(typeof(CellBoolDrawer), "CreateMaterialIfNeeded");

		private (int meshIndex, int colorIndex)[] _indexToColorIndex;

		public TemperatureCellBoolDrawer(ICellBoolGiver giver, int mapSizeX, int mapSizeZ, float opacity = 0.33F) : base(giver, mapSizeX, mapSizeZ, opacity)
		{
		}

		public TemperatureCellBoolDrawer(ICellBoolGiver giver, int mapSizeX, int mapSizeZ, int renderQueue, float opacity = 0.33F) : base(giver, mapSizeX, mapSizeZ, renderQueue, opacity)
		{
		}

		public TemperatureCellBoolDrawer(Func<int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, Color> extraColorGetter, int mapSizeX, int mapSizeZ, float opacity = 0.33F) : base(cellBoolGetter, colorGetter, extraColorGetter, mapSizeX, mapSizeZ, opacity)
		{
		}

		public TemperatureCellBoolDrawer(Func<int, bool> cellBoolGetter, Func<Color> colorGetter, Func<int, Color> extraColorGetter, int mapSizeX, int mapSizeZ, int renderQueue, float opacity = 0.33F) : base(cellBoolGetter, colorGetter, extraColorGetter, mapSizeX, mapSizeZ, renderQueue, opacity)
		{
		}

		public void MarkTemperatureAsDirty()
		{
			_isDirtyTemperatureValues = true;
		}

		public void TemperatureCellBoolDrawer_CellBoolDrawerUpdate()
		{
			if (_isDirtyTemperatureValues)
			{
				_isDirtyTemperatureValues = false;
				if ((bool)_dirtyField.GetValue(this) == false)
				{
					var meshes = (List<Mesh>)_meshesField.GetValue(this);
					var colors = new Color[meshes.Count][];
					for (var i = 0; i < meshes.Count; i++)
					{
						colors[i] = meshes[i].colors;
					}
					var extraColorGetter = (Func<int, Color>)_extraColorGetterField.GetValue(this);
					for (var i = 0; i < _indexToColorIndex.Length; i++)
					{
						var (meshIndex, colorIndex) = _indexToColorIndex[i];
						if (meshIndex < 0)
						{
							continue;
						}

						Color color = extraColorGetter(i);

						var list = colors[meshIndex];
						for (var k = 0; k < 4; k++)
						{
							list[colorIndex + k] = color;
						}
					}

					for (var i = 0; i < meshes.Count; i++)
					{
						meshes[i].SetColors(colors[i]);
					}
				}
			}
		}

		public void TemperatureCellBoolDrawer_RegenerateMesh()
		{
			var mapSizeX = (int)_mapSizeXField.GetValue(this);
			var mapSizeZ = (int)_mapSizeZField.GetValue(this);

			if (_indexToColorIndex == null)
			{
				_indexToColorIndex = new (int meshIndex, int colorIndex)[mapSizeX * mapSizeZ];
			}

			var meshes = (List<Mesh>)_meshesField.GetValue(this);

			for (int i = 0; i < meshes.Count; i++)
			{
				meshes[i].Clear();
			}

			int num = 0;
			int num2 = 0;
			if (meshes.Count < num + 1)
			{
				Mesh mesh = new Mesh();
				mesh.name = "CellBoolDrawer";
				meshes.Add(mesh);
			}

			var cellBoolGetter = (Func<int, bool>)_cellBoolGetterField.GetValue(this);
			var extraColorGetter = (Func<int, Color>)_extraColorGetterField.GetValue(this);
			var verts = (List<Vector3>)_vertsField.GetValue(this);
			var colors = (List<Color>)_colorsField.GetValue(this);
			var tris = (List<int>)_trisField.GetValue(this);

			Mesh mesh2 = meshes[num];
			CellRect cellRect = new CellRect(0, 0, mapSizeX, mapSizeZ);
			float y = AltitudeLayer.MapDataOverlay.AltitudeFor();
			bool careAboutVertexColors = false;
			for (int j = cellRect.minX; j <= cellRect.maxX; j++)
			{
				for (int k = cellRect.minZ; k <= cellRect.maxZ; k++)
				{
					int arg = CellIndicesUtility.CellToIndex(j, k, mapSizeX);
					if (!cellBoolGetter(arg))
					{
						_indexToColorIndex[arg] = (-1, -1);
						continue;
					}

					verts.Add(new Vector3(j, y, k));
					verts.Add(new Vector3(j, y, k + 1));
					verts.Add(new Vector3(j + 1, y, k + 1));
					verts.Add(new Vector3(j + 1, y, k));

					_indexToColorIndex[arg] = (num, colors.Count);

					Color color = extraColorGetter(arg);
					colors.Add(color);
					colors.Add(color);
					colors.Add(color);
					colors.Add(color);
					if (color != Color.white)
					{
						careAboutVertexColors = true;
					}

					int count = verts.Count;
					tris.Add(count - 4);
					tris.Add(count - 3);
					tris.Add(count - 2);
					tris.Add(count - 4);
					tris.Add(count - 2);
					tris.Add(count - 1);
					num2++;
					if (num2 >= 16383)
					{
						_FinalizeWorkingDataIntoMeshMethod.Invoke(this, new object[] { mesh2 });
						num++;
						if (meshes.Count < num + 1)
						{
							Mesh mesh3 = new Mesh();
							mesh3.name = "CellBoolDrawer";
							meshes.Add(mesh3);
						}

						mesh2 = meshes[num];
						num2 = 0;
					}
				}
			}

			_FinalizeWorkingDataIntoMeshMethod.Invoke(this, new object[] { mesh2 });
			_CreateMaterialIfNeededMeshMethod.Invoke(this, new object[] { careAboutVertexColors });

			_dirtyField.SetValue(this, false);
		}
	}
}
