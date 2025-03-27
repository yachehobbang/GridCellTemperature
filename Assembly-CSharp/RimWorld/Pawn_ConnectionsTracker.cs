using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class Pawn_ConnectionsTracker : IExposable
{
	private Pawn pawn;

	private List<Thing> connectedThings = new List<Thing>();

	public List<Thing> ConnectedThings => connectedThings;

	public Pawn_ConnectionsTracker()
	{
	}

	public Pawn_ConnectionsTracker(Pawn pawn)
	{
		this.pawn = pawn;
	}

	public void ConnectTo(Thing thing)
	{
		connectedThings.Add(thing);
	}

	public void Notify_PawnKilled()
	{
		for (int num = connectedThings.Count - 1; num >= 0; num--)
		{
			connectedThings[num].TryGetComp<CompTreeConnection>()?.Notify_PawnDied(pawn);
			connectedThings.RemoveAt(num);
		}
	}

	public void Notify_ConnectedThingDestroyed(Thing thing)
	{
		if (connectedThings.Remove(thing) && pawn.needs?.mood?.thoughts?.memories != null)
		{
			pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.ConnectedTreeDied);
		}
	}

	public IEnumerable<Gizmo> GetGizmos()
	{
		if (!connectedThings.Any() || Find.Selector.SingleSelectedThing != pawn)
		{
			yield break;
		}
		if (pawn.IsColonistPlayerControlled)
		{
			yield return new Command_Action
			{
				defaultLabel = "CommandSelectConnectedTree".Translate(),
				onHover = DrawConnectionLines,
				icon = Widgets.GetIconFor(ThingDefOf.Plant_TreeGauranlen, null, null, null),
				action = delegate
				{
					if (ConnectedThings.Count == 1)
					{
						CameraJumper.TryJumpAndSelect(ConnectedThings[0]);
					}
					else if (ConnectedThings.Count > 1)
					{
						List<FloatMenuOption> list = new List<FloatMenuOption>();
						for (int i = 0; i < ConnectedThings.Count; i++)
						{
							Thing t = ConnectedThings[i];
							string text = "NoCaste".Translate();
							CompTreeConnection compTreeConnection = t.TryGetComp<CompTreeConnection>();
							if (compTreeConnection != null && compTreeConnection.Mode != null)
							{
								text = compTreeConnection.Mode.label;
							}
							list.Add(new FloatMenuOption(t.LabelCap + " (" + text + ")", delegate
							{
								CameraJumper.TryJumpAndSelect(t);
							}, t.def, null, forceBasicStyle: false, MenuOptionPriority.Default, null, null, 0f, null, null, playSelectionSound: true, 0, null));
						}
						if (list.Any())
						{
							Find.WindowStack.Add(new FloatMenu(list));
						}
					}
				}
			};
		}
		else if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Dryad)
		{
			yield return new Command_Toggle
			{
				defaultLabel = "CommandHealInHealingPod".Translate(),
				defaultDesc = "CommandHealInHealingPodDesc".Translate(ThingDefOf.DryadHealingPod.GetCompProperties<CompProperties_DryadCocoon>().daysToComplete.Named("DAYS")),
				icon = Widgets.GetIconFor(ThingDefOf.DryadHealingPod, null, null, null),
				isActive = () => pawn.mindState.returnToHealingPod,
				toggleAction = delegate
				{
					pawn.mindState.returnToHealingPod = !pawn.mindState.returnToHealingPod;
				}
			};
		}
	}

	private void DrawConnectionLines()
	{
		foreach (Thing connectedThing in ConnectedThings)
		{
			DrawConnectionLine(connectedThing);
		}
	}

	public void DrawConnectionLine(Thing t)
	{
		if (t.Spawned && t.Map == pawn.Map)
		{
			GenDraw.DrawLineBetween(pawn.TrueCenter(), t.TrueCenter(), SimpleColor.Orange);
		}
	}

	public void ExposeData()
	{
		Scribe_Collections.Look(ref connectedThings, "connectedThings", LookMode.Reference);
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			connectedThings.RemoveAll((Thing x) => x?.Destroyed ?? true);
		}
	}
}
