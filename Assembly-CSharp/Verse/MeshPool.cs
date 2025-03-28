using System.Collections.Generic;
using System.Text;
using LudeonTK;
using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public static class MeshPool
{
	public const float HumanlikeBodyWidth = 1.5f;

	public const float HumanlikeHeadAverageWidth = 1.5f;

	private static readonly Dictionary<Vector2, Mesh> planes;

	private static readonly Dictionary<Vector2, Mesh> planesFlip;

	private static readonly Dictionary<Mesh, Vector2> meshSizes;

	private static readonly Dictionary<Vector2, GraphicMeshSet> humanlikeMeshSet_Custom;

	public static readonly Mesh plane025;

	public static readonly Mesh plane03;

	public static readonly Mesh plane05;

	public static readonly Mesh plane08;

	public static readonly Mesh plane10;

	public static readonly Mesh plane10Back;

	public static readonly Mesh plane10Flip;

	public static readonly Mesh plane14;

	public static readonly Mesh plane20;

	public static readonly Mesh circle;

	public static readonly Mesh[] pies;

	public static readonly Mesh wholeMapPlane;

	static MeshPool()
	{
		planes = new Dictionary<Vector2, Mesh>(FastVector2Comparer.Instance);
		planesFlip = new Dictionary<Vector2, Mesh>(FastVector2Comparer.Instance);
		meshSizes = new Dictionary<Mesh, Vector2>();
		humanlikeMeshSet_Custom = new Dictionary<Vector2, GraphicMeshSet>();
		plane025 = MeshMakerPlanes.NewPlaneMesh(0.25f);
		plane03 = MeshMakerPlanes.NewPlaneMesh(0.3f);
		plane05 = MeshMakerPlanes.NewPlaneMesh(0.5f);
		plane08 = MeshMakerPlanes.NewPlaneMesh(0.8f);
		plane10 = MeshMakerPlanes.NewPlaneMesh(1f);
		plane10Back = MeshMakerPlanes.NewPlaneMesh(1f, flipped: false, backLift: true);
		plane10Flip = MeshMakerPlanes.NewPlaneMesh(1f, flipped: true);
		plane14 = MeshMakerPlanes.NewPlaneMesh(1.4f);
		plane20 = MeshMakerPlanes.NewPlaneMesh(2f);
		circle = MeshMakerCircles.MakeCircleMesh(1f);
		pies = new Mesh[361];
		for (int i = 0; i < 361; i++)
		{
			pies[i] = MeshMakerCircles.MakePieMesh(i);
		}
		wholeMapPlane = MeshMakerPlanes.NewWholeMapPlane();
	}

	public static Mesh GridPlane(Vector2 size)
	{
		if (!planes.TryGetValue(size, out var value))
		{
			value = MeshMakerPlanes.NewPlaneMesh(size, flipped: false, backLift: false, twist: false);
			planes.Add(size, value);
		}
		return value;
	}

	public static Mesh GridPlaneFlip(Vector2 size)
	{
		if (!planesFlip.TryGetValue(size, out var value))
		{
			value = MeshMakerPlanes.NewPlaneMesh(size, flipped: true, backLift: false, twist: false);
			planesFlip.Add(size, value);
		}
		return value;
	}

	public static Mesh GridPlane(Vector2 size, bool flip)
	{
		if (!flip)
		{
			return GridPlane(size);
		}
		return GridPlaneFlip(size);
	}

	public static Vector2 SizeOf(Mesh mesh)
	{
		if (meshSizes.TryGetValue(mesh, out var value))
		{
			return value;
		}
		return Vector2.zero;
	}

	public static void EnsureSizeCached(Mesh mesh, Vector2 size)
	{
		if (!meshSizes.ContainsKey(mesh))
		{
			meshSizes[mesh] = size;
		}
	}

	[DebugOutput("System", false)]
	public static void MeshPoolStats()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("MeshPool stats:");
		stringBuilder.AppendLine("Planes: " + planes.Count);
		stringBuilder.AppendLine("PlanesFlip: " + planesFlip.Count);
		Log.Message(stringBuilder.ToString());
	}

	public static GraphicMeshSet GetMeshSetForWidth(float width)
	{
		return GetMeshSetForSize(width, width);
	}

	public static GraphicMeshSet GetMeshSetForSize(Vector2 size)
	{
		if (!humanlikeMeshSet_Custom.ContainsKey(size))
		{
			humanlikeMeshSet_Custom[size] = new GraphicMeshSet(size.x, size.y);
		}
		return humanlikeMeshSet_Custom[size];
	}

	public static GraphicMeshSet GetMeshSetForSize(float width, float height)
	{
		return GetMeshSetForSize(new Vector2(width, height));
	}
}
