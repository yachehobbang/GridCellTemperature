using System.Collections.Generic;
using LudeonTK;
using Verse;
using Verse.Grammar;

namespace RimWorld;

public static class GrowthUtility
{
	public const int GrowthTiers = 9;

	public static readonly float[] GrowthTierPointsRequirements = new float[9] { 0f, 30f, 55f, 80f, 100f, 120f, 135f, 150f, 162f };

	public static readonly int[] PassionGainsPerTier = new int[9] { 0, 0, 0, 0, 1, 1, 1, 2, 3 };

	public static readonly int[] PassionChoicesPerTier = new int[9] { 0, 0, 0, 0, 1, 2, 3, 4, 6 };

	public static readonly int[] TraitGainsPerTier = new int[9] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };

	public static readonly int[] TraitChoicesPerTier = new int[9] { 1, 2, 3, 4, 4, 4, 4, 4, 6 };

	public static readonly int[] GrowthMomentAges = new int[3] { 7, 10, 13 };

	public static bool IsGrowthBirthday(int age)
	{
		for (int i = 0; i < GrowthMomentAges.Length; i++)
		{
			if (age == GrowthMomentAges[i])
			{
				return true;
			}
		}
		return false;
	}

	[DebugOutput("Text generation", true)]
	private static void GrowthMomentFlavor()
	{
		if (!PawnsFinder.AllMaps_FreeColonists.TryRandomElement(out var targetPawn))
		{
			Log.Error("No colonists to generate growth moment flavor for.");
			return;
		}
		List<DebugMenuOption> list = new List<DebugMenuOption>();
		for (int i = 0; i < 9; i++)
		{
			int tier = i;
			list.Add(new DebugMenuOption("Tier " + tier, DebugMenuOptionMode.Action, delegate
			{
				string text = "Samples for " + targetPawn.NameShortColored.Resolve() + " at tier " + tier + ":";
				for (int j = 0; j < 10; j++)
				{
					text = text + "\n  - " + GrowthFlavorForTier(targetPawn, tier);
				}
				Log.Message(text);
			}));
		}
		Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
	}

	public static string GrowthFlavorForTier(Pawn pawn, int growthTier)
	{
		int num = ((growthTier >= 8) ? 4 : ((growthTier >= 6) ? 3 : ((growthTier >= 4) ? 2 : ((growthTier >= 1) ? 1 : 0))));
		GrammarRequest request = default(GrammarRequest);
		request.Includes.Add(RulePackDefOf.GrowthMomentFlavor);
		request.Constants.Add("tierSection", num.ToString());
		request.Rules.AddRange(GrammarUtility.RulesForPawn("PAWN", pawn, request.Constants, addRelationInfoSymbol: false));
		return GrammarResolver.Resolve("r_root", request);
	}
}
