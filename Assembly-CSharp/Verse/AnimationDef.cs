using System.Collections.Generic;

namespace Verse;

public class AnimationDef : Def
{
	public int durationTicks;

	public bool startOnRandomTick;

	public bool playWhenDowned;

	public float scale = 1f;

	public Dictionary<PawnRenderNodeTagDef, AnimationPart> animationParts;

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (string item in base.ConfigErrors())
		{
			yield return item;
		}
		foreach (var (pawnRenderNodeTagDef2, animationPart2) in animationParts)
		{
			if (animationPart2.workerClass == null && animationPart2.keyframes.NullOrEmpty())
			{
				yield return "Animation part for " + pawnRenderNodeTagDef2.defName + " has no keyframes.";
			}
		}
	}
}
