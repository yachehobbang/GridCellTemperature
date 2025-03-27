using Verse;

namespace RimWorld;

public class CompTargetable_AllAnimalsOnTheMap : CompTargetable_AllPawnsOnTheMap
{
	public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
	{
		if (target.Thing is Pawn { IsNonMutantAnimal: not false })
		{
			return base.ValidateTarget(target.Thing, showMessages);
		}
		return false;
	}
}
