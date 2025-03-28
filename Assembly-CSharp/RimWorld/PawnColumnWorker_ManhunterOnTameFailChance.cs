using Verse;

namespace RimWorld;

public class PawnColumnWorker_ManhunterOnTameFailChance : PawnColumnWorker_Text
{
	protected override string GetTextFor(Pawn pawn)
	{
		float manhunterOnTameFailChance = pawn.RaceProps.manhunterOnTameFailChance;
		if (manhunterOnTameFailChance == 0f)
		{
			return "-";
		}
		return manhunterOnTameFailChance.ToStringPercent();
	}

	protected override string GetTip(Pawn pawn)
	{
		return "Stat_Race_Animal_TameFailedRevengeChance_Desc".Translate();
	}

	public override int Compare(Pawn a, Pawn b)
	{
		return a.RaceProps.manhunterOnTameFailChance.CompareTo(b.RaceProps.manhunterOnTameFailChance);
	}
}
