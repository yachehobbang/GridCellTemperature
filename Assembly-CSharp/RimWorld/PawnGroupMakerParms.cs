namespace RimWorld;

public class PawnGroupMakerParms
{
	public PawnGroupKindDef groupKind;

	public int tile = -1;

	public bool inhabitants;

	public float points;

	public Faction faction;

	public Ideo ideo;

	public TraderKindDef traderKind;

	public bool generateFightersOnly;

	public bool dontUseSingleUseRocketLaunchers;

	public RaidStrategyDef raidStrategy;

	public bool forceOneDowned;

	public int? seed;

	public RaidAgeRestrictionDef raidAgeRestriction;

	public override string ToString()
	{
		return string.Concat("groupKind=", groupKind, ", tile=", tile, ", inhabitants=", inhabitants.ToString(), ", points=", points, ", faction=", faction, ", ideo=", ideo?.name, ", traderKind=", traderKind, ", generateFightersOnly=", generateFightersOnly.ToString(), ", dontUseSingleUseRocketLaunchers=", dontUseSingleUseRocketLaunchers.ToString(), ", raidStrategy=", raidStrategy, ", forceOneDowned=", forceOneDowned.ToString(), ", seed=", seed, ", raidAgeRestriction=", raidAgeRestriction);
	}
}
