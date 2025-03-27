using System;
using System.Collections.Generic;

namespace Verse;

public sealed class FleckManager : IExposable
{
	public readonly Map parent;

	private Dictionary<Type, FleckSystem> systems = new Dictionary<Type, FleckSystem>();

	private DrawBatch drawBatch = new DrawBatch();

	public FleckManager()
	{
		foreach (FleckDef item in DefDatabase<FleckDef>.AllDefsListForReading)
		{
			if (!systems.TryGetValue(item.fleckSystemClass, out var value))
			{
				value = (FleckSystem)Activator.CreateInstance(item.fleckSystemClass);
				value.parent = this;
				systems.Add(item.fleckSystemClass, value);
			}
			value.handledDefs.Add(item);
		}
	}

	public FleckManager(Map parent)
		: this()
	{
		this.parent = parent;
	}

	public void CreateFleck(FleckCreationData fleckData)
	{
		if (!systems.TryGetValue(fleckData.def.fleckSystemClass, out var value))
		{
			throw new Exception(string.Concat("No system to handle MoteDef ", fleckData.def, " found!?"));
		}
		fleckData.spawnPosition.y = fleckData.def.altitudeLayer.AltitudeFor(fleckData.def.altitudeLayerIncOffset);
		value.CreateFleck(fleckData);
	}

	public void FleckManagerUpdate()
	{
		foreach (FleckSystem value in systems.Values)
		{
			value.Update();
		}
	}

	public void FleckManagerTick()
	{
		foreach (FleckSystem value in systems.Values)
		{
			value.Tick();
		}
	}

	public void FleckManagerDraw()
	{
		try
		{
			foreach (FleckSystem value in systems.Values)
			{
				value.Draw(drawBatch);
			}
		}
		finally
		{
			drawBatch.Flush();
		}
	}

	public void FleckManagerOnGUI()
	{
		foreach (FleckSystem value in systems.Values)
		{
			value.OnGUI();
		}
	}

	public void ExposeData()
	{
	}
}
