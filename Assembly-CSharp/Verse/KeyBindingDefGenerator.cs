using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Verse;

public static class KeyBindingDefGenerator
{
	public static IEnumerable<KeyBindingCategoryDef> ImpliedKeyBindingCategoryDefs(bool hotReload = false)
	{
		List<KeyBindingCategoryDef> gameUniversalCats = DefDatabase<KeyBindingCategoryDef>.AllDefs.Where((KeyBindingCategoryDef d) => d.isGameUniversal).ToList();
		foreach (DesignationCategoryDef allDef in DefDatabase<DesignationCategoryDef>.AllDefs)
		{
			string defName = "Architect_" + allDef.defName;
			KeyBindingCategoryDef keyBindingCategoryDef = (hotReload ? (DefDatabase<KeyBindingCategoryDef>.GetNamed(defName, errorOnFail: false) ?? new KeyBindingCategoryDef()) : new KeyBindingCategoryDef());
			keyBindingCategoryDef.defName = defName;
			keyBindingCategoryDef.label = allDef.label + " tab";
			keyBindingCategoryDef.description = "Key bindings for the \"" + allDef.LabelCap + "\" section of the Architect menu";
			keyBindingCategoryDef.modContentPack = allDef.modContentPack;
			keyBindingCategoryDef.checkForConflicts.AddRange(gameUniversalCats);
			for (int i = 0; i < gameUniversalCats.Count; i++)
			{
				gameUniversalCats[i].checkForConflicts.Add(keyBindingCategoryDef);
			}
			allDef.bindingCatDef = keyBindingCategoryDef;
			yield return keyBindingCategoryDef;
		}
	}

	public static IEnumerable<KeyBindingDef> ImpliedKeyBindingDefs(bool hotReload = false)
	{
		foreach (MainButtonDef item in DefDatabase<MainButtonDef>.AllDefs.OrderBy((MainButtonDef td) => td.order))
		{
			if (item.defaultHotKey != 0)
			{
				string defName = "MainTab_" + item.defName;
				KeyBindingDef keyBindingDef = (hotReload ? (DefDatabase<KeyBindingDef>.GetNamed(defName, errorOnFail: false) ?? new KeyBindingDef()) : new KeyBindingDef());
				keyBindingDef.label = "Toggle " + item.label + " tab";
				keyBindingDef.defName = defName;
				keyBindingDef.category = KeyBindingCategoryDefOf.MainTabs;
				keyBindingDef.defaultKeyCodeA = item.defaultHotKey;
				keyBindingDef.modContentPack = item.modContentPack;
				item.hotKey = keyBindingDef;
				yield return keyBindingDef;
			}
		}
	}
}
