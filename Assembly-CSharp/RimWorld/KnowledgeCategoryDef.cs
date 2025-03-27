using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public class KnowledgeCategoryDef : Def
{
	public KnowledgeCategoryDef overflowCategory;

	public Color color;

	[NoTranslate]
	public string texPath;

	public override IEnumerable<string> ConfigErrors()
	{
		if (overflowCategory == this)
		{
			yield return "overflowCategory is this category.";
		}
	}
}
