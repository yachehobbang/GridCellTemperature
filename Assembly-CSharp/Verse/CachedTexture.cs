using UnityEngine;

namespace Verse;

public class CachedTexture
{
	private string texPath;

	private Texture2D cachedTexture;

	private int validationIndex;

	private static int curValidationIndex;

	public Texture2D Texture
	{
		get
		{
			if (cachedTexture == null || validationIndex != curValidationIndex)
			{
				if (texPath.NullOrEmpty())
				{
					cachedTexture = BaseContent.BadTex;
				}
				else
				{
					cachedTexture = ContentFinder<Texture2D>.Get(texPath) ?? BaseContent.BadTex;
				}
				validationIndex = curValidationIndex;
			}
			return cachedTexture;
		}
	}

	public CachedTexture(string texPath)
	{
		this.texPath = texPath;
		cachedTexture = null;
		validationIndex = -1;
	}

	public static void ResetStaticData()
	{
		curValidationIndex++;
	}
}
