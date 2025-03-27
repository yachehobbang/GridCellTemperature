using UnityEngine;

namespace Verse;

[StaticConstructorOnStartup]
public static class MatBases
{
	public static readonly Material LightOverlay = MatLoader.LoadMat("Lighting/LightOverlay");

	public static readonly Material SunShadow = MatLoader.LoadMat("Lighting/SunShadow");

	public static readonly Material SunShadowFade = MatLoader.LoadMat("Lighting/SunShadowFade");

	public static readonly Material EdgeShadow = MatLoader.LoadMat("Lighting/EdgeShadow");

	public static readonly Material IndoorMask = MatLoader.LoadMat("Misc/IndoorMask");

	public static readonly Material ShadowMask = MatLoader.LoadMat("Misc/ShadowMask");

	public static readonly Material RoofedOutdoorMask = MatLoader.LoadMat("Misc/RoofedOutdoorMask");

	public static readonly Material FogOfWar = MatLoader.LoadMat("Misc/FogOfWar");

	public static readonly Material Darkness = MatLoader.LoadMat("Misc/Darkness");

	public static readonly Material Snow = MatLoader.LoadMat("Misc/Snow");

	public static readonly Material DebugOverlay = MatLoader.LoadMat("Misc/DebugOverlay");
}
