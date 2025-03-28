using UnityEngine;

namespace Verse;

public static class Altitudes
{
	private const int NumAltitudeLayers = 38;

	private static readonly float[] Alts;

	private const float LayerSpacing = 0.3846154f;

	public const float AltInc = 1f / 26f;

	public static readonly Vector3 AltIncVect;

	static Altitudes()
	{
		Alts = new float[38];
		AltIncVect = new Vector3(0f, 1f / 26f, 0f);
		for (int i = 0; i < 38; i++)
		{
			Alts[i] = (float)i * 0.3846154f;
		}
	}

	public static float AltitudeFor(this AltitudeLayer alt)
	{
		return Alts[(uint)alt];
	}

	public static float AltitudeFor(this AltitudeLayer alt, float incOffset)
	{
		return alt.AltitudeFor() + incOffset * (1f / 26f);
	}
}
