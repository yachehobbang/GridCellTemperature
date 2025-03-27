using System.Collections.Generic;

namespace Verse;

public abstract class FleckSystem : IExposable, ILoadReferenceable
{
	public List<FleckDef> handledDefs = new List<FleckDef>();

	public FleckManager parent;

	public abstract void Update();

	public abstract void Tick();

	public abstract void Draw(DrawBatch drawBatch);

	public virtual void OnGUI()
	{
	}

	public abstract void CreateFleck(FleckCreationData fleckData);

	public abstract void ExposeData();

	public string GetUniqueLoadID()
	{
		return parent.parent.GetUniqueLoadID() + "_FleckSystem_" + GetType().FullName;
	}
}
