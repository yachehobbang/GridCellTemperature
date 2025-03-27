using Verse;

namespace RimWorld;

[DefOf]
public static class SongDefOf
{
	public static SongDef EntrySong;

	public static SongDef EndCreditsSong;

	[MayRequireIdeology]
	public static SongDef ArchonexusVictorySong;

	[MayRequireAnomaly]
	public static SongDef AnomalyCreditsSong;

	static SongDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(SongDefOf));
	}
}
