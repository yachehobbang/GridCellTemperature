using Verse;

namespace RimWorld;

public class PreceptComp_Apparel_DesiredStrong : PreceptComp_Apparel
{
	public override void Notify_MemberGenerated(Pawn pawn, Precept precept, bool newborn, bool ignoreApparel = false)
	{
		if (!(newborn || !AppliesToPawn(pawn, precept) || ignoreApparel))
		{
			GiveApparelToPawn(pawn, (Precept_Apparel)precept);
		}
	}
}
