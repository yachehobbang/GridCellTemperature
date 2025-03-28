using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Noise;

namespace RimWorld;

public class GenStep_Undercaves : GenStep_Caves
{
	private const int MinUndercaveSize = 2000;

	private const int MaxTries = 100;

	private float baseWidth = 5f;

	public override void Generate(Map map, GenStepParams parms)
	{
		if (!ModLister.CheckAnomaly("Undercave"))
		{
			return;
		}
		directionNoise = new Perlin(0.002050000010058284, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);
		HashSet<IntVec3> hashSet = new HashSet<IntVec3>();
		List<IntVec3> list = map.AllCells.ToList();
		int num = 0;
		while (hashSet.Count < 2000)
		{
			num++;
			hashSet.Clear();
			MapGenerator.Caves.Clear();
			IntVec3 start = list.RandomElement();
			float width = Rand.Range(baseWidth * 0.8f, baseWidth);
			Dig(start, Rand.Range(0f, 360f), width, list, map, closed: true, hashSet);
			if (num > 100)
			{
				Log.Error($"Undercave generation exceeded {100} tries");
				break;
			}
		}
	}
}
