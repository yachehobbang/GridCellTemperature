using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public class Graphic_MoteRandom : Graphic_Random
{
	protected static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

	protected virtual bool ForcePropertyBlock => false;

	public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
	{
		Graphic_Mote.DrawMote(data, SubGraphicFor((Mote)thing).MatSingle, base.Color, loc, rot, thingDef, thing, 0, ForcePropertyBlock);
	}

	public Graphic SubGraphicFor(Mote mote)
	{
		return subGraphics[mote.offsetRandom % subGraphics.Length];
	}

	public override string ToString()
	{
		return string.Concat("Mote(path=", path, ", shader=", base.Shader, ", color=", color, ", colorTwo=unsupported)");
	}
}
