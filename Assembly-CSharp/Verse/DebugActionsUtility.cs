using System.Collections.Generic;

namespace Verse;

public static class DebugActionsUtility
{
	public static void DustPuffFrom(Thing t)
	{
		if (t is Pawn pawn)
		{
			pawn.Drawer.Notify_DebugAffected();
		}
	}

	public static IEnumerable<float> PointsOptions(bool extended)
	{
		if (!extended)
		{
			yield return 35f;
			yield return 70f;
			yield return 100f;
			yield return 150f;
			yield return 200f;
			yield return 350f;
			yield return 500f;
			yield return 700f;
			yield return 1000f;
			yield return 1200f;
			yield return 1500f;
			yield return 2000f;
			yield return 3000f;
			yield return 4000f;
			yield return 5000f;
		}
		else
		{
			for (int i = 20; i < 100; i += 10)
			{
				yield return i;
			}
			for (int i = 100; i < 500; i += 25)
			{
				yield return i;
			}
			for (int i = 500; i < 1500; i += 50)
			{
				yield return i;
			}
			for (int i = 1500; i <= 5000; i += 100)
			{
				yield return i;
			}
		}
		yield return 6000f;
		yield return 7000f;
		yield return 8000f;
		yield return 9000f;
		yield return 10000f;
	}

	public static IEnumerable<int> PopulationOptions()
	{
		for (int i = 1; i <= 20; i++)
		{
			yield return i;
		}
		for (int i = 30; i <= 50; i += 10)
		{
			yield return i;
		}
	}
}
