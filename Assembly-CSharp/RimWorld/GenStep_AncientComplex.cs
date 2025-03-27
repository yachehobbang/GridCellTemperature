using System.Collections.Generic;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace RimWorld;

public class GenStep_AncientComplex : GenStep_ScattererBestFit
{
	private LayoutStructureSketch structureSketch;

	private static readonly IntVec2 DefaultComplexSize = new IntVec2(80, 80);

	public override int SeedPart => 235635649;

	protected override IntVec2 Size => new IntVec2(structureSketch.structureLayout.container.Width + 10, structureSketch.structureLayout.container.Height + 10);

	public override bool CollisionAt(IntVec3 cell, Map map)
	{
		List<Thing> thingList = cell.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i].def.IsBuildingArtificial)
			{
				return true;
			}
		}
		return false;
	}

	public override void Generate(Map map, GenStepParams parms)
	{
		count = 1;
		nearMapCenter = true;
		structureSketch = parms.sitePart.parms.ancientLayoutStructureSketch;
		if (structureSketch?.structureLayout == null)
		{
			TryRecoverEmptySketch(parms);
		}
		base.Generate(map, parms);
	}

	private void TryRecoverEmptySketch(GenStepParams parms)
	{
		bool flag = false;
		foreach (Quest item in Find.QuestManager.QuestsListForReading)
		{
			if (item.TryGetFirstPartOfType<QuestPart_SpawnWorldObject>(out var part) && part.worldObject == parms.sitePart.site && item.root.root is QuestNode_Root_AncientComplex questNode_Root_AncientComplex)
			{
				structureSketch = questNode_Root_AncientComplex.QuestSetupComplex(item, parms.sitePart.parms.points);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			StructureGenParams parms2 = new StructureGenParams
			{
				size = DefaultComplexSize
			};
			structureSketch = LayoutDefOf.AncientComplex.Worker.GenerateStructureSketch(parms2);
			Log.Warning("Failed to recover lost complex from any quest. Generating default.");
		}
	}

	protected override void ScatterAt(IntVec3 c, Map map, GenStepParams parms, int stackCount = 1)
	{
		ResolveParams resolveParams = default(ResolveParams);
		resolveParams.ancientLayoutStructureSketch = structureSketch;
		resolveParams.threatPoints = parms.sitePart.parms.threatPoints;
		resolveParams.rect = CellRect.CenteredOn(c, structureSketch.structureLayout.container.Width, structureSketch.structureLayout.container.Height);
		resolveParams.thingSetMakerDef = parms.sitePart.parms.ancientComplexRewardMaker;
		ResolveParams parms2 = resolveParams;
		FormCaravanComp component = parms.sitePart.site.GetComponent<FormCaravanComp>();
		if (component != null)
		{
			component.foggedRoomsCheckRect = parms2.rect;
		}
		GenerateComplex(map, parms2);
	}

	protected virtual void GenerateComplex(Map map, ResolveParams parms)
	{
		RimWorld.BaseGen.BaseGen.globalSettings.map = map;
		RimWorld.BaseGen.BaseGen.symbolStack.Push("ancientComplex", parms);
		RimWorld.BaseGen.BaseGen.Generate();
	}
}
