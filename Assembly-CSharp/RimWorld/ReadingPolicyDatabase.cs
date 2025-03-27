using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld;

public class ReadingPolicyDatabase : IExposable
{
	private List<ReadingPolicy> readingPolicies = new List<ReadingPolicy>();

	public List<ReadingPolicy> AllReadingPolicies => readingPolicies;

	public ReadingPolicyDatabase()
	{
		GenerateStartingPolicies();
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref readingPolicies, "readingPolicies", LookMode.Deep);
		BackCompatibility.PostExposeData(this);
	}

	public ReadingPolicy DefaultReadingPolicy()
	{
		if (readingPolicies.Count == 0)
		{
			MakeNewReadingPolicy();
		}
		return readingPolicies[0];
	}

	public AcceptanceReport TryDelete(ReadingPolicy policy)
	{
		foreach (Pawn item in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive)
		{
			if (item.reading != null && item.reading.CurrentPolicy == policy)
			{
				return new AcceptanceReport("ReadingPolicyInUse".Translate(item));
			}
		}
		foreach (Pawn item2 in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
		{
			if (item2.reading != null && item2.reading.CurrentPolicy == policy)
			{
				item2.reading.CurrentPolicy = null;
			}
		}
		readingPolicies.Remove(policy);
		return AcceptanceReport.WasAccepted;
	}

	public ReadingPolicy MakeNewReadingPolicy()
	{
		int id = ((!readingPolicies.Any()) ? 1 : (readingPolicies.Max((ReadingPolicy o) => o.id) + 1));
		ReadingPolicy readingPolicy = new ReadingPolicy(id, string.Format("{0} {1}", "ReadingPolicy".Translate(), id.ToString()));
		readingPolicies.Add(readingPolicy);
		return readingPolicy;
	}

	private void GenerateStartingPolicies()
	{
		ReadingPolicy readingPolicy = MakeNewReadingPolicy();
		readingPolicy.label = "AllReadingPolicy".Translate();
		readingPolicy.defFilter.SetAllowAll(null, includeNonStorable: true);
		ReadingPolicy readingPolicy2 = MakeNewReadingPolicy();
		readingPolicy2.label = "TextbookPolicy".Translate();
		readingPolicy2.defFilter.SetDisallowAll();
		readingPolicy2.defFilter.SetAllow(ThingDefOf.TextBook, allow: true);
		ReadingPolicy readingPolicy3 = MakeNewReadingPolicy();
		readingPolicy3.label = "SchematicPolicy".Translate();
		readingPolicy3.defFilter.SetDisallowAll();
		readingPolicy3.defFilter.SetAllow(ThingDefOf.Schematic, allow: true);
		ReadingPolicy readingPolicy4 = MakeNewReadingPolicy();
		readingPolicy4.label = "NovelPolicy".Translate();
		readingPolicy4.defFilter.SetDisallowAll();
		readingPolicy4.defFilter.SetAllow(ThingDefOf.Novel, allow: true);
		if (ModsConfig.AnomalyActive)
		{
			ReadingPolicy readingPolicy5 = MakeNewReadingPolicy();
			readingPolicy5.label = "TomePolicy".Translate();
			readingPolicy5.defFilter.SetDisallowAll();
			readingPolicy5.defFilter.SetAllow(ThingDefOf.Tome, allow: true);
		}
		ReadingPolicy readingPolicy6 = MakeNewReadingPolicy();
		readingPolicy6.label = "NoneReadingPolicy".Translate();
		readingPolicy6.defFilter.SetDisallowAll();
	}
}
