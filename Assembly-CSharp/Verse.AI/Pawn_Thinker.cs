namespace Verse.AI;

public class Pawn_Thinker : IExposable
{
	public Pawn pawn;

	public ThinkTreeDef MainThinkTree
	{
		get
		{
			object obj;
			if (!pawn.IsMutant)
			{
				obj = pawn.ageTracker.CurLifeStage?.thinkTreeMainOverride;
				if (obj == null)
				{
					return pawn.RaceProps.thinkTreeMain;
				}
			}
			else
			{
				obj = pawn.mutant.GetThinkTrees().main;
			}
			return (ThinkTreeDef)obj;
		}
	}

	public ThinkNode MainThinkNodeRoot => MainThinkTree.thinkRoot;

	public ThinkTreeDef ConstantThinkTree
	{
		get
		{
			object obj;
			if (!pawn.IsMutant)
			{
				obj = pawn.ageTracker.CurLifeStage?.thinkTreeConstantOverride;
				if (obj == null)
				{
					return pawn.RaceProps.thinkTreeConstant;
				}
			}
			else
			{
				obj = pawn.mutant.GetThinkTrees().constant;
			}
			return (ThinkTreeDef)obj;
		}
	}

	public ThinkNode ConstantThinkNodeRoot => ConstantThinkTree.thinkRoot;

	public Pawn_Thinker(Pawn pawn)
	{
		this.pawn = pawn;
	}

	public T TryGetMainTreeThinkNode<T>() where T : ThinkNode
	{
		return MainThinkNodeRoot.FirstNodeOfType<T>();
	}

	public T GetMainTreeThinkNode<T>() where T : ThinkNode
	{
		T val = TryGetMainTreeThinkNode<T>();
		if (val == null)
		{
			Log.Warning(string.Concat(pawn, " looked for ThinkNode of type ", typeof(T), " and didn't find it."));
		}
		return val;
	}

	public void ExposeData()
	{
	}
}
