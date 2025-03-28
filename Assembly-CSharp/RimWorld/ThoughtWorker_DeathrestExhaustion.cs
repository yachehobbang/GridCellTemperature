using Verse;

namespace RimWorld;

public class ThoughtWorker_DeathrestExhaustion : ThoughtWorker
{
	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		if (!ModsConfig.BiotechActive)
		{
			return ThoughtState.Inactive;
		}
		Need_Deathrest need_Deathrest = p.needs.TryGetNeed<Need_Deathrest>();
		if (need_Deathrest == null)
		{
			return ThoughtState.Inactive;
		}
		return need_Deathrest.CurLevel == 0f;
	}
}
