using Verse;

namespace RimWorld;

public class ThoughtWorker_KillThirst : ThoughtWorker
{
	public const float MinLevelForThought = 0.3f;

	protected override ThoughtState CurrentStateInternal(Pawn p)
	{
		if (!ModsConfig.BiotechActive)
		{
			return ThoughtState.Inactive;
		}
		Need_KillThirst need_KillThirst = p.needs?.TryGetNeed<Need_KillThirst>();
		return need_KillThirst != null && need_KillThirst.CurLevel <= 0.3f;
	}
}
