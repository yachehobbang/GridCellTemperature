using UnityEngine;

namespace Verse;

public class Graphic_MoteWithParentRotation : Graphic_Mote
{
	protected override bool ForcePropertyBlock => true;

	public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
	{
		MoteAttached moteAttached = (MoteAttached)thing;
		Graphic_Mote.propertyBlock.SetColor(ShaderPropertyIDs.Color, color);
		if (moteAttached != null && moteAttached.link1.Linked)
		{
			Graphic_Mote.propertyBlock.SetInt(ShaderPropertyIDs.Rotation, moteAttached.link1.Target.Thing.Rotation.AsInt);
		}
		DrawMoteInternal(loc, rot, thingDef, thing, 0);
	}

	public override string ToString()
	{
		return string.Concat("Graphic_MoteWithParentRotation(path=", path, ", shader=", base.Shader, ", color=", color, ", colorTwo=unsupported)");
	}
}
