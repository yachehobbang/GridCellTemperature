using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class SitePartWorker_WorkSite_Logging : SitePartWorker_WorkSite
{
	public override IEnumerable<PreceptDef> DisallowedPrecepts => DefDatabase<PreceptDef>.AllDefs.Where((PreceptDef p) => p.disallowLoggingCamps);

	public override PawnGroupKindDef WorkerGroupKind => PawnGroupKindDefOf.Loggers;

	public override bool CanSpawnOn(int tile)
	{
		return Find.WorldGrid[tile].biome.TreeDensity >= BiomeDefOf.Tundra.TreeDensity;
	}
}
