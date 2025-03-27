using UnityEngine;

namespace Verse;

public class AnimationWorker_Keyframes : AnimationWorker
{
	public AnimationWorker_Keyframes(AnimationDef def, Pawn pawn, AnimationPart part, PawnRenderNode node)
		: base(def, pawn, part, node)
	{
	}

	public override Vector3 OffsetAtTick(int tick, PawnDrawParms parms)
	{
		if (tick <= part.keyframes[0].tick)
		{
			return part.keyframes[0].offset;
		}
		if (tick >= part.keyframes[part.keyframes.Count - 1].tick)
		{
			return part.keyframes[part.keyframes.Count - 1].offset;
		}
		Keyframe keyframe = part.keyframes[0];
		Keyframe keyframe2 = part.keyframes[part.keyframes.Count - 1];
		for (int i = 0; i < part.keyframes.Count; i++)
		{
			if (tick <= part.keyframes[i].tick)
			{
				keyframe2 = part.keyframes[i];
				if (i > 0)
				{
					keyframe = part.keyframes[i - 1];
				}
				break;
			}
		}
		float t = (float)(tick - keyframe.tick) / (float)(keyframe2.tick - keyframe.tick);
		return def.scale * Vector3.Lerp(keyframe.offset, keyframe2.offset, t);
	}

	public override float AngleAtTick(int tick, PawnDrawParms parms)
	{
		if (tick <= part.keyframes[0].tick)
		{
			return part.keyframes[0].angle;
		}
		if (tick >= part.keyframes[part.keyframes.Count - 1].tick)
		{
			return part.keyframes[part.keyframes.Count - 1].angle;
		}
		Keyframe keyframe = part.keyframes[0];
		Keyframe keyframe2 = part.keyframes[part.keyframes.Count - 1];
		for (int i = 0; i < part.keyframes.Count; i++)
		{
			if (tick <= part.keyframes[i].tick)
			{
				keyframe2 = part.keyframes[i];
				if (i > 0)
				{
					keyframe = part.keyframes[i - 1];
				}
				break;
			}
		}
		float t = (float)(tick - keyframe.tick) / (float)(keyframe2.tick - keyframe.tick);
		return def.scale * Mathf.Lerp(keyframe.angle, keyframe2.angle, t);
	}

	public override Vector3 ScaleAtTick(int tick, PawnDrawParms parms)
	{
		if (tick <= part.keyframes[0].tick)
		{
			return part.keyframes[0].scale;
		}
		if (tick >= part.keyframes[part.keyframes.Count - 1].tick)
		{
			return part.keyframes[part.keyframes.Count - 1].scale;
		}
		Keyframe keyframe = part.keyframes[0];
		Keyframe keyframe2 = part.keyframes[part.keyframes.Count - 1];
		for (int i = 0; i < part.keyframes.Count; i++)
		{
			if (tick <= part.keyframes[i].tick)
			{
				keyframe2 = part.keyframes[i];
				if (i > 0)
				{
					keyframe = part.keyframes[i - 1];
				}
				break;
			}
		}
		float t = (float)(tick - keyframe.tick) / (float)(keyframe2.tick - keyframe.tick);
		return Vector3.Lerp(keyframe.scale, keyframe2.scale, t);
	}
}
