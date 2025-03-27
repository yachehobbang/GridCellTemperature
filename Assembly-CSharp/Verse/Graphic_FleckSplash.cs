using RimWorld;
using UnityEngine;

namespace Verse;

public class Graphic_FleckSplash : Graphic_Fleck
{
	public override void DrawFleck(FleckDrawData drawData, DrawBatch batch)
	{
		drawData.propertyBlock = drawData.propertyBlock ?? batch.GetPropertyBlock();
		drawData.propertyBlock.SetColor(ShaderPropertyIDs.ShockwaveColor, new Color(1f, 1f, 1f, drawData.alpha));
		drawData.propertyBlock.SetFloat(ShaderPropertyIDs.ShockwaveSpan, drawData.calculatedShockwaveSpan);
		drawData.drawLayer = SubcameraDefOf.WaterDepth.LayerId;
		base.DrawFleck(drawData, batch);
	}

	public override string ToString()
	{
		return string.Concat("FleckSplash(path=", path, ", shader=", base.Shader, ", color=", color, ", colorTwo=unsupported)");
	}
}
