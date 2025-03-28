using UnityEngine;

namespace Verse;

public class AnimationWorker
{
	public AnimationDef def;

	public Pawn pawn;

	public AnimationPart part;

	public PawnRenderNode node;

	public AnimationWorker(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
	{
		this.def = def;
		this.pawn = pawn;
		this.part = part;
		this.node = node;
	}

	public virtual bool Enabled()
	{
		if (!def.playWhenDowned && pawn.Downed)
		{
			return false;
		}
		return true;
	}

	public virtual void Draw(PawnDrawParms parms, Matrix4x4 matrix)
	{
	}

	public virtual Vector3 OffsetAtTick(int tick, PawnDrawParms parms)
	{
		return Vector3.zero;
	}

	public virtual float AngleAtTick(int tick, PawnDrawParms parms)
	{
		return 0f;
	}

	public virtual Vector3 ScaleAtTick(int tick, PawnDrawParms parms)
	{
		return Vector3.one;
	}
}
