using System.Text;
using Verse;

namespace RimWorld;

public class StatWorker_SuppressionFallRate : StatWorker
{
	public const float FastFallRate = 0.2f;

	public const float MediumFallRate = 0.1f;

	public const float SlowFallRate = 0.05f;

	public const float FastFallRateThreshold = 0.3f;

	public const float MediumFallRateThreshold = 0.15f;

	public override bool ShouldShowFor(StatRequest req)
	{
		if (!base.ShouldShowFor(req))
		{
			return false;
		}
		if (!(req.Thing is Pawn pawn))
		{
			return false;
		}
		return pawn.IsSlave;
	}

	private static float CurrentFallRateBasedOnSuppression(float suppression)
	{
		if (suppression > 0.3f)
		{
			return 0.2f;
		}
		if (suppression > 0.15f)
		{
			return 0.1f;
		}
		return 0.05f;
	}

	public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
	{
		return CurrentFallRateBasedOnSuppression(((Pawn)req.Thing).needs.TryGetNeed<Need_Suppression>().CurLevelPercentage);
	}

	public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
	{
		Need_Suppression need_Suppression = ((Pawn)req.Thing).needs.TryGetNeed<Need_Suppression>();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetExplanationUnfinalized(req, numberSense));
		stringBuilder.Append(string.Format("{0} ({1}): {2}", "CurrentSuppression".Translate(), need_Suppression.CurLevelPercentage.ToStringPercent(), CurrentFallRateBasedOnSuppression(need_Suppression.CurLevelPercentage).ToStringPercent()));
		return stringBuilder.ToString();
	}

	public string GetExplanationForTooltip(StatRequest req)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("SuppressionFallRate".Translate() + ": " + GetValue(req.Thing).ToStringPercent());
		Need_Suppression need_Suppression = ((Pawn)req.Thing).needs.TryGetNeed<Need_Suppression>();
		stringBuilder.AppendLine(string.Format("   {0} ({1}): {2}", "CurrentSuppression".Translate(), need_Suppression.CurLevelPercentage.ToStringPercent(), CurrentFallRateBasedOnSuppression(need_Suppression.CurLevelPercentage).ToStringPercent()));
		if (stat.parts != null)
		{
			for (int i = 0; i < stat.parts.Count; i++)
			{
				string text = stat.parts[i].ExplanationPart(req);
				if (!text.NullOrEmpty())
				{
					stringBuilder.AppendLine("   " + text);
				}
			}
		}
		return stringBuilder.ToString();
	}
}
