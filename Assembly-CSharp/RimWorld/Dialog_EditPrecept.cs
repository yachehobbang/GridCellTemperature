using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld;

public class Dialog_EditPrecept : Window
{
	private Precept precept;

	private string newPreceptName;

	private string newPreceptNameFemale;

	private float windowHeight = 170f;

	private float attachedRewardDescWidth;

	private float attachedRewardDescHeight;

	private int newTriggerDaysSinceStartOfYear = -1;

	private bool newCanStartAnytime;

	private Quadrum quadrum;

	private int day;

	private StyleCategoryPair selectedStyle;

	private float styleListLastHeight;

	private Vector2 styleListScrollPos;

	private RitualObligationTrigger_Date dateTrigger;

	private static readonly Vector2 ButSize = new Vector2(150f, 38f);

	private static readonly float EditFieldHeight = 30f;

	private const float ApparelRequirementThingHeight = 30f;

	private const float ApparelRequirementBtnHeight = 30f;

	private const float WindowBaseHeight = 170f;

	private const float HeaderHeight = 35f;

	private const float ApparelRequirementNoteHeight = 30f;

	private const float BuildingStyleIconSize = 64f;

	private const int MaxPreceptNameLength = 32;

	private static readonly Regex ValidSymbolRegex = new Regex("^[\\p{L}0-9 '\\-]*$");

	private List<PreceptApparelRequirement> apparelRequirements;

	private RitualAttachableOutcomeEffectDef selectedReward;

	private List<RitualAttachableOutcomeEffectDef> attachableOutcomeEffects;

	private List<RitualAttachableOutcomeEffectDef> attachableUsableOutcomeEffects;

	private static List<StyleCategoryPair> stylesForBuildingTmp = new List<StyleCategoryPair>();

	public override Vector2 InitialSize => new Vector2(700f, windowHeight);

	public Dialog_EditPrecept(Precept precept)
	{
		this.precept = precept;
		absorbInputAroundWindow = true;
		newPreceptName = precept.Label;
		Precept_Ritual ritual;
		if ((ritual = precept as Precept_Ritual) != null)
		{
			dateTrigger = ritual.obligationTriggers.OfType<RitualObligationTrigger_Date>().FirstOrDefault();
			if (dateTrigger != null)
			{
				newTriggerDaysSinceStartOfYear = dateTrigger.triggerDaysSinceStartOfYear;
				RecalculateQuadrumAndDay();
			}
			newCanStartAnytime = ritual.isAnytime;
			attachableOutcomeEffects = DefDatabase<RitualAttachableOutcomeEffectDef>.AllDefs.ToList();
			attachableUsableOutcomeEffects = attachableOutcomeEffects.Where((RitualAttachableOutcomeEffectDef e) => e.CanAttachToRitual(ritual)).ToList();
			selectedReward = ritual.attachableOutcomeEffect;
		}
		if (precept.def.leaderRole)
		{
			newPreceptNameFemale = precept.ideo.leaderTitleFemale;
		}
		apparelRequirements = ((precept.ApparelRequirements != null) ? precept.ApparelRequirements.ToList() : null);
		if (precept is Precept_Building precept_Building)
		{
			selectedStyle = precept_Building.ideo.GetStyleAndCategoryFor(precept_Building.ThingDef);
		}
		UpdateWindowHeight();
	}

	private void UpdateWindowHeight()
	{
		windowHeight = 170f;
		if (precept.def.leaderRole || dateTrigger != null)
		{
			windowHeight += 10f + EditFieldHeight;
		}
		if (apparelRequirements != null)
		{
			windowHeight += 39f;
			windowHeight += 40f;
			windowHeight += 30f;
			if (!apparelRequirements.NullOrEmpty())
			{
				foreach (PreceptApparelRequirement apparelRequirement in apparelRequirements)
				{
					int num = apparelRequirement.requirement.AllRequiredApparel().EnumerableCount();
					windowHeight += (float)num * 30f;
				}
				windowHeight += (float)apparelRequirements.Count * 10f;
			}
		}
		if (precept is Precept_Ritual)
		{
			windowHeight += 10f + EditFieldHeight;
			windowHeight += 10f + EditFieldHeight;
			windowHeight += 10f + EditFieldHeight;
			if (selectedReward != null)
			{
				float x = Text.CalcSize(selectedReward.LabelCap).x;
				attachedRewardDescWidth = x + ButSize.x + 4f;
				attachedRewardDescHeight = Text.CalcHeight(selectedReward.effectDesc, attachedRewardDescWidth);
				windowHeight += attachedRewardDescHeight;
			}
		}
		if (precept is Precept_Building building && StylesForBuilding(building).Count > 1)
		{
			int val = NumStyleRows(building);
			windowHeight += (float)Math.Min(val, 2) * 74f + 14f;
		}
		if (precept is Precept_Relic precept_Relic && precept_Relic.ThingDef.MadeFromStuff)
		{
			windowHeight += 10f + EditFieldHeight;
		}
		SetInitialSizeAndPosition();
	}

	private int NumStyleRows(Precept_Building building)
	{
		int num = (int)(InitialSize.x - Margin * 2f - InitialSize.x / 3f) / 68;
		return StylesForBuilding(building).Count / num + 1;
	}

	public override void OnAcceptKeyPressed()
	{
		ApplyChanges();
		Event.current.Use();
	}

	public override void DoWindowContents(Rect rect)
	{
		float num = rect.x + rect.width / 3f;
		float num2 = rect.xMax - num;
		Text.Font = GameFont.Medium;
		Widgets.Label(new Rect(rect.x, rect.y, rect.width, 35f), "EditName".Translate());
		Text.Font = GameFont.Small;
		float num3 = rect.y + 35f + 10f;
		if (precept.def.leaderRole)
		{
			Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "LeaderTitle".Translate() + " (" + Gender.Male.GetLabel() + ")");
			newPreceptName = Widgets.TextField(new Rect(num, num3, num2, EditFieldHeight), newPreceptName, 99999, ValidSymbolRegex);
			num3 += EditFieldHeight + 10f;
			if (newPreceptName.Length > 32)
			{
				Messages.Message("PreceptNameTooLong".Translate(32), null, MessageTypeDefOf.RejectInput, historical: false);
				newPreceptName = newPreceptName.Substring(0, 32);
			}
			Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "LeaderTitle".Translate() + " (" + Gender.Female.GetLabel() + ")");
			newPreceptNameFemale = Widgets.TextField(new Rect(num, num3, num2, EditFieldHeight), newPreceptNameFemale, 99999, ValidSymbolRegex);
			num3 += EditFieldHeight + 10f;
			if (newPreceptNameFemale.Length > 32)
			{
				Messages.Message("PreceptNameTooLong".Translate(32), null, MessageTypeDefOf.RejectInput, historical: false);
				newPreceptNameFemale = newPreceptNameFemale.Substring(0, 32);
			}
		}
		else
		{
			Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "Name".Translate());
			newPreceptName = Widgets.TextField(new Rect(num, num3, num2 - EditFieldHeight - 4f, EditFieldHeight), newPreceptName, 99999, ValidSymbolRegex);
			if (newPreceptName.Length > 32)
			{
				Messages.Message("PreceptNameTooLong".Translate(32), null, MessageTypeDefOf.RejectInput, historical: false);
				newPreceptName = newPreceptName.Substring(0, 32);
			}
			Rect rect2 = new Rect(rect.xMax - 35f, num3, EditFieldHeight, EditFieldHeight);
			if (Widgets.ButtonImage(rect2, precept.nameLocked ? IdeoUIUtility.LockedTex : IdeoUIUtility.UnlockedTex))
			{
				precept.nameLocked = !precept.nameLocked;
				if (precept.nameLocked)
				{
					SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
				}
				else
				{
					SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
				}
			}
			GUI.color = Color.white;
			if (Mouse.IsOver(rect2))
			{
				TaggedString taggedString = "LockPreceptNameButtonDesc".Translate() + "\n\n" + (precept.nameLocked ? "LockInOn" : "LockInOff").Translate("PreceptName".Translate(), "PreceptNameLower".Translate());
				TooltipHandler.TipRegion(rect2, taggedString);
			}
			num3 += EditFieldHeight + 10f;
			Precept_Ritual ritual = precept as Precept_Ritual;
			if (ritual != null)
			{
				if (ritual.canBeAnytime && !ritual.sourcePattern.alwaysStartAnytime)
				{
					Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "StartingCondition".Translate());
					bool flag = newCanStartAnytime;
					if (Widgets.RadioButton(num, num3, flag))
					{
						flag = true;
					}
					Widgets.Label(new Rect(num + 10f + 24f, num3, num2, EditFieldHeight), "StartingCondition_Anytime".Translate());
					num3 += EditFieldHeight + 10f;
					if (Widgets.RadioButton(num, num3, !flag))
					{
						flag = false;
					}
					Widgets.Label(new Rect(num + 10f + 24f, num3, num2, EditFieldHeight), "StartingCondition_Date".Translate());
					num3 += EditFieldHeight + 10f;
					if (flag != newCanStartAnytime)
					{
						newCanStartAnytime = flag;
						UpdateWindowHeight();
					}
					if (dateTrigger != null && !newCanStartAnytime)
					{
						Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "Date".Translate());
						if (Widgets.ButtonText(new Rect(num, num3, num2 / 4f, EditFieldHeight), Find.ActiveLanguageWorker.OrdinalNumber(day + 1), drawBackground: true, doMouseoverSound: true, active: true, null))
						{
							List<FloatMenuOption> list = new List<FloatMenuOption>();
							for (int i = 0; i < 15; i++)
							{
								int d = i;
								list.Add(new FloatMenuOption(Find.ActiveLanguageWorker.OrdinalNumber(d + 1), delegate
								{
									day = d;
									newTriggerDaysSinceStartOfYear = (int)quadrum * 15 + day;
								}));
							}
							Find.WindowStack.Add(new FloatMenu(list));
						}
						if (Widgets.ButtonText(new Rect(num + num2 / 4f + 10f, num3, num2 / 4f, EditFieldHeight), quadrum.Label(), drawBackground: true, doMouseoverSound: true, active: true, null))
						{
							List<FloatMenuOption> list2 = new List<FloatMenuOption>();
							foreach (Quadrum q in QuadrumUtility.QuadrumsInChronologicalOrder)
							{
								list2.Add(new FloatMenuOption(q.Label(), delegate
								{
									quadrum = q;
									newTriggerDaysSinceStartOfYear = (int)quadrum * 15 + day;
								}));
							}
							Find.WindowStack.Add(new FloatMenu(list2));
						}
						num3 += EditFieldHeight + 10f;
					}
				}
				if (ritual.SupportsAttachableOutcomeEffect)
				{
					Widgets.Label(new Rect(rect.x, num3, num2 / 2f, EditFieldHeight), "RitualAttachedReward".Translate());
					if (selectedReward != null)
					{
						GUI.color = Color.gray;
						string text = selectedReward.AppliesToOutcomesString(ritual.outcomeEffect.def);
						Widgets.Label(new Rect(rect.x, num3 + EditFieldHeight, num2 / 2f, EditFieldHeight * 2f), (selectedReward.AppliesToSeveralOutcomes(ritual) ? "RitualAttachedApplliesForOutcomes" : "RitualAttachedApplliesForOutcome").Translate(text));
						Widgets.Label(new Rect(num, num3 + EditFieldHeight, attachedRewardDescWidth, attachedRewardDescHeight), selectedReward.effectDesc.CapitalizeFirst());
						GUI.color = Color.white;
					}
					TaggedString taggedString2 = ((selectedReward != null) ? selectedReward.LabelCap : "None".Translate());
					Widgets.Label(new Rect(num, num3, num2, EditFieldHeight), taggedString2);
					float num4 = num + Text.CalcSize(taggedString2).x + 10f;
					if (selectedReward != null)
					{
						TooltipHandler.TipRegion(new Rect(rect.x, num3, num4 - rect.x, EditFieldHeight), new TipSignal(() => selectedReward.TooltipForRitual(ritual), ritual.Id));
					}
					if (Widgets.ButtonText(new Rect(num4, num3 - 4f, num2 / 4f, EditFieldHeight), "RitualAttachedRewardChoose".Translate() + "...", drawBackground: true, doMouseoverSound: true, attachableOutcomeEffects.Any(), null))
					{
						List<FloatMenuOption> list3 = new List<FloatMenuOption>();
						if (selectedReward != null)
						{
							list3.Add(new FloatMenuOption("None".Translate(), delegate
							{
								selectedReward = null;
								UpdateWindowHeight();
							}));
						}
						foreach (RitualAttachableOutcomeEffectDef attachableOutcomeEffect in attachableOutcomeEffects)
						{
							RitualAttachableOutcomeEffectDef eff = attachableOutcomeEffect;
							if (eff == selectedReward)
							{
								continue;
							}
							AcceptanceReport acceptanceReport = eff.CanAttachToRitual(ritual);
							if ((bool)acceptanceReport)
							{
								list3.Add(new FloatMenuOption(eff.LabelCap, delegate
								{
									selectedReward = eff;
									UpdateWindowHeight();
								}, MenuOptionPriority.Default, DrawTooltip));
							}
							else
							{
								list3.Add(new FloatMenuOption(eff.LabelCap + " (" + acceptanceReport.Reason + ")", null));
							}
							void DrawTooltip(Rect r)
							{
								TooltipHandler.TipRegion(r, new TipSignal(() => eff.TooltipForRitual(ritual), ritual.Id));
							}
						}
						if (list3.Any())
						{
							Find.WindowStack.Add(new FloatMenu(list3));
						}
					}
				}
			}
		}
		if (apparelRequirements != null)
		{
			string text2 = "EditApparelRequirement".Translate();
			Text.Font = GameFont.Medium;
			Vector2 vector = Text.CalcSize(text2);
			Widgets.Label(new Rect(rect.x, num3, rect.width, 35f), text2);
			Text.Font = GameFont.Small;
			num3 += 39f;
			text2 = "NoteRoleApparelRequirementEffects".Translate();
			Rect rect3 = new Rect(rect.x, num3, rect.width, 30f);
			Color color = GUI.color;
			GUI.color = Color.gray;
			Widgets.Label(rect3, text2);
			GUI.color = color;
			num3 += 40f;
			int num5 = -1;
			for (int j = 0; j < apparelRequirements.Count; j++)
			{
				PreceptApparelRequirement preceptApparelRequirement = apparelRequirements[j];
				Rect rect4 = new Rect(rect.x + rect.width - 100f, num3, 100f, 30f);
				if (Widgets.ButtonText(rect4, "Remove".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
				{
					num5 = j;
				}
				if (preceptApparelRequirement.RequirementOverlapsOther(apparelRequirements, out var compatibilityReason))
				{
					compatibilityReason = compatibilityReason.CapitalizeFirst().Colorize(ColorLibrary.Yellow);
				}
				foreach (ThingDef item in preceptApparelRequirement.requirement.AllRequiredApparel())
				{
					Rect rect5 = new Rect(rect.x + 26f + 10f + 24f, num3 + 3f, 24f, 30f);
					Rect rect6 = new Rect(rect5.x + 32f, num3, compatibilityReason.NullOrEmpty() ? (rect.width - rect5.width - rect4.width - 32f) : 100f, 30f);
					text2 = item.LabelCap;
					vector = Text.CalcSize(text2);
					rect6.y += (rect6.height - vector.y) / 2f;
					Widgets.Label(rect6, text2.Truncate(rect6.width));
					Widgets.DefIcon(rect5, item, null, 1f, null, drawPlaceholder: false, null, null, null);
					if (!compatibilityReason.NullOrEmpty())
					{
						Widgets.Label(new Rect(rect6.xMax + 4f, rect6.y, rect.width - rect6.width - rect5.width - rect4.width, 30f), compatibilityReason);
					}
					num3 += rect6.height;
				}
				num3 += 10f;
			}
			if (num5 != -1)
			{
				apparelRequirements.RemoveAt(num5);
				SoundDefOf.Tick_Low.PlayOneShotOnCamera();
				UpdateWindowHeight();
			}
			if (Widgets.ButtonText(new Rect(rect.x + 26f, num3, rect.width - 26f, 30f), "Add".Translate().CapitalizeFirst() + "...", drawBackground: true, doMouseoverSound: true, active: true, null))
			{
				bool flag2 = false;
				foreach (MemeDef meme in precept.ideo.memes)
				{
					if (meme.preventApparelRequirements)
					{
						flag2 = true;
						Messages.Message("CannotNotAddRoleApparelDueToMeme".Translate(meme.LabelCap.Named("MEME")), MessageTypeDefOf.RejectInput, historical: false);
						break;
					}
				}
				if (!flag2)
				{
					List<FloatMenuOption> list4 = new List<FloatMenuOption>();
					foreach (PreceptApparelRequirement item2 in Precept_Role.AllPossibleRequirements(precept.ideo, precept.def, desperate: true))
					{
						PreceptApparelRequirement localReq = item2;
						List<ThingDef> list5 = item2.requirement.AllRequiredApparel().ToList();
						if (list5.Count > 0)
						{
							FloatMenuOption floatMenuOption = new FloatMenuOption(string.Join(", ", list5.Select((ThingDef ap) => ap.LabelCap)), delegate
							{
								apparelRequirements.Add(localReq);
								UpdateWindowHeight();
							}, list5[0].uiIcon, list5[0].uiIconColor);
							floatMenuOption.Disabled = !item2.CanAddRequirement(precept, apparelRequirements, out var cannotAddReason);
							if (floatMenuOption.Disabled)
							{
								floatMenuOption.Label = floatMenuOption.Label + " (" + cannotAddReason + ")";
							}
							list4.Add(floatMenuOption);
						}
					}
					Find.WindowStack.Add(new FloatMenu(list4));
				}
			}
		}
		Precept_Building building;
		if ((building = precept as Precept_Building) != null && StylesForBuilding(building).Count > 1)
		{
			_ = building.ThingDef;
			Rect rect7 = new Rect(rect);
			rect7.y += num3;
			float num6 = 8f;
			Widgets.Label(rect7, "Appearance".Translate() + ":");
			List<StyleCategoryPair> elements = StylesForBuilding(building);
			int val = NumStyleRows(building);
			Rect rect8 = default(Rect);
			rect8.x = rect.x + InitialSize.x / 3f;
			rect8.y = rect.y + num3;
			rect8.width = rect.width - InitialSize.x / 3f;
			rect8.height = (float)Math.Min(val, 2) * 68f + num6;
			Rect outRect = rect8;
			Widgets.BeginScrollView(outRect, ref styleListScrollPos, new Rect(0f, 0f, outRect.width - 16f, styleListLastHeight + num6));
			styleListLastHeight = GenUI.DrawElementStack(new Rect(num6, num6, outRect.width - num6, 99999f), 64f, elements, delegate(Rect r, StyleCategoryPair obj)
			{
				if (selectedStyle != null && selectedStyle.styleDef == obj.styleDef)
				{
					Widgets.DrawBoxSolid(r, new Color(1f, 0.8f, 0.2f, 0.2f));
				}
				if (Mouse.IsOver(r))
				{
					if (Widgets.ButtonInvisible(r))
					{
						selectedStyle = obj;
						SoundDefOf.Tick_High.PlayOneShotOnCamera();
					}
					TooltipHandler.TipRegion(r, "Origin".Translate() + " " + obj.category.LabelCap + ".");
					Widgets.DrawHighlight(r);
				}
				Widgets.DefIcon(r, building.ThingDef, GenStuff.DefaultStuffFor(building.ThingDef), 1f, obj.styleDef, drawPlaceholder: false, null, null, null);
			}, (StyleCategoryPair obj) => 64f, 6f).height;
			Widgets.EndScrollView();
		}
		Precept_Relic relic;
		if ((relic = precept as Precept_Relic) != null && relic.ThingDef.MadeFromStuff)
		{
			Widgets.Label(new Rect(rect.x, num3, num2, EditFieldHeight), "RelicStuff".Translate() + ":");
			Vector2 vector2 = Text.CalcSize(relic.stuff.LabelCap);
			Text.Anchor = TextAnchor.MiddleCenter;
			Rect rect9 = new Rect(num, num3, vector2.x, EditFieldHeight);
			Widgets.Label(rect9, relic.stuff.LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;
			Rect rect10 = new Rect(rect9.xMax + 4f, num3, EditFieldHeight, EditFieldHeight);
			Widgets.DefIcon(rect10, relic.stuff, null, 1f, null, drawPlaceholder: false, null, null, null);
			TaggedString taggedString3 = "ChooseStuffForRelic".Translate() + "...";
			Vector2 vector3 = Text.CalcSize(taggedString3);
			if (Widgets.ButtonText(new Rect(rect10.xMax + 4f, num3, vector3.x + 20f, EditFieldHeight), taggedString3, drawBackground: true, doMouseoverSound: true, active: true, null))
			{
				IEnumerable<ThingDef> enumerable = GenStuff.AllowedStuffsFor(relic.ThingDef);
				if (enumerable.Count() > 0)
				{
					List<FloatMenuOption> list6 = new List<FloatMenuOption>();
					foreach (ThingDef item3 in enumerable)
					{
						ThingDef localStuff = item3;
						list6.Add(new FloatMenuOption(item3.LabelCap, delegate
						{
							relic.stuff = localStuff;
						}, item3, null, forceBasicStyle: false, MenuOptionPriority.Default, null, null, 0f, null, null, playSelectionSound: true, 0, null));
					}
					Find.WindowStack.Add(new FloatMenu(list6));
				}
			}
			num3 += EditFieldHeight + 10f;
		}
		if (Widgets.ButtonText(new Rect(0f, rect.height - ButSize.y, ButSize.x, ButSize.y), "Back".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			Close();
		}
		if (Widgets.ButtonText(new Rect(rect.width / 2f - ButSize.x / 2f, rect.height - ButSize.y, ButSize.x, ButSize.y), "Randomize".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			newPreceptName = (newPreceptNameFemale = precept.GenerateNewName());
			apparelRequirements = precept.GenerateNewApparelRequirements();
			if (dateTrigger != null)
			{
				newTriggerDaysSinceStartOfYear = dateTrigger.RandomDate();
				RecalculateQuadrumAndDay();
			}
			if (precept is Precept_Ritual precept_Ritual)
			{
				if (precept_Ritual.canBeAnytime && !precept_Ritual.sourcePattern.alwaysStartAnytime)
				{
					newCanStartAnytime = Rand.Bool;
				}
				if (attachableUsableOutcomeEffects.Any() && precept_Ritual.SupportsAttachableOutcomeEffect)
				{
					selectedReward = attachableUsableOutcomeEffects.RandomElement();
				}
			}
			if (precept is Precept_Building building2)
			{
				selectedStyle = StylesForBuilding(building2).RandomElement();
			}
			if (precept is Precept_Relic precept_Relic)
			{
				precept_Relic.SetRandomStuff();
			}
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
			UpdateWindowHeight();
		}
		if (Widgets.ButtonText(new Rect(rect.width - ButSize.x, rect.height - ButSize.y, ButSize.x, ButSize.y), "DoneButton".Translate(), drawBackground: true, doMouseoverSound: true, active: true, null))
		{
			ApplyChanges();
		}
	}

	private void RecalculateQuadrumAndDay()
	{
		Map map = ((Current.ProgramState == ProgramState.Playing) ? Find.AnyPlayerHomeMap : null);
		float longitude = ((map != null) ? Find.WorldGrid.LongLatOf(map.Tile).x : 0f);
		quadrum = GenDate.Quadrum(newTriggerDaysSinceStartOfYear * 60000, longitude);
		day = GenDate.DayOfQuadrum(newTriggerDaysSinceStartOfYear * 60000, longitude);
	}

	private void ApplyChanges()
	{
		if (apparelRequirements != null && apparelRequirements.Any((PreceptApparelRequirement x) => x.RequirementOverlapsOther(apparelRequirements, out var _)))
		{
			Messages.Message("OverlappingRoleApparel".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			return;
		}
		precept.SetName(newPreceptName);
		if (precept.def.leaderRole)
		{
			precept.ideo.leaderTitleMale = newPreceptName;
			precept.ideo.leaderTitleFemale = newPreceptNameFemale;
		}
		if (precept is Precept_Ritual precept_Ritual)
		{
			precept_Ritual.isAnytime = newCanStartAnytime;
			precept_Ritual.attachableOutcomeEffect = selectedReward;
		}
		if (dateTrigger != null && newTriggerDaysSinceStartOfYear != dateTrigger.triggerDaysSinceStartOfYear)
		{
			dateTrigger.triggerDaysSinceStartOfYear = newTriggerDaysSinceStartOfYear;
		}
		foreach (Precept item in precept.ideo.PreceptsListForReading)
		{
			item.ClearTipCache();
		}
		if (apparelRequirements != null)
		{
			precept.ApparelRequirements = apparelRequirements;
		}
		if (precept is Precept_Building precept_Building && selectedStyle != null)
		{
			precept.ideo.style.SetStyleForThingDef(precept_Building.ThingDef, selectedStyle);
		}
		Close();
	}

	private List<StyleCategoryPair> StylesForBuilding(Precept_Building building)
	{
		ThingDef thingDef = building.ThingDef;
		if (thingDef != null && thingDef.canEditAnyStyle)
		{
			return Precept_ThingDef.AllPossibleStylesForBuilding(building.ThingDef);
		}
		stylesForBuildingTmp.Clear();
		foreach (ThingStyleCategoryWithPriority thingStyleCategory in precept.ideo.thingStyleCategories)
		{
			foreach (ThingDefStyle thingDefStyle in thingStyleCategory.category.thingDefStyles)
			{
				if (thingDefStyle.ThingDef == building.ThingDef)
				{
					stylesForBuildingTmp.Add(new StyleCategoryPair
					{
						category = thingStyleCategory.category,
						styleDef = thingDefStyle.StyleDef
					});
				}
			}
		}
		return stylesForBuildingTmp;
	}
}
