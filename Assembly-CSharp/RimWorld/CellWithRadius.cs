using System;
using Verse;

namespace RimWorld;

public struct CellWithRadius : IEquatable<CellWithRadius>
{
	public readonly IntVec3 cell;

	public readonly float radius;

	public CellWithRadius(IntVec3 c, float r)
	{
		cell = c;
		radius = r;
	}

	public bool Equals(CellWithRadius other)
	{
		if (cell.Equals(other.cell))
		{
			return radius.Equals(other.radius);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is CellWithRadius other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (cell.GetHashCode() * 397) ^ radius.GetHashCode();
	}
}
