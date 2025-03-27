using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class PitGateExit : MapPortal
{
	private static readonly CachedTexture ExitPitGateTex = new CachedTexture("UI/Commands/ExitPitGate");

	private static readonly CachedTexture ViewSurfaceTex = new CachedTexture("UI/Commands/ViewUndercave");

	private static readonly Vector3 RopeDrawOffset = new Vector3(0f, 1f, 1f);

	public PitGate pitGate;

	[Unsaved(false)]
	private Graphic cachedRopeGraphic;

	public override string EnterCommandString => "ExitPitGate".Translate();

	protected override Texture2D EnterTex => ExitPitGateTex.Texture;

	private Graphic RopeGraphic
	{
		get
		{
			if (cachedRopeGraphic == null)
			{
				cachedRopeGraphic = GraphicDatabase.Get<Graphic_Single_AgeSecs>("Things/Building/PitGate/PitGateExit/PitGateExit_Rope", ShaderDatabase.CaveExitRope, def.graphicData.drawSize, Color.white);
			}
			return cachedRopeGraphic;
		}
	}

	public override Map GetOtherMap()
	{
		return pitGate.Map;
	}

	public override IntVec3 GetDestinationLocation()
	{
		return pitGate.Position;
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		RopeGraphic.Draw(DrawPos + RopeDrawOffset, Rot4.North, this);
	}

	public override void OnEntered(Pawn pawn)
	{
		base.OnEntered(pawn);
		if (Find.CurrentMap == base.Map)
		{
			SoundDefOf.TraversePitGate.PlayOneShot(this);
		}
		else if (Find.CurrentMap == pitGate.Map)
		{
			SoundDefOf.TraversePitGate.PlayOneShot(pitGate);
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (Gizmo gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		yield return new Command_Action
		{
			defaultLabel = "ViewSurface".Translate(),
			defaultDesc = "ViewSurfaceDesc".Translate(),
			icon = ViewSurfaceTex.Texture,
			action = delegate
			{
				CameraJumper.TryJumpAndSelect(pitGate);
			}
		};
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_References.Look(ref pitGate, "pitGate");
	}
}
