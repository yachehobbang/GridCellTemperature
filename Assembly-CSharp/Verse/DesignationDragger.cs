using System.Collections.Generic;
using UnityEngine;
using Verse.Sound;

namespace Verse;

[StaticConstructorOnStartup]
public class DesignationDragger
{
	private bool dragging;

	private IntVec3 startDragCell;

	private int lastFrameDragCellsDrawn;

	private Sustainer sustainer;

	private float lastDragRealTime = -1000f;

	private List<IntVec3> dragCells = new List<IntVec3>();

	private string failureReasonInt;

	private int lastUpdateFrame = -1;

	private static readonly Texture2D OutlineTex = SolidColorMaterials.NewSolidColorTexture(new Color32(109, 139, 79, 100));

	private const string TimeSinceDragParam = "TimeSinceDrag";

	protected List<IntVec3> tmpHighlightCells = new List<IntVec3>();

	private int numSelectedCells;

	public bool Dragging => dragging;

	private Designator SelDes => Find.DesignatorManager.SelectedDesignator;

	public List<IntVec3> DragCells
	{
		get
		{
			UpdateDragCellsIfNeeded();
			return dragCells;
		}
	}

	public string FailureReason
	{
		get
		{
			UpdateDragCellsIfNeeded();
			return failureReasonInt;
		}
	}

	public void StartDrag()
	{
		dragging = true;
		startDragCell = UI.MouseCell();
	}

	public void EndDrag()
	{
		dragging = false;
		lastDragRealTime = -99999f;
		lastFrameDragCellsDrawn = 0;
		if (sustainer != null)
		{
			sustainer.End();
			sustainer = null;
		}
	}

	public void DraggerUpdate()
	{
		if (!dragging)
		{
			return;
		}
		tmpHighlightCells.Clear();
		numSelectedCells = 0;
		CellRect cellRect = DragRect();
		CellRect cellRect2 = cellRect.ClipInsideRect(Find.CameraDriver.CurrentViewRect.ExpandedBy(3)).ClipInsideMap(SelDes.Map);
		foreach (IntVec3 item in cellRect)
		{
			if ((bool)SelDes.CanDesignateCell(item))
			{
				if (cellRect2.Contains(item))
				{
					tmpHighlightCells.Add(item);
				}
				numSelectedCells++;
			}
		}
		SelDes.RenderHighlight(tmpHighlightCells);
		if (numSelectedCells != lastFrameDragCellsDrawn)
		{
			if (SelDes.soundDragChanged != null)
			{
				SoundInfo info = SoundInfo.OnCamera();
				info.SetParameter("TimeSinceDrag", Time.realtimeSinceStartup - lastDragRealTime);
				SelDes.soundDragChanged.PlayOneShot(info);
			}
			lastDragRealTime = Time.realtimeSinceStartup;
			lastFrameDragCellsDrawn = numSelectedCells;
		}
		if (sustainer == null || sustainer.Ended)
		{
			if (SelDes.soundDragSustain != null)
			{
				sustainer = SelDes.soundDragSustain.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.PerFrame));
			}
		}
		else
		{
			sustainer.externalParams["TimeSinceDrag"] = Time.realtimeSinceStartup - lastDragRealTime;
			sustainer.Maintain();
		}
	}

	public void DraggerOnGUI()
	{
		if (!dragging || SelDes == null)
		{
			return;
		}
		IntVec3 intVec = startDragCell - UI.MouseCell();
		intVec.x = Mathf.Abs(intVec.x) + 1;
		intVec.z = Mathf.Abs(intVec.z) + 1;
		if (SelDes.DragDrawOutline && (intVec.x > 1 || intVec.z > 1))
		{
			IntVec3 intVec2 = UI.MouseCell();
			Vector3 v = new Vector3(Mathf.Min(startDragCell.x, intVec2.x), 0f, Mathf.Min(startDragCell.z, intVec2.z));
			Vector3 v2 = new Vector3(Mathf.Max(startDragCell.x, intVec2.x) + 1, 0f, Mathf.Max(startDragCell.z, intVec2.z) + 1);
			Vector2 vector = v.MapToUIPosition();
			Vector2 vector2 = v2.MapToUIPosition();
			Widgets.DrawBox(Rect.MinMaxRect(vector.x, vector.y, vector2.x, vector2.y), 1, OutlineTex);
		}
		if (SelDes.DragDrawMeasurements)
		{
			if (intVec.x >= 3)
			{
				Vector2 screenPos = (startDragCell.ToUIPosition() + UI.MouseCell().ToUIPosition()) / 2f;
				screenPos.y = startDragCell.ToUIPosition().y;
				Widgets.DrawNumberOnMap(screenPos, intVec.x, Color.white);
			}
			if (intVec.z >= 3)
			{
				Vector2 screenPos2 = (startDragCell.ToUIPosition() + UI.MouseCell().ToUIPosition()) / 2f;
				screenPos2.x = startDragCell.ToUIPosition().x;
				Widgets.DrawNumberOnMap(screenPos2, intVec.z, Color.white);
			}
		}
		if (intVec.x >= 5 && intVec.z >= 5 && numSelectedCells > 0)
		{
			Widgets.DrawNumberOnMap((startDragCell.ToUIPosition() + UI.MouseCell().ToUIPosition()) / 2f, numSelectedCells, Color.white);
		}
	}

	public CellRect DragRect()
	{
		IntVec3 intVec = startDragCell;
		IntVec3 intVec2 = UI.MouseCell();
		if (SelDes.DraggableDimensions == 1)
		{
			bool flag = true;
			if (Mathf.Abs(intVec.x - intVec2.x) < Mathf.Abs(intVec.z - intVec2.z))
			{
				flag = false;
			}
			if (flag)
			{
				int z = intVec.z;
				if (intVec.x > intVec2.x)
				{
					IntVec3 intVec3 = intVec;
					intVec = intVec2;
					intVec2 = intVec3;
				}
				return CellRect.FromLimits(intVec.x, z, intVec2.x, z);
			}
			int x = intVec.x;
			if (intVec.z > intVec2.z)
			{
				IntVec3 intVec4 = intVec;
				intVec = intVec2;
				intVec2 = intVec4;
			}
			return CellRect.FromLimits(x, intVec.z, x, intVec2.z);
		}
		if (SelDes.DraggableDimensions == 2)
		{
			IntVec3 intVec5 = intVec;
			IntVec3 intVec6 = intVec2;
			if (intVec6.x < intVec5.x)
			{
				int x2 = intVec5.x;
				intVec5 = new IntVec3(intVec6.x, intVec5.y, intVec5.z);
				intVec6 = new IntVec3(x2, intVec6.y, intVec6.z);
			}
			if (intVec6.z < intVec5.z)
			{
				int z2 = intVec5.z;
				intVec5 = new IntVec3(intVec5.x, intVec5.y, intVec6.z);
				intVec6 = new IntVec3(intVec6.x, intVec6.y, z2);
			}
			return CellRect.FromLimits(intVec5.x, intVec5.z, intVec6.x, intVec6.z);
		}
		return CellRect.Empty;
	}

	private void UpdateDragCellsIfNeeded()
	{
		if (Time.frameCount == lastUpdateFrame)
		{
			return;
		}
		lastUpdateFrame = Time.frameCount;
		dragCells.Clear();
		failureReasonInt = null;
		if (SelDes.DraggableDimensions <= 0)
		{
			return;
		}
		CellRect cellRect = DragRect();
		for (int i = cellRect.minX; i <= cellRect.maxX; i++)
		{
			for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
			{
				TryAddDragCell(new IntVec3(i, startDragCell.y, j));
			}
		}
	}

	private void TryAddDragCell(IntVec3 c)
	{
		AcceptanceReport acceptanceReport = SelDes.CanDesignateCell(c);
		if (acceptanceReport.Accepted)
		{
			dragCells.Add(c);
		}
		else if (!acceptanceReport.Reason.NullOrEmpty())
		{
			failureReasonInt = acceptanceReport.Reason;
		}
	}
}
