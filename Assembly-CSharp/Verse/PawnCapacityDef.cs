using System;

namespace Verse;

public class PawnCapacityDef : Def
{
	public int listOrder;

	public Type workerClass = typeof(PawnCapacityWorker);

	[MustTranslate]
	public string labelMechanoids = "";

	[MustTranslate]
	public string labelAnimals = "";

	[MustTranslate]
	public string labelAnomalyEntity = "";

	public bool showOnHumanlikes = true;

	public bool showOnAnimals = true;

	public bool showOnMechanoids = true;

	public bool showOnAnomalyEntities = true;

	public bool lethalFlesh;

	public bool lethalMechanoids;

	public float minForCapable;

	public float minValue;

	public bool zeroIfCannotBeAwake;

	public bool showOnCaravanHealthTab;

	[Unsaved(false)]
	private PawnCapacityWorker workerInt;

	public PawnCapacityWorker Worker
	{
		get
		{
			if (workerInt == null)
			{
				workerInt = (PawnCapacityWorker)Activator.CreateInstance(workerClass);
			}
			return workerInt;
		}
	}

	public string GetLabelFor(Pawn pawn)
	{
		if (pawn.RaceProps.Humanlike)
		{
			return label;
		}
		if (pawn.RaceProps.IsFlesh && !labelAnimals.NullOrEmpty())
		{
			return labelAnimals;
		}
		if (pawn.RaceProps.IsAnomalyEntity && !labelAnomalyEntity.NullOrEmpty())
		{
			return labelAnomalyEntity;
		}
		if (pawn.RaceProps.IsMechanoid && !labelMechanoids.NullOrEmpty())
		{
			return labelMechanoids;
		}
		return label;
	}

	public string GetLabelFor()
	{
		return label;
	}

	public bool CanShowOnPawn(Pawn p)
	{
		if (p.def.race.Humanlike)
		{
			return showOnHumanlikes;
		}
		if (p.def.race.Animal)
		{
			return showOnAnimals;
		}
		if (p.def.race.IsAnomalyEntity)
		{
			return showOnAnomalyEntities;
		}
		return showOnMechanoids;
	}
}
