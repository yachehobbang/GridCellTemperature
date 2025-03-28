using Verse;

namespace RimWorld;

public class CompProperties_LetterOnRevealed : CompProperties
{
	public string label;

	public string text;

	public LetterDef letterDef;

	public CompProperties_LetterOnRevealed()
	{
		compClass = typeof(CompLetterOnRevealed);
	}
}
