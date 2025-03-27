using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld;

public class LayoutDef : Def
{
	public List<LayoutRoomDef> roomDefs;

	public Type workerClass;

	public bool canHaveMultipleLayoutsInRoom;

	public float multipleLayoutRoomChance = 0.15f;

	[Unsaved(false)]
	private LayoutWorker workerInt;

	public LayoutWorker Worker => workerInt ?? (workerInt = (LayoutWorker)Activator.CreateInstance(workerClass, this));
}
