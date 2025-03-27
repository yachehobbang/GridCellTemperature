using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorld;

public class Dialog_MapSearch : Window
{
	private QuickSearchWidget quickSearchWidget;

	private SortedList<string, Thing> searchResults;

	private HashSet<Thing> searchResultsSet;

	private Thing highlightedThing;

	private Vector2 scrollPos;

	private float scrollHeight;

	private Map map;

	private List<Thing> allThings;

	private int searchIndex;

	private bool triedToFocus;

	private string[] searchingTexts;

	private const float ElementHeight = 26f;

	private List<Thing> tmpContents = new List<Thing>();

	public override Vector2 InitialSize => new Vector2(350f, 100f);

	private bool Searching
	{
		get
		{
			if (!quickSearchWidget.filter.Text.NullOrEmpty() && allThings.Any())
			{
				return searchIndex < allThings.Count;
			}
			return false;
		}
	}

	public override QuickSearchWidget CommonSearchWidget => quickSearchWidget;

	private int SearchingTextIndex => Time.frameCount / 20 % searchingTexts.Length;

	protected override Rect QuickSearchWidgetRect(Rect winRect, Rect inRect)
	{
		return new Rect(inRect.x, inRect.yMax - 24f, inRect.width, 24f);
	}

	public Dialog_MapSearch()
	{
		map = Find.CurrentMap;
		doCloseX = true;
		closeOnAccept = false;
		preventCameraMotion = false;
		quickSearchWidget = new QuickSearchWidget
		{
			maxSearchTextLength = 26
		};
		searchResults = new SortedList<string, Thing>(new DuplicateKeyComparer<string>());
		searchResultsSet = new HashSet<Thing>();
		allThings = new List<Thing>();
		string text = "Searching".Translate();
		searchingTexts = new string[3]
		{
			text + ".",
			text + "..",
			text + "..."
		};
	}

	public override void PostOpen()
	{
		base.PostOpen();
		ThingListChangedCallbacks thingListChangedCallbacks = map.thingListChangedCallbacks;
		thingListChangedCallbacks.onThingAdded = (Action<Thing>)Delegate.Combine(thingListChangedCallbacks.onThingAdded, new Action<Thing>(TryAddThingFromMap));
		ThingListChangedCallbacks thingListChangedCallbacks2 = map.thingListChangedCallbacks;
		thingListChangedCallbacks2.onThingRemoved = (Action<Thing>)Delegate.Combine(thingListChangedCallbacks2.onThingRemoved, new Action<Thing>(ThingRemovedFromMap));
	}

	public override void PostClose()
	{
		base.PostClose();
		ThingListChangedCallbacks thingListChangedCallbacks = map.thingListChangedCallbacks;
		thingListChangedCallbacks.onThingAdded = (Action<Thing>)Delegate.Remove(thingListChangedCallbacks.onThingAdded, new Action<Thing>(TryAddThingFromMap));
		ThingListChangedCallbacks thingListChangedCallbacks2 = map.thingListChangedCallbacks;
		thingListChangedCallbacks2.onThingRemoved = (Action<Thing>)Delegate.Remove(thingListChangedCallbacks2.onThingRemoved, new Action<Thing>(ThingRemovedFromMap));
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		highlightedThing = null;
		float num = Text.CalcHeight("SearchTheMapDesc".Translate(), inRect.width);
		Rect rect = new Rect(0f, inRect.yMax - 24f - num, inRect.width, num);
		using (new TextBlock(ColoredText.SubtleGrayColor, TextAnchor.MiddleLeft, newWordWrap: true))
		{
			Widgets.Label(label: Searching ? searchingTexts[SearchingTextIndex] : ((quickSearchWidget.filter.Text.Length <= 0) ? ((string)"SearchTheMap".Translate()) : ((string)((searchResults.Count == 1) ? "MapSearchResultSingular".Translate() : "MapSearchResults".Translate(searchResults.Count)))), rect: rect);
		}
		if (searchResults.Count > 0)
		{
			Rect outRect = new Rect(0f, 0f, inRect.width, inRect.height);
			outRect.yMax = rect.yMin;
			Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, scrollHeight);
			Rect rect2 = new Rect(0f, scrollPos.y, outRect.width, outRect.height);
			Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
			using (new ProfilerBlock("DrawSearchResults"))
			{
				for (int i = 0; i < searchResults.Count; i++)
				{
					Rect rect3 = new Rect(0f, 26f * (float)i, inRect.width, 26f);
					if (!rect2.Overlaps(rect3))
					{
						continue;
					}
					if (i % 2 == 1)
					{
						Widgets.DrawLightHighlight(rect3);
					}
					Thing thing = searchResults.Values[i];
					if (thing != null && !(thing is Corpse { Bugged: not false }) && !(thing is MinifiedThing { InnerThing: null }))
					{
						Rect rect4 = rect3;
						rect4.xMax = 26f;
						Widgets.ThingIcon(rect4, thing, 1f, null);
						Rect rect5 = rect3;
						rect5.xMin += rect4.width + 4f;
						Widgets.Label(rect5, thing.LabelCap);
						if (Mouse.IsOver(rect3))
						{
							Widgets.DrawHighlight(rect3);
							highlightedThing = thing;
						}
						if (Widgets.ButtonInvisible(rect3))
						{
							JumpAndSelect(thing);
						}
					}
				}
			}
			Widgets.EndScrollView();
		}
		if (!triedToFocus)
		{
			quickSearchWidget.Focus();
			triedToFocus = true;
		}
	}

	private void GetSearchResults()
	{
		scrollPos = Vector2.zero;
		searchIndex = 0;
		searchResults.Clear();
		searchResultsSet.Clear();
		allThings.Clear();
		allThings.AddRange(map.listerThings.AllThings);
		SetInitialSizeAndPosition();
	}

	public override void WindowUpdate()
	{
		base.WindowUpdate();
		if (Find.CurrentMap != map || WorldRendererUtility.WorldRenderedNow)
		{
			Close();
			return;
		}
		if (highlightedThing != null)
		{
			GenDraw.DrawArrowPointingAt(highlightedThing.PositionHeld.ToVector3Shifted());
		}
		if (Searching)
		{
			using (new ProfilerBlock("Searching"))
			{
				for (int i = 0; i < 500; i++)
				{
					searchIndex++;
					if (searchIndex >= allThings.Count)
					{
						allThings.Clear();
						break;
					}
					TryAddThingFromMap(allThings[searchIndex]);
				}
				return;
			}
		}
		if (Time.frameCount % 20 != 0)
		{
			return;
		}
		bool flag = false;
		for (int num = searchResults.Count - 1; num >= 0; num--)
		{
			Thing thing = searchResults.Values[num];
			if (thing == null || thing.Destroyed || thing.MapHeld != map || (thing is Pawn pawn && pawn.IsHiddenFromPlayer()))
			{
				searchResults.RemoveAt(num);
				searchResultsSet.Remove(thing);
				flag = true;
			}
		}
		if (flag)
		{
			SetInitialSizeAndPosition();
		}
	}

	public override void Notify_CommonSearchChanged()
	{
		GetSearchResults();
	}

	private void JumpAndSelect(Thing thing)
	{
		if (thing != null && !thing.Destroyed && thing.MapHeld == map)
		{
			CameraJumper.TryJump(thing);
			Find.Selector.ClearSelection();
			Find.Selector.Select(thing);
		}
	}

	protected override void SetInitialSizeAndPosition()
	{
		scrollHeight = (float)searchResults.Count * 26f;
		Vector2 initialSize = InitialSize;
		initialSize.y = Mathf.Clamp(initialSize.y + scrollHeight, InitialSize.y, (float)UI.screenHeight / 2f);
		windowRect = new Rect((float)UI.screenWidth - initialSize.x, (float)UI.screenHeight - initialSize.y - 35f, initialSize.x, initialSize.y).Rounded();
	}

	private bool CanAddThing(Thing thing)
	{
		if (!quickSearchWidget.filter.Text.NullOrEmpty() && thing != null && thing.def.selectable && !thing.Destroyed && thing.def.showInSearch && thing.MapHeld == map && (DebugSettings.searchIgnoresRestrictions || !thing.PositionHeld.Fogged(thing.MapHeld)) && !searchResultsSet.Contains(thing) && !(thing is Corpse { Bugged: not false }) && thing.LabelNoCount.IndexOf(quickSearchWidget.filter.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
		{
			if (!DebugSettings.searchIgnoresRestrictions)
			{
				if (thing is Pawn pawn)
				{
					return !pawn.IsHiddenFromPlayer();
				}
				return true;
			}
			return true;
		}
		return false;
	}

	private void TryAddThingFromMap(Thing thing)
	{
		bool flag = false;
		foreach (Thing item in ContentsFromThing(thing))
		{
			searchResults.Add(item.LabelNoParenthesis.ToLower(), item);
			searchResultsSet.Add(item);
			flag = true;
		}
		if (flag)
		{
			SetInitialSizeAndPosition();
		}
	}

	private void ThingRemovedFromMap(Thing thing)
	{
		bool flag = false;
		foreach (Thing item in ContentsFromThing(thing))
		{
			int num = searchResults.IndexOfValue(item);
			if (num >= 0)
			{
				searchResults.RemoveAt(num);
				searchResultsSet.Remove(item);
				flag = true;
			}
		}
		if (flag)
		{
			SetInitialSizeAndPosition();
		}
	}

	private List<Thing> ContentsFromThing(Thing thing)
	{
		tmpContents.Clear();
		if (CanAddThing(thing))
		{
			tmpContents.Add(thing);
		}
		if (!thing.Faction.IsPlayerSafe())
		{
			return tmpContents;
		}
		if (thing is ISearchableContents { SearchableContents: { } searchableContents2 })
		{
			foreach (Thing item in (IEnumerable<Thing>)searchableContents2)
			{
				if (CanAddThing(item))
				{
					tmpContents.Add(item);
				}
			}
		}
		if (thing is ThingWithComps thingWithComps)
		{
			foreach (ThingComp allComp in thingWithComps.AllComps)
			{
				if (!(allComp is ISearchableContents { SearchableContents: { } searchableContents4 }))
				{
					continue;
				}
				foreach (Thing item2 in (IEnumerable<Thing>)searchableContents4)
				{
					if (CanAddThing(item2))
					{
						tmpContents.Add(item2);
					}
				}
			}
		}
		return tmpContents;
	}
}
