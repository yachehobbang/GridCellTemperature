using UnityEngine;

namespace Verse;

public class AnimationWorker_RevenantSpasm : AnimationWorker
{
	private IntRange SpasmIntervalShort = new IntRange(6, 18);

	private IntRange SpasmIntervalLong = new IntRange(120, 180);

	private int ShortSpasmLength = 6;

	private IntRange SpasmLength = new IntRange(30, 60);

	private float nextSpasm = -99999f;

	private float spasmStart = -99999f;

	private float spasmLength;

	private float startHeadRot = 90f;

	private Vector3 startHeadOffset = Vector3.zero;

	private float targetHeadRot = 90f;

	private Vector3 targetHeadOffset = Vector3.zero;

	private float AnimationProgress => ((float)Find.TickManager.TicksGame - spasmStart) / spasmLength;

	private float Rotation => Mathf.Lerp(startHeadRot, targetHeadRot, AnimationProgress);

	private Vector3 Offset => new Vector3(Mathf.Lerp(startHeadOffset.x, targetHeadOffset.x, AnimationProgress), 0f, Mathf.Lerp(startHeadOffset.z, targetHeadOffset.z, AnimationProgress));

	public AnimationWorker_RevenantSpasm(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
		: base(def, pawn, part, node)
	{
	}

	public override float AngleAtTick(int tick, PawnDrawParms parms)
	{
		CheckAndDoSpasm();
		if (parms.facing == Rot4.East || parms.facing == Rot4.West)
		{
			return Rotation / 2f;
		}
		return Rotation;
	}

	public override Vector3 OffsetAtTick(int tick, PawnDrawParms parms)
	{
		CheckAndDoSpasm();
		if (parms.facing == Rot4.East || parms.facing == Rot4.West)
		{
			return Offset / 2f;
		}
		return Offset;
	}

	private void CheckAndDoSpasm()
	{
		if ((float)Find.TickManager.TicksGame > nextSpasm)
		{
			startHeadRot = Rotation;
			startHeadOffset = Offset;
			targetHeadRot = Rand.Range(-20, 20);
			targetHeadOffset = new Vector3(Rand.Range(-0.1f, 0.1f), 0f, Rand.Range(-0.05f, 0.05f));
			spasmStart = Find.TickManager.TicksGame;
			spasmLength = (Rand.Bool ? SpasmLength.RandomInRange : ShortSpasmLength);
			nextSpasm = Find.TickManager.TicksGame + (Rand.Bool ? SpasmIntervalShort.RandomInRange : SpasmIntervalLong.RandomInRange);
		}
	}
}
