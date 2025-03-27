using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorld.Planet;

[StaticConstructorOnStartup]
public class WorldRenderer
{
	private List<WorldLayer> layers = new List<WorldLayer>();

	public WorldRenderMode wantedMode;

	private bool asynchronousRegenerationActive;

	private bool ShouldRegenerateDirtyLayersInLongEvent
	{
		get
		{
			for (int i = 0; i < layers.Count; i++)
			{
				if (layers[i].Dirty && layers[i] is WorldLayer_Terrain)
				{
					return true;
				}
			}
			return false;
		}
	}

	public WorldRenderer()
	{
		foreach (Type item in typeof(WorldLayer).AllLeafSubclasses())
		{
			layers.Add((WorldLayer)Activator.CreateInstance(item));
		}
	}

	public void SetAllLayersDirty()
	{
		for (int i = 0; i < layers.Count; i++)
		{
			layers[i].SetDirty();
		}
	}

	public void SetDirty<T>() where T : WorldLayer
	{
		for (int i = 0; i < layers.Count; i++)
		{
			if (layers[i] is T)
			{
				layers[i].SetDirty();
			}
		}
	}

	public void RegenerateAllLayersNow()
	{
		for (int i = 0; i < layers.Count; i++)
		{
			layers[i].RegenerateNow();
		}
	}

	private IEnumerable RegenerateDirtyLayersNow_Async()
	{
		for (int i = 0; i < layers.Count; i++)
		{
			if (!layers[i].Dirty)
			{
				continue;
			}
			{
				IEnumerator enumerator = layers[i].Regenerate().GetEnumerator();
				try
				{
					while (true)
					{
						try
						{
							if (!enumerator.MoveNext())
							{
								break;
							}
						}
						catch (Exception ex)
						{
							Log.Error("Could not regenerate WorldLayer: " + ex);
							break;
						}
						yield return enumerator.Current;
					}
				}
				finally
				{
					IDisposable disposable = enumerator as IDisposable;
					if (disposable != null)
					{
						disposable.Dispose();
					}
				}
			}
			yield return null;
		}
		asynchronousRegenerationActive = false;
	}

	public void Notify_StaticWorldObjectPosChanged()
	{
		for (int i = 0; i < layers.Count; i++)
		{
			if (layers[i] is WorldLayer_WorldObjects worldLayer_WorldObjects)
			{
				worldLayer_WorldObjects.SetDirty();
			}
		}
	}

	public void Notify_TilePollutionChanged(int tileId)
	{
		for (int i = 0; i < layers.Count; i++)
		{
			if (layers[i] is WorldLayer_Pollution worldLayer_Pollution)
			{
				worldLayer_Pollution.Notify_TilePollutionChanged(tileId);
			}
		}
	}

	public void CheckActivateWorldCamera()
	{
		Find.WorldCamera.gameObject.SetActive(WorldRendererUtility.WorldRenderedNow);
	}

	public bool RegenerateLayersIfDirtyInLongEvent()
	{
		if (ShouldRegenerateDirtyLayersInLongEvent)
		{
			asynchronousRegenerationActive = true;
			LongEventHandler.QueueLongEvent(RegenerateDirtyLayersNow_Async(), "GeneratingPlanet", null, showExtraUIInfo: false);
			return true;
		}
		return false;
	}

	public void DrawWorldLayers()
	{
		if (asynchronousRegenerationActive)
		{
			Log.Error("Called DrawWorldLayers() but already regenerating. This shouldn't ever happen because LongEventHandler should have stopped us.");
		}
		else
		{
			if (RegenerateLayersIfDirtyInLongEvent())
			{
				return;
			}
			WorldRendererUtility.UpdateWorldShadersParams();
			for (int i = 0; i < layers.Count; i++)
			{
				try
				{
					layers[i].Render();
				}
				catch (Exception ex)
				{
					Log.Error("Error drawing WorldLayer: " + ex);
				}
			}
		}
	}

	public int GetTileIDFromRayHit(RaycastHit hit)
	{
		int i = 0;
		for (int count = layers.Count; i < count; i++)
		{
			if (layers[i] is WorldLayer_Terrain worldLayer_Terrain)
			{
				return worldLayer_Terrain.GetTileIDFromRayHit(hit);
			}
		}
		return -1;
	}
}
