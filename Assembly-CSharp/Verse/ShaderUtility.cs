using UnityEngine;

namespace Verse;

public static class ShaderUtility
{
	public static bool SupportsMaskTex(this Shader shader)
	{
		if (!(shader == ShaderDatabase.CutoutComplex) && !(shader == ShaderDatabase.CutoutSkinOverlay) && !(shader == ShaderDatabase.Wound) && !(shader == ShaderDatabase.FirefoamOverlay) && !(shader == ShaderDatabase.CutoutWithOverlay) && !(shader == ShaderDatabase.CutoutComplexBlend))
		{
			return shader == ShaderDatabase.BioferriteHarvester;
		}
		return true;
	}

	public static Shader GetSkinShader(Pawn pawn)
	{
		foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
		{
			if (hediff.def.skinShader != null)
			{
				return hediff.def.skinShader.Shader;
			}
		}
		bool dead = pawn.Dead || (pawn.IsMutant && pawn.mutant.HasTurned);
		return GetSkinShaderAbstract(pawn.story != null && pawn.story.SkinColorOverriden, dead);
	}

	public static Shader GetSkinShaderAbstract(bool skinColorOverriden, bool dead)
	{
		if (skinColorOverriden)
		{
			return ShaderDatabase.CutoutSkinColorOverride;
		}
		if (dead)
		{
			return ShaderDatabase.Cutout;
		}
		return ShaderDatabase.CutoutSkin;
	}
}
