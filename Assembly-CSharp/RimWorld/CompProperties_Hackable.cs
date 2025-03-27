using Verse;

namespace RimWorld;

public class CompProperties_Hackable : CompProperties
{
	public float defence;

	public EffecterDef effectHacking;

	public QuestScriptDef completedQuest;

	public bool glowIfHacked;

	public SoundDef hackingCompletedSound;

	public CompProperties_Hackable()
	{
		compClass = typeof(CompHackable);
	}
}
