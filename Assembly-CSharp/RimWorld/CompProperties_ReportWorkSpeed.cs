using Verse;

namespace RimWorld;

public class CompProperties_ReportWorkSpeed : CompProperties
{
	public StatDef workSpeedStat;

	public bool displayReasons = true;

	public CompProperties_ReportWorkSpeed()
	{
		compClass = typeof(CompReportWorkSpeed);
	}
}
