using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class Selector
{
	public DragBox dragBox = new DragBox();

	public MultiPawnGotoController gotoController = new MultiPawnGotoController();

	private List<object> selected = new List<object>();

	public const float PawnSelectRadius = 1f;

	private const int MaxNumSelected = 200;

	private static List<Pawn> tmpSelectedPawns = new List<Pawn>();

	private static List<Thing> tmp_selectedBoxThings = new List<Thing>();

	private static List<Pawn> tmpDraftedGotoPawns = new List<Pawn>();

	private bool ShiftIsHeld
	{
		get
		{
			if (!Input.GetKey(KeyCode.LeftShift))
			{
				return Input.GetKey(KeyCode.RightShift);
			}
			return true;
		}
	}

	public List<object> SelectedObjects => selected;

	public List<object> SelectedObjectsListForReading => selected;

	public Thing SingleSelectedThing
	{
		get
		{
			if (selected.Count != 1)
			{
				return null;
			}
			if (selected[0] is Thing)
			{
				return (Thing)selected[0];
			}
			return null;
		}
	}

	public object FirstSelectedObject
	{
		get
		{
			if (selected.Count == 0)
			{
				return null;
			}
			return selected[0];
		}
	}

	public object SingleSelectedObject
	{
		get
		{
			if (selected.Count != 1)
			{
				return null;
			}
			return selected[0];
		}
	}

	public List<Pawn> SelectedPawns
	{
		get
		{
			tmpSelectedPawns.Clear();
			for (int i = 0; i < selected.Count; i++)
			{
				if (selected[i] is Pawn item)
				{
					tmpSelectedPawns.Add(item);
				}
			}
			return tmpSelectedPawns;
		}
	}

	public int NumSelected => selected.Count;

	public Zone SelectedZone
	{
		get
		{
			if (selected.Count == 0)
			{
				return null;
			}
			return selected[0] as Zone;
		}
		set
		{
			ClearSelection();
			if (value != null)
			{
				Select(value);
			}
		}
	}

	public bool AnyPawnSelected
	{
		get
		{
			for (int i = 0; i < selected.Count; i++)
			{
				if (selected[i] is Pawn)
				{
					return true;
				}
			}
			return false;
		}
	}

	public void SelectorOnGUI()
	{
		HandleMapClicks();
		if (KeyBindingDefOf.Cancel.KeyDownEvent && selected.Count > 0)
		{
			ClearSelection();
			Event.current.Use();
		}
		if (NumSelected > 0 && Find.MainTabsRoot.OpenTab == null && !WorldRendererUtility.WorldRenderedNow)
		{
			Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Inspect, playSound: false);
		}
	}

	public void SelectorOnGUI_BeforeMainTabs()
	{
		if (gotoController.Active)
		{
			gotoController.OnGUI();
		}
	}

	private void HandleMapClicks()
	{
		if (Event.current.type == EventType.MouseDown)
		{
			if (Event.current.button == 0)
			{
				if (Event.current.clickCount == 1)
				{
					dragBox.active = true;
					dragBox.start = UI.MouseMapPosition();
				}
				if (Event.current.clickCount == 2)
				{
					SelectAllMatchingObjectUnderMouseOnScreen();
				}
				Event.current.Use();
			}
			if (Event.current.button == 1 && selected.Count > 0)
			{
				if (selected.Count == 1)
				{
					if (selected[0] is Pawn)
					{
						FloatMenuMakerMap.TryMakeFloatMenu((Pawn)selected[0]);
					}
					else if (selected[0] is Thing selectedThing)
					{
						FloatMenuMakerMap.TryMakeFloatMenu_NonPawn(selectedThing);
					}
				}
				else if (!FloatMenuMakerMap.TryMakeMultiSelectFloatMenu(SelectedPawns))
				{
					MassTakeFirstAutoTakeableOptionOrGoto();
				}
				Event.current.Use();
			}
		}
		if (Event.current.rawType == EventType.MouseUp)
		{
			if (Event.current.button == 0)
			{
				if (dragBox.active)
				{
					dragBox.active = false;
					if (!dragBox.IsValid)
					{
						SelectUnderMouse();
					}
					else
					{
						SelectInsideDragBox();
					}
				}
			}
			else if (Event.current.button == 1 && gotoController.Active)
			{
				gotoController.FinalizeInteraction();
			}
			Event.current.Use();
		}
		if (gotoController.Active && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
		{
			gotoController.FinalizeInteraction();
		}
		if (gotoController.Active)
		{
			gotoController.ProcessInputEvents();
		}
	}

	public bool IsSelected(object obj)
	{
		return selected.Contains(obj);
	}

	public void ClearSelection()
	{
		SelectionDrawer.Clear();
		selected.Clear();
		gotoController.Deactivate();
	}

	public void Deselect(object obj)
	{
		DeselectInternal(obj);
	}

	private void DeselectInternal(object obj)
	{
		if (selected.Contains(obj))
		{
			selected.Remove(obj);
		}
	}

	public void Select(object obj, bool playSound = true, bool forceDesignatorDeselect = true)
	{
		SelectInternal(obj, playSound, forceDesignatorDeselect);
	}

	private void SelectInternal(object obj, bool playSound = true, bool forceDesignatorDeselect = true)
	{
		if (obj == null)
		{
			Log.Error("Cannot select null.");
			return;
		}
		Thing thing = obj as Thing;
		if (thing == null && !(obj is Zone))
		{
			Log.Error(string.Concat("Tried to select ", obj, " which is neither a Thing nor a Zone."));
			return;
		}
		if (thing != null && thing.Destroyed)
		{
			Log.Error("Cannot select destroyed thing.");
			return;
		}
		CompSelectProxy compSelectProxy;
		if ((compSelectProxy = thing.TryGetComp<CompSelectProxy>()) != null && compSelectProxy.thingToSelect != null)
		{
			SelectInternal(compSelectProxy.thingToSelect, playSound, forceDesignatorDeselect);
			return;
		}
		if (obj is Pawn p && p.IsWorldPawn())
		{
			Log.Error("Cannot select world pawns.");
			return;
		}
		if (forceDesignatorDeselect)
		{
			Find.DesignatorManager.Deselect();
		}
		if (SelectedZone != null && !(obj is Zone))
		{
			ClearSelection();
		}
		if (obj is Zone && SelectedZone == null)
		{
			ClearSelection();
		}
		Map map = ((thing != null) ? thing.MapHeld : ((Zone)obj).Map);
		for (int num = selected.Count - 1; num >= 0; num--)
		{
			if (((selected[num] is Thing thing2) ? thing2.MapHeld : ((Zone)selected[num]).Map) != map)
			{
				Deselect(selected[num]);
			}
		}
		if (selected.Count < 200 && !IsSelected(obj))
		{
			if (map != Find.CurrentMap)
			{
				Current.Game.CurrentMap = map;
				SoundDefOf.MapSelected.PlayOneShotOnCamera();
				IntVec3 cell = thing?.PositionHeld ?? ((Zone)obj).Cells[0];
				Find.CameraDriver.JumpToCurrentMapLoc(cell);
			}
			if (playSound)
			{
				PlaySelectionSoundFor(obj);
			}
			selected.Add(obj);
			thing?.Notify_ThingSelected();
			SelectionDrawer.Notify_Selected(obj);
		}
	}

	public void Notify_DialogOpened()
	{
		dragBox.active = false;
		gotoController.Deactivate();
	}

	private void PlaySelectionSoundFor(object obj)
	{
		if (obj is Pawn && ((Pawn)obj).Faction == Faction.OfPlayer && ((Pawn)obj).RaceProps.Humanlike)
		{
			SoundDefOf.ColonistSelected.PlayOneShotOnCamera();
		}
		else if (obj is Thing || obj is Zone)
		{
			SoundDefOf.ThingSelected.PlayOneShotOnCamera();
		}
		else
		{
			Log.Warning("Can't determine selection sound for " + obj);
		}
	}

	private void SelectInsideDragBox()
	{
		if (!ShiftIsHeld)
		{
			ClearSelection();
		}
		bool selectedSomething = false;
		List<Thing> list = Find.ColonistBar.MapColonistsOrCorpsesInScreenRect(dragBox.ScreenRect);
		for (int i = 0; i < list.Count; i++)
		{
			selectedSomething = true;
			Select(list[i]);
		}
		if (selectedSomething)
		{
			return;
		}
		List<Caravan> list2 = Find.ColonistBar.CaravanMembersCaravansInScreenRect(dragBox.ScreenRect);
		for (int j = 0; j < list2.Count; j++)
		{
			if (!selectedSomething)
			{
				CameraJumper.TryJumpAndSelect(list2[j]);
				selectedSomething = true;
			}
			else
			{
				Find.WorldSelector.Select(list2[j]);
			}
		}
		if (selectedSomething)
		{
			return;
		}
		List<Thing> boxThings = ThingSelectionUtility.MultiSelectableThingsInScreenRectDistinct(dragBox.ScreenRect).ToList();
		Predicate<Thing> predicate2 = (Thing t) => t.def.category == ThingCategory.Pawn && (((Pawn)t).RaceProps.Humanlike || (ModsConfig.BiotechActive && ((Pawn)t).RaceProps.IsMechanoid)) && t.Faction == Faction.OfPlayer;
		if (SelectWhere(predicate2, SelectorUtility.SortInColonistBarOrder))
		{
			return;
		}
		Predicate<Thing> predicate3 = (Thing t) => t.def.category == ThingCategory.Pawn && ((Pawn)t).RaceProps.Humanlike;
		if (SelectWhere(predicate3, null))
		{
			return;
		}
		Predicate<Thing> predicate4 = (Thing t) => t.def.CountAsResource;
		if (SelectWhere(predicate4, null))
		{
			return;
		}
		Predicate<Thing> predicate5 = (Thing t) => t.def.category == ThingCategory.Pawn;
		if (SelectWhere(predicate5, null) || SelectWhere((Thing t) => t.def.selectable, null))
		{
			return;
		}
		foreach (Zone item in ThingSelectionUtility.MultiSelectableZonesInScreenRectDistinct(dragBox.ScreenRect).ToList())
		{
			selectedSomething = true;
			Select(item);
		}
		if (!selectedSomething)
		{
			SelectUnderMouse();
		}
		bool SelectWhere(Predicate<Thing> predicate, Action<List<Thing>> postProcessor)
		{
			tmp_selectedBoxThings.Clear();
			foreach (Thing item2 in boxThings.Where((Thing t) => predicate(t)))
			{
				tmp_selectedBoxThings.Add(item2);
			}
			if (tmp_selectedBoxThings.Any())
			{
				postProcessor?.Invoke(tmp_selectedBoxThings);
				foreach (Thing tmp_selectedBoxThing in tmp_selectedBoxThings)
				{
					Select(tmp_selectedBoxThing);
					selectedSomething = true;
				}
			}
			tmp_selectedBoxThings.Clear();
			return selectedSomething;
		}
	}

	private IEnumerable<object> SelectableObjectsUnderMouse()
	{
		Vector2 mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
		Thing thing = Find.ColonistBar.ColonistOrCorpseAt(mousePositionOnUIInverted);
		if (thing != null && thing.SpawnedOrAnyParentSpawned)
		{
			yield return thing;
		}
		else
		{
			if (!UI.MouseCell().InBounds(Find.CurrentMap))
			{
				yield break;
			}
			TargetingParameters targetingParameters = new TargetingParameters();
			targetingParameters.mustBeSelectable = true;
			targetingParameters.canTargetPawns = true;
			targetingParameters.canTargetBuildings = true;
			targetingParameters.canTargetItems = true;
			targetingParameters.mapObjectTargetsMustBeAutoAttackable = false;
			List<Thing> selectableList = GenUI.ThingsUnderMouse(UI.MouseMapPosition(), 1f, targetingParameters);
			if (selectableList.Count > 0 && selectableList[0] is Pawn && (selectableList[0].DrawPos - UI.MouseMapPosition()).MagnitudeHorizontal() < 0.4f)
			{
				for (int num = selectableList.Count - 1; num >= 0; num--)
				{
					Thing thing2 = selectableList[num];
					if (thing2.def.category == ThingCategory.Pawn && (thing2.DrawPosHeld.Value - UI.MouseMapPosition()).MagnitudeHorizontal() > 0.4f)
					{
						selectableList.Remove(thing2);
					}
				}
			}
			for (int i = 0; i < selectableList.Count; i++)
			{
				yield return selectableList[i];
			}
			Zone zone = Find.CurrentMap.zoneManager.ZoneAt(UI.MouseCell());
			if (zone != null)
			{
				yield return zone;
			}
		}
	}

	public static IEnumerable<object> SelectableObjectsAt(IntVec3 c, Map map)
	{
		List<Thing> thingList = c.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			Thing t = thingList[i];
			if (!ThingSelectionUtility.SelectableByMapClick(t))
			{
				continue;
			}
			yield return t;
			foreach (Thing item in ContainingSelectionUtility.SelectableContainedThings(t))
			{
				yield return item;
			}
		}
		Zone zone = map.zoneManager.ZoneAt(c);
		if (zone != null)
		{
			yield return zone;
		}
	}

	private void SelectUnderMouse()
	{
		Caravan caravan = Find.ColonistBar.CaravanMemberCaravanAt(UI.MousePositionOnUIInverted);
		if (caravan != null)
		{
			CameraJumper.TryJumpAndSelect(caravan);
			return;
		}
		List<object> list = SelectableObjectsUnderMouse().ToList();
		list.SortBy((object x) => (!(x is Building_Storage)) ? 1 : 0);
		if (list.Count == 0)
		{
			if (!ShiftIsHeld)
			{
				ClearSelection();
			}
		}
		else if (list.Count == 1)
		{
			object obj2 = list[0];
			if (!ShiftIsHeld)
			{
				ClearSelection();
				Select(obj2);
			}
			else if (!selected.Contains(obj2))
			{
				Select(obj2);
			}
			else
			{
				Deselect(obj2);
			}
		}
		else
		{
			if (list.Count <= 1)
			{
				return;
			}
			object obj3 = list.Where((object obj) => selected.Contains(obj)).FirstOrDefault();
			if (obj3 != null)
			{
				if (ShiftIsHeld)
				{
					foreach (object item in list)
					{
						if (selected.Contains(item))
						{
							Deselect(item);
						}
					}
					return;
				}
				int num = list.IndexOf(obj3) + 1;
				if (num >= list.Count)
				{
					num -= list.Count;
				}
				if (obj3 is Thing thing && thing.def.category == ThingCategory.Item && thing.Spawned && thing.Position.GetItemCount(thing.Map) >= 2 && list.All(delegate(object x)
				{
					if (x is Thing thing2 && thing2.def.category != ThingCategory.Item)
					{
						Building obj4 = thing2 as Building;
						if (obj4 == null)
						{
							return false;
						}
						return obj4.MaxItemsInCell > 1;
					}
					return true;
				}))
				{
					if (obj3 != list[0])
					{
						ClearSelection();
						Select(list[0]);
					}
					else if (list[list.Count - 1] is Building { MaxItemsInCell: >1 })
					{
						ClearSelection();
						Select(list[list.Count - 1]);
					}
					else
					{
						ClearSelection();
						Select(list[num]);
					}
				}
				else
				{
					ClearSelection();
					Select(list[num]);
				}
			}
			else
			{
				if (!ShiftIsHeld)
				{
					ClearSelection();
				}
				Select(list[0]);
			}
		}
	}

	public void SelectNextAt(IntVec3 c, Map map)
	{
		if (SelectedObjects.Count() != 1)
		{
			Log.Error("Cannot select next at with < or > 1 selected.");
			return;
		}
		List<object> list = SelectableObjectsAt(c, map).ToList();
		int num = list.IndexOf(SingleSelectedThing) + 1;
		if (num >= list.Count)
		{
			num -= list.Count;
		}
		ClearSelection();
		Select(list[num]);
	}

	private void SelectAllMatchingObjectUnderMouseOnScreen()
	{
		List<object> list = SelectableObjectsUnderMouse().ToList();
		if (list.Count == 0)
		{
			return;
		}
		Thing clickedThing = list.FirstOrDefault((object o) => o is Pawn && ((Pawn)o).Faction == Faction.OfPlayer && !((Pawn)o).IsPrisoner) as Thing;
		clickedThing = list.FirstOrDefault((object o) => o is Pawn) as Thing;
		if (clickedThing != null && !clickedThing.Spawned)
		{
			clickedThing = null;
		}
		if (clickedThing == null)
		{
			clickedThing = list.FirstOrDefault(delegate(object o)
			{
				if (!(o is Thing thing))
				{
					return false;
				}
				return !thing.def.neverMultiSelect;
			}) as Thing;
		}
		Rect rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
		if (clickedThing == null)
		{
			if (list.FirstOrDefault((object o) => o is Zone && ((Zone)o).IsMultiselectable) == null)
			{
				return;
			}
			{
				foreach (Zone item in ThingSelectionUtility.MultiSelectableZonesInScreenRectDistinct(rect))
				{
					if (!IsSelected(item))
					{
						Select(item);
					}
				}
				return;
			}
		}
		Building edifice = clickedThing.PositionHeld.GetEdifice(clickedThing.MapHeld);
		if (edifice != null && edifice is Building_Storage building_Storage && building_Storage.GetSlotGroup().HeldThings.Contains(clickedThing))
		{
			Deselect(edifice);
		}
		IEnumerable<Thing> enumerable = ThingSelectionUtility.MultiSelectableThingsInScreenRectDistinct(rect);
		Predicate<Thing> predicate = delegate(Thing t)
		{
			Thing innerIfMinified = clickedThing.GetInnerIfMinified();
			if (IsSelected(t) || t.Faction != innerIfMinified.Faction)
			{
				return false;
			}
			if (innerIfMinified is Pawn pawn && t is Pawn pawn2)
			{
				if (pawn2.HostFaction != pawn.HostFaction)
				{
					return false;
				}
				if (pawn2.mutant?.Def != pawn.mutant?.Def)
				{
					return false;
				}
				if (!SelectorUtility.IsEquivalentRace(pawn2, pawn))
				{
					return false;
				}
				return true;
			}
			return t.def == innerIfMinified.def;
		};
		foreach (Thing item2 in (IEnumerable)enumerable)
		{
			if (predicate(item2.GetInnerIfMinified()))
			{
				Select(item2);
			}
		}
	}

	private void MassTakeFirstAutoTakeableOptionOrGoto()
	{
		List<Pawn> selectedPawns = SelectedPawns;
		if (!selectedPawns.Any())
		{
			return;
		}
		Map map = selectedPawns[0].Map;
		if (map == null)
		{
			return;
		}
		IntVec3 intVec = UI.MouseCell();
		if (!intVec.InBounds(map))
		{
			return;
		}
		tmpDraftedGotoPawns.Clear();
		foreach (Pawn item in selectedPawns)
		{
			if (!FloatMenuMakerMap.InvalidPawnForMultiSelectOption(item) && !TakeFirstAutoTakeableOption(item, intVec, suppressAutoTakeableGoto: true) && item.Drafted)
			{
				tmpDraftedGotoPawns.Add(item);
			}
		}
		if (tmpDraftedGotoPawns.Count == 1)
		{
			TakeFirstAutoTakeableOption(tmpDraftedGotoPawns[0], intVec);
		}
		else
		{
			IntVec3 mouseCell = CellFinder.StandableCellNear(intVec, map, 2.9f);
			if (mouseCell.IsValid)
			{
				gotoController.StartInteraction(mouseCell);
				for (int i = 0; i < tmpDraftedGotoPawns.Count; i++)
				{
					gotoController.AddPawn(tmpDraftedGotoPawns[i]);
				}
			}
		}
		tmpDraftedGotoPawns.Clear();
	}

	private static bool TakeFirstAutoTakeableOption(Pawn pawn, IntVec3 dest, bool suppressAutoTakeableGoto = false)
	{
		FloatMenuOption floatMenuOption = null;
		foreach (FloatMenuOption item in FloatMenuMakerMap.ChoicesAtFor(dest.ToVector3Shifted(), pawn, suppressAutoTakeableGoto))
		{
			if (!item.Disabled && item.autoTakeable && (floatMenuOption == null || item.autoTakeablePriority > floatMenuOption.autoTakeablePriority))
			{
				floatMenuOption = item;
			}
		}
		if (floatMenuOption != null)
		{
			floatMenuOption.Chosen(colonistOrdering: true, null);
			return true;
		}
		return false;
	}
}
