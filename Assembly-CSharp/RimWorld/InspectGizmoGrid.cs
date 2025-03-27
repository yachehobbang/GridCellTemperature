using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld;

public static class InspectGizmoGrid
{
	private static List<object> objList = new List<object>();

	private static List<Gizmo> gizmoList = new List<Gizmo>();

	private static int cacheFrame;

	private static List<object> tmpObjectCacheList = new List<object>();

	public static void DrawInspectGizmoGridFor(IEnumerable<object> selectedObjects, out Gizmo mouseoverGizmo)
	{
		mouseoverGizmo = null;
		ISelectable obj = null;
		if (Find.ScreenshotModeHandler.Active)
		{
			return;
		}
		try
		{
			bool flag = true;
			int frameCount = Time.frameCount;
			if (cacheFrame == frameCount)
			{
				tmpObjectCacheList.Clear();
				tmpObjectCacheList.AddRange(selectedObjects);
				if (objList.Count == tmpObjectCacheList.Count)
				{
					for (int i = 0; i < objList.Count; i++)
					{
						if (tmpObjectCacheList[i] != objList[i])
						{
							flag = false;
							break;
						}
					}
				}
				else
				{
					flag = false;
				}
			}
			else
			{
				flag = false;
			}
			if (!flag)
			{
				cacheFrame = frameCount;
				objList.Clear();
				objList.AddRange(selectedObjects);
				gizmoList.Clear();
				for (int j = 0; j < objList.Count; j++)
				{
					if (objList[j] is ISelectable selectable)
					{
						gizmoList.AddRange(selectable.GetGizmos());
					}
					if (objList[j] is Gizmo item)
					{
						gizmoList.Add(item);
					}
				}
				for (int k = 0; k < objList.Count; k++)
				{
					if (!(objList[k] is Thing t))
					{
						continue;
					}
					List<Designator> allDesignators = Find.ReverseDesignatorDatabase.AllDesignators;
					for (int l = 0; l < allDesignators.Count; l++)
					{
						Command_Action command_Action = allDesignators[l].CreateReverseDesignationGizmo(t);
						if (command_Action != null)
						{
							gizmoList.Add(command_Action);
						}
					}
				}
			}
			GizmoGridDrawer.DrawGizmoGrid(gizmoList, InspectPaneUtility.PaneWidthFor(Find.WindowStack.WindowOfType<IInspectPane>()) + GizmoGridDrawer.GizmoSpacing.y, out mouseoverGizmo, null, null, null, objList.Count > 1);
		}
		catch (Exception ex)
		{
			Log.ErrorOnce(ex.ToString() + " currentSelectable: " + obj.ToStringSafe(), 3427734);
		}
	}
}
