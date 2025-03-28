using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public static class CaravanTicksPerMoveUtility
{
	public struct CaravanInfo
	{
		public List<Pawn> pawns;

		public float massUsage;

		public float massCapacity;

		public CaravanInfo(Caravan caravan)
		{
			pawns = caravan.PawnsListForReading;
			massUsage = caravan.MassUsage;
			massCapacity = caravan.MassCapacity;
		}

		public CaravanInfo(Dialog_FormCaravan formCaravanDialog)
		{
			pawns = TransferableUtility.GetPawnsFromTransferables(formCaravanDialog.transferables);
			massUsage = formCaravanDialog.MassUsage;
			massCapacity = formCaravanDialog.MassCapacity;
		}
	}

	public const float CellToTilesConversionRatio = 340f;

	public const int DefaultTicksPerMove = 3300;

	private const float MoveSpeedFactorAtZeroMass = 2f;

	private static List<float> caravanAnimalSpeedFactors = new List<float>();

	public static int GetTicksPerMove(Caravan caravan, StringBuilder explanation = null)
	{
		if (caravan == null)
		{
			if (explanation != null)
			{
				AppendUsingDefaultTicksPerMoveInfo(explanation);
			}
			return 3300;
		}
		return GetTicksPerMove(new CaravanInfo(caravan), explanation);
	}

	public static int GetTicksPerMove(CaravanInfo caravanInfo, StringBuilder explanation = null)
	{
		return GetTicksPerMove(caravanInfo.pawns, caravanInfo.massUsage, caravanInfo.massCapacity, explanation);
	}

	public static int GetTicksPerMove(List<Pawn> pawns, float massUsage, float massCapacity, StringBuilder explanation = null)
	{
		caravanAnimalSpeedFactors.Clear();
		if (pawns.Any())
		{
			int num = 0;
			foreach (Pawn pawn in pawns)
			{
				if (pawn.RaceProps.Humanlike)
				{
					num++;
				}
				else if (pawn.IsCaravanRideable())
				{
					caravanAnimalSpeedFactors.Add(pawn.GetStatValue(StatDefOf.CaravanRidingSpeedFactor));
				}
			}
			float num2 = 1f;
			int num3 = 0;
			int count = caravanAnimalSpeedFactors.Count;
			if (count > 0 && num > 0)
			{
				caravanAnimalSpeedFactors.Sort();
				caravanAnimalSpeedFactors.Reverse();
				if (caravanAnimalSpeedFactors.Count > num)
				{
					caravanAnimalSpeedFactors.RemoveRange(num, caravanAnimalSpeedFactors.Count - num);
				}
				num3 = caravanAnimalSpeedFactors.Count;
				while (caravanAnimalSpeedFactors.Count < num)
				{
					caravanAnimalSpeedFactors.Add(1f);
				}
				num2 = caravanAnimalSpeedFactors.Average();
			}
			float num4 = (float)BaseHumanlikeTicksPerCell() * 340f;
			float moveSpeedFactorFromMass = GetMoveSpeedFactorFromMass(massUsage, massCapacity);
			int num5 = Mathf.Max(Mathf.RoundToInt(num4 / (moveSpeedFactorFromMass * num2)), 1);
			bool flag = massUsage > massCapacity;
			if (explanation != null)
			{
				float num6 = 60000f / num4;
				explanation.Append("CaravanMovementSpeedFull".Translate() + ":");
				explanation.AppendLine();
				explanation.Append("  " + "StatsReport_BaseValue".Translate() + ": " + num6.ToString("0.#") + " " + "TilesPerDay".Translate());
				explanation.AppendLine();
				explanation.Append("  " + "RideableAnimalsPerPeople".Translate() + $": {count} / {num}");
				if (num3 > 0)
				{
					explanation.AppendLine();
					explanation.Append("  " + "MultiplierFromRiddenAnimals".Translate() + ": " + num2.ToStringPercent());
				}
				if (!flag)
				{
					explanation.AppendLine();
					explanation.Append("  " + "MultiplierForCarriedMass".Translate(moveSpeedFactorFromMass.ToStringPercent()));
				}
				float num7 = 60000f / (float)num5;
				explanation.AppendLine();
				explanation.Append("  " + "FinalCaravanPawnsMovementSpeed".Translate() + ": " + num7.ToString("0.#") + " " + "TilesPerDay".Translate());
			}
			return num5;
		}
		if (explanation != null)
		{
			AppendUsingDefaultTicksPerMoveInfo(explanation);
		}
		return 3300;
	}

	private static int BaseHumanlikeTicksPerCell()
	{
		float num = ThingDefOf.Human.GetStatValueAbstract(StatDefOf.MoveSpeed) / 60f;
		return Mathf.RoundToInt(1f / num);
	}

	private static float GetMoveSpeedFactorFromMass(float massUsage, float massCapacity)
	{
		if (massCapacity <= 0f)
		{
			return 1f;
		}
		float t = massUsage / massCapacity;
		return Mathf.Lerp(2f, 1f, t);
	}

	private static void AppendUsingDefaultTicksPerMoveInfo(StringBuilder sb)
	{
		sb.Append("CaravanMovementSpeedFull".Translate() + ":");
		float num = 18.181818f;
		sb.AppendLine();
		sb.Append("  " + "Default".Translate() + ": " + num.ToString("0.#") + " " + "TilesPerDay".Translate());
	}
}
