using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class PawnShamblerScarDrawer : PawnOverlayDrawer
{
	private const string ScarTexturePath = "Things/Pawn/Overlays/ShamblerScars/ShamblerScarOverlay";

	public PawnShamblerScarDrawer(Pawn pawn)
		: base(pawn)
	{
	}

	protected override void WriteCache(CacheKey key, PawnDrawParms parms, List<DrawCall> writeTarget)
	{
		Rot4 pawnRot = key.pawnRot;
		Mesh bodyMesh = key.bodyMesh;
		OverlayLayer layer = key.layer;
		Graphic graphic = ((layer == OverlayLayer.Body) ? pawn.Drawer.renderer.BodyGraphic : pawn.Drawer.renderer.HeadGraphic);
		if (graphic == null)
		{
			return;
		}
		Rand.PushState(pawn.thingIDNumber * (int)(layer + 1));
		try
		{
			Mesh mesh = (((graphic.EastFlipped && pawnRot == Rot4.East) || (graphic.WestFlipped && pawnRot == Rot4.West)) ? MeshPool.GridPlaneFlip(Vector2.one) : MeshPool.GridPlane(Vector2.one));
			Vector3 size = bodyMesh.bounds.size;
			float magnitude = size.magnitude;
			Vector3 vector = mesh.bounds.size * magnitude;
			Vector4 value = new Vector4(vector.x / size.x, vector.z / size.z);
			Material material = MaterialPool.MatFrom("Things/Pawn/Overlays/ShamblerScars/ShamblerScarOverlay", ShaderDatabase.Wound, Color.white);
			MaterialRequest req = default(MaterialRequest);
			req.maskTex = (Texture2D)graphic.MatAt(pawnRot).mainTexture;
			req.mainTex = material.mainTexture;
			req.color = material.color;
			req.shader = material.shader;
			material = MaterialPool.MatFrom(req);
			Vector3 vector2 = Rand.InsideUnitCircleVec3 / 2f;
			int rotation = Rand.Range(0, 360);
			writeTarget.Add(new DrawCall
			{
				overlayMat = material,
				matrix = Matrix4x4.Scale(size),
				overlayMesh = mesh,
				mainTexScale = value,
				mainTexOffset = new Vector4(vector2.x, vector2.z),
				rotation = rotation
			});
		}
		finally
		{
			Rand.PopState();
		}
	}
}
