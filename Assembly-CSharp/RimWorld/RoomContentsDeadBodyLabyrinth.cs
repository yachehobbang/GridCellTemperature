using Verse;

namespace RimWorld;

public class RoomContentsDeadBodyLabyrinth : RoomContentsDeadBody
{
	protected override ThingDef KillerThing => ThingDefOf.Fingerspike;

	protected override DamageDef DamageType => DamageDefOf.Scratch;

	protected override Tool ToolUsed => ThingDefOf.Fingerspike.tools[0];
}
