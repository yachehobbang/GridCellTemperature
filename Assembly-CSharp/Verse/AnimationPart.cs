using System;
using System.Collections.Generic;
using UnityEngine;

namespace Verse;

public class AnimationPart
{
	public List<Keyframe> keyframes;

	public Vector2 pivot = new Vector2(0.5f, 0.5f);

	public Type workerClass;
}
