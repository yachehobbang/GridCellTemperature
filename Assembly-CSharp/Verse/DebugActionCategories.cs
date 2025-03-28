using System.Collections.Generic;

namespace Verse;

public static class DebugActionCategories
{
	public const string Incidents = "Incidents";

	public const string Quests = "Quests";

	public const string QuestsOld = "Quests (old)";

	public const string Translation = "Translation";

	public const string General = "General";

	public const string Sound = "Sound";

	public const string Pawns = "Pawns";

	public const string Spawning = "Spawning";

	public const string Ideoligion = "Ideoligion";

	public const string MapManagement = "Map";

	public const string Autotests = "Autotests";

	public const string Mods = "Mods";

	public const string More = "More debug actions";

	public const string Anomaly = "Anomaly";

	public const string Humanlike = "Humanlike";

	public const string Animal = "Animal";

	public const string Insect = "Insect";

	public const string Mechanoid = "Mechanoid";

	public const string Other = "Other";

	public static readonly Dictionary<string, int> categoryOrders;

	static DebugActionCategories()
	{
		categoryOrders = new Dictionary<string, int>();
		categoryOrders.Add("Incidents", 100);
		categoryOrders.Add("Quests", 200);
		categoryOrders.Add("Quests (old)", 250);
		categoryOrders.Add("Translation", 300);
		categoryOrders.Add("General", 400);
		categoryOrders.Add("Sound", 450);
		categoryOrders.Add("Pawns", 500);
		categoryOrders.Add("Spawning", 600);
		categoryOrders.Add("Ideoligion", 700);
		categoryOrders.Add("Map", 800);
		categoryOrders.Add("Autotests", 900);
		categoryOrders.Add("Mods", 100);
		categoryOrders.Add("More debug actions", 1000);
		categoryOrders.Add("Humanlike", 1100);
		categoryOrders.Add("Animal", 1200);
		categoryOrders.Add("Insect", 1300);
		categoryOrders.Add("Mechanoid", 1400);
		categoryOrders.Add("Other", 1500);
	}

	public static int GetOrderFor(string category)
	{
		if (!category.NullOrEmpty() && categoryOrders.TryGetValue(category, out var value))
		{
			return value;
		}
		return int.MaxValue;
	}
}
