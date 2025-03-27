using System.Collections.Generic;
using Verse;

namespace RimWorld;

public sealed class RelationshipRecords : IExposable
{
	private Dictionary<int, RelationshipRecord> records = new Dictionary<int, RelationshipRecord>();

	private static readonly HashSet<int> toRemove = new HashSet<int>();

	private List<int> tmpKeys;

	private List<RelationshipRecord> tmpValues;

	public IReadOnlyDictionary<int, RelationshipRecord> Records => records;

	public RelationshipRecord GetOrCreateRecord(Pawn pawn)
	{
		if (records.TryGetValue(pawn.thingIDNumber, out var value))
		{
			return value;
		}
		return CreateRecord(pawn);
	}

	public RelationshipRecord CreateRecord(Pawn pawn)
	{
		RelationshipRecord relationshipRecord = new RelationshipRecord(pawn.thingIDNumber, pawn.gender, pawn.Name.ToStringShort);
		records.Add(relationshipRecord.ID, relationshipRecord);
		return relationshipRecord;
	}

	public RelationshipRecord GetRecord(int id)
	{
		return records[id];
	}

	public int CleanupUnusedRecords()
	{
		toRemove.Clear();
		foreach (KeyValuePair<int, RelationshipRecord> record in records)
		{
			if (record.Value.References.Count <= 1)
			{
				toRemove.Add(record.Key);
			}
		}
		foreach (int item in toRemove)
		{
			records.Remove(item);
		}
		int count = toRemove.Count;
		toRemove.Clear();
		return count;
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref records, "records", LookMode.Value, LookMode.Deep, ref tmpKeys, ref tmpValues);
	}
}
