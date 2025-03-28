using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

[StaticConstructorOnStartup]
public static class FormCaravanGizmoUtility
{
	private static readonly Texture2D FormCaravanCommand = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan");

	private static Gizmo mouseoverGizmo;

	private static readonly List<Gizmo> tmpObjectsList = new List<Gizmo>();

	private static readonly List<MapParent> settlements = new List<MapParent>();

	private static readonly List<MapParent> possible = new List<MapParent>();

	public static void CaravanFormingUIOnGUI()
	{
		if (!Find.WindowStack.IsOpen<WorldInspectPane>())
		{
			tmpObjectsList.Clear();
			if (TryGetGizmo(out var gizmo))
			{
				tmpObjectsList.Add(gizmo);
			}
			GizmoGridDrawer.DrawGizmoGrid(tmpObjectsList, GizmoGridDrawer.GizmoSpacing.y, out mouseoverGizmo);
			tmpObjectsList.Clear();
		}
	}

	public static void CaravanFormingUIUpdate()
	{
		if (mouseoverGizmo != null)
		{
			mouseoverGizmo.GizmoUpdateOnMouseover();
		}
	}

	public static bool TryGetGizmo(out Gizmo gizmo)
	{
		gizmo = null;
		if (Find.WorldRoutePlanner.Active)
		{
			return false;
		}
		List<WorldObject> selectedObjects = Find.WorldSelector.SelectedObjects;
		int num = Find.WorldSelector.selectedTile;
		Settlement settlement = null;
		foreach (WorldObject item in selectedObjects)
		{
			if (item is Caravan || item is CaravansBattlefield || item is TravelingTransportPods)
			{
				return false;
			}
			if (item is Settlement settlement2)
			{
				settlement = settlement2;
			}
			FormCaravanComp component = item.GetComponent<FormCaravanComp>();
			if (component != null && component.CanReformNow())
			{
				return false;
			}
			if (num < 0)
			{
				num = item.Tile;
			}
		}
		RefreshSettlements();
		if (settlements.Count == 0)
		{
			return false;
		}
		settlement = settlement ?? Find.WorldObjects.SettlementAt(num);
		if (settlement != null)
		{
			if (settlement.Faction != Faction.OfPlayer)
			{
				gizmo = SendToTileGizmo(settlement.Tile);
			}
			else if (settlement.Map != null)
			{
				gizmo = GetFormCaravanAction("CommandFormCaravan".Translate(), "CommandFormCaravanDesc".Translate(), delegate
				{
					Find.WindowStack.Add(new Dialog_FormCaravan(settlement.Map, reform: false, null, mapAboutToBeRemoved: false, null));
				});
			}
			return true;
		}
		MapParent parent = Find.WorldObjects.MapParentAt(num);
		FormCaravanComp formCaravanComp = parent?.GetComponent<FormCaravanComp>();
		if (formCaravanComp != null && parent.HasMap && parent.Map.mapPawns.ColonistCount > 0 && !formCaravanComp.Reform)
		{
			gizmo = GetFormCaravanAction("CommandFormCaravan".Translate(), "CommandFormCaravanDesc".Translate(), delegate
			{
				Dialog_FormCaravan window = new Dialog_FormCaravan(parent.Map, reform: false, null, mapAboutToBeRemoved: false, null);
				Find.WindowStack.Add(window);
			});
			return true;
		}
		if (num >= 0)
		{
			gizmo = SendToTileGizmo(num);
			return true;
		}
		gizmo = GetFormCaravanAction("CommandFormCaravan".Translate(), "CommandFormCaravanDesc".Translate(), delegate
		{
			Dialog_FormCaravan window2 = new Dialog_FormCaravan(settlements[0].Map, reform: false, null, mapAboutToBeRemoved: false, null);
			Find.WindowStack.Add(window2);
		});
		return true;
	}

	private static Gizmo SendToTileGizmo(int selectedTile)
	{
		possible.Clear();
		if (!Find.World.Impassable(selectedTile) || Find.WorldObjects.AnySettlementAt(selectedTile))
		{
			for (int i = 0; i < settlements.Count; i++)
			{
				if (Find.WorldReachability.CanReach(settlements[i].Tile, selectedTile))
				{
					possible.Add(settlements[i]);
				}
			}
		}
		TaggedString taggedString = "CommandSendCaravanDesc".Translate();
		Gizmo gizmo = ((possible.Count <= 1) ? GetFormCaravanAction("CommandSendCaravan".Translate(), taggedString, delegate
		{
			DialogFromToSettlement(possible[0].Map, selectedTile);
		}) : GetFormCaravanAction("CommandSendCaravanMultiple".Translate(), taggedString, delegate
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			for (int j = 0; j < possible.Count; j++)
			{
				MapParent settlement = possible[j];
				list.Add(new FloatMenuOption(settlement.LabelCap, delegate
				{
					DialogFromToSettlement(settlement.Map, selectedTile);
				}));
			}
			FloatMenu window = new FloatMenu(list)
			{
				absorbInputAroundWindow = true
			};
			Find.WindowStack.Add(window);
		}));
		if (!possible.Any())
		{
			gizmo.Disable("CommandSendCaravanCantReach".Translate());
		}
		return gizmo;
	}

	private static void DialogFromToSettlement(Map origin, int tile)
	{
		Dialog_FormCaravan dialog_FormCaravan = new Dialog_FormCaravan(origin, reform: false, null, mapAboutToBeRemoved: false, null);
		Find.WindowStack.Add(dialog_FormCaravan);
		WorldRoutePlanner worldRoutePlanner = Find.WorldRoutePlanner;
		worldRoutePlanner.Start(dialog_FormCaravan);
		worldRoutePlanner.TryAddWaypoint(tile);
	}

	private static Command_Action GetFormCaravanAction(string label, string desc, Action action)
	{
		return new Command_Action
		{
			tutorTag = "FormCaravan",
			defaultLabel = label,
			defaultDesc = desc,
			icon = FormCaravanCommand,
			hotKey = KeyBindingDefOf.Misc2,
			action = action
		};
	}

	private static void RefreshSettlements()
	{
		for (int num = settlements.Count - 1; num >= 0; num--)
		{
			if (settlements[num] == null)
			{
				settlements.RemoveAt(num);
			}
			else if (!Current.Game.Maps.Contains(settlements[num].Map))
			{
				settlements.RemoveAt(num);
			}
		}
		for (int i = 0; i < Current.Game.Maps.Count; i++)
		{
			Map map = Current.Game.Maps[i];
			if (map.IsPlayerHome && !settlements.Contains(map.Parent))
			{
				settlements.Add(map.Parent);
			}
		}
	}
}
