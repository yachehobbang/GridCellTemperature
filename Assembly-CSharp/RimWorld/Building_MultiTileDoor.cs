using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class Building_MultiTileDoor : Building_Door
{
	private bool openedBefore;

	[Unsaved(false)]
	private Graphic suppportGraphic;

	[Unsaved(false)]
	private Graphic topGraphic;

	private const float UpperMoverOffsetStart = 0.25f;

	private const float UpperMoverOffsetEnd = 0.6f;

	private static readonly float UpperMoverAltitude = AltitudeLayer.DoorMoveable.AltitudeFor() + 1f / 52f;

	private static readonly Vector3 MoverDrawScale = new Vector3(0.5f, 1f, 1f);

	private Graphic SupportGraphic
	{
		get
		{
			if (suppportGraphic == null)
			{
				Graphic graphic = def.building.doorSupportGraphic?.Graphic;
				if (graphic == null)
				{
					return null;
				}
				suppportGraphic = graphic.GetColoredVersion(graphic.Shader, DrawColor, Graphic.ColorTwo);
			}
			return suppportGraphic;
		}
	}

	private Graphic TopGraphic
	{
		get
		{
			if (topGraphic == null)
			{
				Graphic graphic = def.building.doorTopGraphic?.Graphic;
				if (graphic == null)
				{
					return null;
				}
				topGraphic = graphic.GetColoredVersion(graphic.Shader, DrawColor, Graphic.ColorTwo);
			}
			return topGraphic;
		}
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		DoorPreDraw();
		if (!base.StuckOpen)
		{
			float offsetDist = 0.25f + 0.35000002f * base.OpenPct;
			DrawMovers(drawLoc, offsetDist, Graphic, AltitudeLayer.DoorMoveable.AltitudeFor(), MoverDrawScale, Graphic.ShadowGraphic);
			if (def.building.upperMoverGraphic != null)
			{
				float offsetDist2 = 0.25f + 0.35000002f * Mathf.Clamp01(base.OpenPct * 2.5f);
				DrawMovers(drawLoc, offsetDist2, def.building.upperMoverGraphic.Graphic, UpperMoverAltitude, MoverDrawScale, null);
			}
		}
		Vector3 drawPos = DrawPos;
		bool flag = base.Rotation == Rot4.North || base.Rotation == Rot4.South;
		if (flag)
		{
			drawPos.z += 0.1f;
		}
		drawPos.y = (flag ? AltitudeLayer.BuildingOnTop.AltitudeFor() : AltitudeLayer.Blueprint.AltitudeFor());
		SupportGraphic?.Draw(drawPos, base.Rotation, this);
		drawPos.y = AltitudeLayer.Blueprint.AltitudeFor();
		TopGraphic?.Draw(drawPos, base.Rotation, this);
	}

	public override void Tick()
	{
		base.Tick();
		if (!openInt && base.OpenPct <= 0f && openedBefore)
		{
			openedBefore = false;
			if (!def.building.soundDoorCloseEnd.NullOrUndefined())
			{
				def.building.soundDoorCloseEnd.PlayOneShot(this);
			}
		}
	}

	protected override void DoorOpen(int ticksToClose = 110)
	{
		base.DoorOpen(ticksToClose);
		openedBefore = true;
	}

	public override void Notify_ColorChanged()
	{
		suppportGraphic = null;
		topGraphic = null;
		base.Notify_ColorChanged();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref openedBefore, "openedBefore", defaultValue: false);
	}
}
