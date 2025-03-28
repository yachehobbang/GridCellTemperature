using UnityEngine;
using Verse;

namespace RimWorld.Planet;

public class WorldLayer_SettleTile : WorldLayer_SingleTile
{
	protected override int Tile
	{
		get
		{
			if (!(Find.WorldInterface.inspectPane.mouseoverGizmo is Command_Settle))
			{
				return -1;
			}
			if (!(Find.WorldSelector.SingleSelectedObject is Caravan caravan))
			{
				return -1;
			}
			return caravan.Tile;
		}
	}

	protected override Material Material => WorldMaterials.CurrentMapTile;

	protected override float Alpha => Mathf.Abs(Time.time % 2f - 1f);
}
