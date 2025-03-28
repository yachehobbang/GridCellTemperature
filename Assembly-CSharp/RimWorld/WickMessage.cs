using Verse;

namespace RimWorld;

public struct WickMessage
{
	[NoTranslate]
	public string wickMessagekey;

	public MessageTypeDef messageType;

	public int ticksLeft;
}
