using UnityEngine;

namespace Verse;

public class Graphic_MoteWithAgeSecs : Graphic_Mote
{
	protected override bool ForcePropertyBlock => true;

	public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
	{
		Mote mote = (Mote)thing;
		Graphic_Mote.propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
		Graphic_Mote.propertyBlock.SetFloat(ShaderPropertyIDs.AgeSecs, mote.AgeSecs);
		Graphic_Mote.propertyBlock.SetFloat(ShaderPropertyIDs.AgeSecsPausable, mote.AgeSecsPausable);
		Graphic_Mote.propertyBlock.SetFloat(ShaderPropertyIDs.RandomPerObject, Gen.HashCombineInt(mote.spawnTick, mote.DrawPos.GetHashCode()));
		Graphic_Mote.propertyBlock.SetFloat(ShaderPropertyIDs.RandomPerObjectOffsetRandom, Gen.HashCombineInt(mote.spawnTick, mote.offsetRandom));
		DrawMoteInternal(loc, rot, thingDef, thing, 0);
	}

	public override string ToString()
	{
		return string.Concat("Graphic_MoteWithAgeSecs(path=", path, ", shader=", base.Shader, ", color=", color, ", colorTwo=unsupported)");
	}
}
