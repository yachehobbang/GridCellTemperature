using System;

namespace Verse;

public struct TraverseParms : IEquatable<TraverseParms>
{
	public Pawn pawn;

	public TraverseMode mode;

	public Danger maxDanger;

	public bool canBashDoors;

	public bool canBashFences;

	public bool alwaysUseAvoidGrid;

	public bool fenceBlocked;

	public static TraverseParms For(Pawn pawn, Danger maxDanger = Danger.Deadly, TraverseMode mode = TraverseMode.ByPawn, bool canBashDoors = false, bool alwaysUseAvoidGrid = false, bool canBashFences = false)
	{
		if (pawn == null)
		{
			Log.Error("TraverseParms for null pawn.");
			return For(TraverseMode.NoPassClosedDoors, maxDanger, canBashDoors, alwaysUseAvoidGrid, canBashFences);
		}
		TraverseParms result = default(TraverseParms);
		result.pawn = pawn;
		result.maxDanger = maxDanger;
		result.mode = mode;
		result.canBashDoors = canBashDoors;
		result.canBashFences = canBashFences;
		result.alwaysUseAvoidGrid = alwaysUseAvoidGrid;
		result.fenceBlocked = pawn.ShouldAvoidFences;
		return result;
	}

	public static TraverseParms For(TraverseMode mode, Danger maxDanger = Danger.Deadly, bool canBashDoors = false, bool alwaysUseAvoidGrid = false, bool canBashFences = false)
	{
		TraverseParms result = default(TraverseParms);
		result.pawn = null;
		result.mode = mode;
		result.maxDanger = maxDanger;
		result.canBashDoors = canBashDoors;
		result.canBashFences = canBashFences;
		result.alwaysUseAvoidGrid = alwaysUseAvoidGrid;
		result.fenceBlocked = false;
		return result;
	}

	public TraverseParms WithFenceblockedOf(Pawn otherPawn)
	{
		return WithFenceblocked(otherPawn.ShouldAvoidFences);
	}

	public TraverseParms WithFenceblocked(bool forceFenceblocked)
	{
		TraverseParms result = default(TraverseParms);
		result.pawn = pawn;
		result.mode = mode;
		result.maxDanger = maxDanger;
		result.canBashDoors = canBashDoors;
		result.canBashFences = canBashFences;
		result.alwaysUseAvoidGrid = alwaysUseAvoidGrid;
		result.fenceBlocked = fenceBlocked || forceFenceblocked;
		return result;
	}

	public void Validate()
	{
		if (mode == TraverseMode.ByPawn && pawn == null)
		{
			Log.Error("Invalid traverse parameters: IfPawnAllowed but traverser = null.");
		}
	}

	public static implicit operator TraverseParms(TraverseMode m)
	{
		if (m == TraverseMode.ByPawn)
		{
			throw new InvalidOperationException("Cannot implicitly convert TraverseMode.ByPawn to RegionTraverseParameters.");
		}
		return For(m);
	}

	public static bool operator ==(TraverseParms a, TraverseParms b)
	{
		if (a.pawn == b.pawn && a.mode == b.mode && a.canBashDoors == b.canBashDoors && a.canBashFences == b.canBashFences && a.maxDanger == b.maxDanger && a.alwaysUseAvoidGrid == b.alwaysUseAvoidGrid)
		{
			return a.fenceBlocked == b.fenceBlocked;
		}
		return false;
	}

	public static bool operator !=(TraverseParms a, TraverseParms b)
	{
		if (a.pawn == b.pawn && a.mode == b.mode && a.canBashDoors == b.canBashDoors && a.canBashFences == b.canBashFences && a.maxDanger == b.maxDanger && a.alwaysUseAvoidGrid == b.alwaysUseAvoidGrid)
		{
			return a.fenceBlocked != b.fenceBlocked;
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is TraverseParms))
		{
			return false;
		}
		return Equals((TraverseParms)obj);
	}

	public bool Equals(TraverseParms other)
	{
		if (other.pawn == pawn && other.mode == mode && other.canBashDoors == canBashDoors && other.canBashFences == canBashFences && other.maxDanger == maxDanger && other.alwaysUseAvoidGrid == alwaysUseAvoidGrid)
		{
			return other.fenceBlocked == fenceBlocked;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int seed = (canBashDoors ? 1 : 0);
		seed = ((pawn == null) ? Gen.HashCombineStruct(seed, mode) : Gen.HashCombine(seed, pawn));
		seed = Gen.HashCombineStruct(seed, canBashFences);
		seed = Gen.HashCombineStruct(seed, maxDanger);
		seed = Gen.HashCombineStruct(seed, alwaysUseAvoidGrid);
		return Gen.HashCombineStruct(seed, fenceBlocked);
	}

	public override string ToString()
	{
		string text = (canBashDoors ? " canBashDoors" : "");
		string text2 = (canBashFences ? " canBashFences" : "");
		string text3 = (alwaysUseAvoidGrid ? " alwaysUseAvoidGrid" : "");
		string text4 = (fenceBlocked ? " fenceBlocked" : "");
		if (mode == TraverseMode.ByPawn)
		{
			return $"({mode} {maxDanger} {pawn}{text}{text2}{text3}{text4})";
		}
		return $"({mode} {maxDanger}{text}{text2}{text3}{text4})";
	}
}
