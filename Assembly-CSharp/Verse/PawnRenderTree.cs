using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;

namespace Verse;

public class PawnRenderTree
{
	public Pawn pawn;

	public PawnRenderNode rootNode;

	private PawnDrawParms oldParms;

	private List<PawnGraphicDrawRequest> drawRequests = new List<PawnGraphicDrawRequest>();

	private Dictionary<PawnRenderNodeTagDef, PawnRenderNode> nodesByTag = new Dictionary<PawnRenderNodeTagDef, PawnRenderNode>();

	private Dictionary<PawnRenderNode, List<PawnRenderNode>> nodeAncestors = new Dictionary<PawnRenderNode, List<PawnRenderNode>>();

	private Dictionary<PawnRenderNodeTagDef, List<PawnRenderNode>> tmpChildTagNodes = new Dictionary<PawnRenderNodeTagDef, List<PawnRenderNode>>();

	public AnimationDef currentAnimation;

	public int animationStartTick = -99999;

	public Color? debugTint;

	private const float ApparelLayerShellNorth = 88f;

	private readonly Dictionary<PawnRenderNode, float> layerOffsets = new Dictionary<PawnRenderNode, float>();

	private readonly Queue<PawnRenderNode> nodeQueue = new Queue<PawnRenderNode>();

	public bool Resolved => rootNode != null;

	public Graphic BodyGraphic
	{
		get
		{
			TrySetupGraphIfNeeded();
			if (nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.Body, out var value))
			{
				return value?.Graphic;
			}
			return null;
		}
	}

	public Graphic HeadGraphic
	{
		get
		{
			TrySetupGraphIfNeeded();
			if (nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.Head, out var value))
			{
				return value?.Graphic;
			}
			return null;
		}
	}

	public int AnimationTick
	{
		get
		{
			if (currentAnimation != null && currentAnimation.durationTicks > 0)
			{
				return (Find.TickManager.TicksGame - animationStartTick) % currentAnimation.durationTicks;
			}
			return 0;
		}
	}

	private bool HasValidPropsAndRenderTree => pawn.RaceProps.renderTree?.root?.nodeClass != null;

	public PawnRenderTree(Pawn pawn)
	{
		this.pawn = pawn;
	}

	public void EnsureInitialized(PawnRenderFlags defaultRenderFlagsNow)
	{
		TrySetupGraphIfNeeded();
		rootNode?.EnsureInitialized(defaultRenderFlagsNow);
	}

	public void ParallelPreDraw(PawnDrawParms parms)
	{
		if (!HasValidPropsAndRenderTree)
		{
			return;
		}
		AdjustParms(ref parms);
		if (oldParms.ShouldRecache(parms) || rootNode.RecacheRequested)
		{
			TraverseTree(delegate(PawnRenderNode node)
			{
				node.requestRecache = false;
			});
			drawRequests.Clear();
			rootNode.AppendRequests(parms, drawRequests);
			oldParms = parms;
		}
		for (int i = 0; i < drawRequests.Count; i++)
		{
			PawnGraphicDrawRequest value = drawRequests[i];
			if (!TryGetMatrix(value.node, parms, out var matrix))
			{
				Log.ErrorOnce($"Failed to get matrix for {value.node} on {pawn}.", Gen.HashCombine(174383246, pawn.GetHashCode()));
				break;
			}
			if (value.node.CheckMaterialEveryDrawRequest)
			{
				value.material = value.node.Worker.GetFinalizedMaterial(value.node, parms);
			}
			value.preDrawnComputedMatrix = matrix;
			drawRequests[i] = value;
		}
	}

	public void Draw(PawnDrawParms parms)
	{
		if (!Resolved)
		{
			Log.ErrorOnce($"Attempted to draw {pawn} without a resolved render tree.", Gen.HashCombine(174383246, pawn.GetHashCode()));
			return;
		}
		using (new ProfilerBlock("PawnRenderTree.Draw"))
		{
			for (int i = 0; i < drawRequests.Count; i++)
			{
				PawnGraphicDrawRequest pawnGraphicDrawRequest = drawRequests[i];
				Material material = pawnGraphicDrawRequest.material;
				if (material != null)
				{
					pawnGraphicDrawRequest.node.Worker.PreDraw(pawnGraphicDrawRequest.node, material, parms);
					MaterialPropertyBlock block = pawnGraphicDrawRequest.node.Worker.GetMaterialPropertyBlock(pawnGraphicDrawRequest.node, material, parms);
					foreach (PawnRenderSubWorker subWorker in pawnGraphicDrawRequest.node.Props.SubWorkers)
					{
						subWorker.EditMaterialPropertyBlock(pawnGraphicDrawRequest.node, material, parms, ref block);
					}
					GenDraw.DrawMeshNowOrLater(pawnGraphicDrawRequest.mesh, pawnGraphicDrawRequest.preDrawnComputedMatrix, material, parms.DrawNow, block);
					block.Clear();
				}
				pawnGraphicDrawRequest.node.Worker.PostDraw(pawnGraphicDrawRequest.node, parms, pawnGraphicDrawRequest.mesh, pawnGraphicDrawRequest.preDrawnComputedMatrix);
			}
		}
	}

	public bool GetRootTPRS(PawnDrawParms parms, out Vector3 offset, out Vector3 pivot, out Quaternion rotation, out Vector3 scale)
	{
		offset = Vector3.zero;
		pivot = Vector3.zero;
		rotation = Quaternion.identity;
		scale = Vector3.zero;
		if (rootNode == null || nodeAncestors == null)
		{
			return false;
		}
		List<PawnRenderNode> value;
		bool result = nodeAncestors.TryGetValue(rootNode, out value);
		for (int i = 0; i < value.Count; i++)
		{
			value[i].GetTransform(parms, out var offset2, out var pivot2, out var rotation2, out var scale2);
			offset += offset2;
			pivot += pivot2;
			rotation *= rotation2;
			scale += scale2;
		}
		return result;
	}

	public bool TryGetMatrix(PawnRenderNode node, PawnDrawParms parms, out Matrix4x4 matrix)
	{
		matrix = parms.matrix;
		if (!nodeAncestors.TryGetValue(node, out var value))
		{
			SetDirty();
			TrySetupGraphIfNeeded();
			if (!nodeAncestors.TryGetValue(node, out value))
			{
				return false;
			}
		}
		for (int i = 0; i < value.Count; i++)
		{
			value[i].GetTransform(parms, out var offset, out var pivot, out var rotation, out var scale);
			if (offset != Vector3.zero)
			{
				matrix *= Matrix4x4.Translate(offset);
			}
			if (pivot != Vector3.zero)
			{
				matrix *= Matrix4x4.Translate(pivot);
			}
			if ((!node.Props.rotateIndependently || value[i] == node) && rotation != Quaternion.identity)
			{
				matrix *= Matrix4x4.Rotate(rotation);
			}
			if (scale != Vector3.one)
			{
				matrix *= Matrix4x4.Scale(scale);
			}
			if (pivot != Vector3.zero)
			{
				matrix *= Matrix4x4.Translate(pivot).inverse;
			}
		}
		float num = node.Worker.AltitudeFor(node, parms);
		if (num != 0f)
		{
			matrix *= Matrix4x4.Translate(Vector3.up * num);
		}
		return true;
	}

	private void AdjustParms(ref PawnDrawParms parms)
	{
		if (debugTint.HasValue)
		{
			parms.tint *= debugTint.Value;
		}
		if (!pawn.RaceProps.Humanlike)
		{
			return;
		}
		if (parms.crawling && !parms.Portrait && parms.facing == Rot4.South)
		{
			parms.facing = Rot4.North;
			parms.flipHead = true;
		}
		if (pawn.apparel != null && PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms))
		{
			foreach (Apparel item in pawn.apparel.WornApparel)
			{
				if (item.def.apparel.renderSkipFlags != null)
				{
					foreach (RenderSkipFlagDef renderSkipFlag in item.def.apparel.renderSkipFlags)
					{
						if (renderSkipFlag != RenderSkipFlagDefOf.None)
						{
							parms.skipFlags |= renderSkipFlag;
						}
					}
				}
				else
				{
					if (item.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
					{
						parms.skipFlags |= RenderSkipFlagDefOf.Hair;
					}
					if (item.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
					{
						parms.skipFlags |= RenderSkipFlagDefOf.Hair;
						parms.skipFlags |= RenderSkipFlagDefOf.Beard;
						parms.skipFlags |= RenderSkipFlagDefOf.Eyes;
					}
				}
				if (item.def.apparel.forceEyesVisibleForRotations.Contains(parms.facing.AsInt))
				{
					parms.skipFlags &= ~(ulong)RenderSkipFlagDefOf.Eyes;
				}
			}
		}
		if (pawn.genes != null && !pawn.genes.TattoosVisible)
		{
			parms.skipFlags |= RenderSkipFlagDefOf.Tattoos;
		}
	}

	private void TrySetupGraphIfNeeded()
	{
		if (Resolved)
		{
			return;
		}
		PawnRenderNodeProperties pawnRenderNodeProperties = pawn.RaceProps.renderTree?.root;
		if (pawnRenderNodeProperties?.nodeClass == null)
		{
			return;
		}
		SetDirty();
		using (new ProfilerBlock("TrySetupGraph"))
		{
			try
			{
				rootNode = (PawnRenderNode)Activator.CreateInstance(pawnRenderNodeProperties.nodeClass, pawn, pawnRenderNodeProperties, this);
				SetupDynamicNodes();
			}
			catch (Exception arg)
			{
				Log.Error($"Exception setting up dynamic nodes for {pawn}: {arg}");
			}
			foreach (var (key, list2) in tmpChildTagNodes)
			{
				if (nodesByTag.TryGetValue(key, out var value))
				{
					value.AddChildren(list2.ToArray());
				}
			}
			if (Prefs.DevMode)
			{
				Dialog_DebugRenderTree dialog_DebugRenderTree = Find.WindowStack.WindowOfType<Dialog_DebugRenderTree>();
				if (dialog_DebugRenderTree != null && dialog_DebugRenderTree.pawn == pawn)
				{
					dialog_DebugRenderTree.Init(pawn);
				}
			}
			InitializeAncestors();
		}
		tmpChildTagNodes.Clear();
	}

	private void SetupDynamicNodes()
	{
		SetupHediffNodes();
		SetupMutantNodes();
		if (pawn.RaceProps.Humanlike)
		{
			SetupApparelNodes();
			SetupGeneNodes();
			SetupTraitNodes();
		}
		foreach (ThingComp allComp in pawn.AllComps)
		{
			List<PawnRenderNode> list = allComp.CompRenderNodes();
			if (list == null)
			{
				continue;
			}
			foreach (PawnRenderNode item in list)
			{
				if (ShouldAddNodeToTree(item?.Props))
				{
					AddChild(item, null);
				}
			}
		}
	}

	private void SetupApparelNodes()
	{
		if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
		{
			return;
		}
		PawnRenderNode value;
		PawnRenderNode headApparelNode = (nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.ApparelHead, out value) ? value : null);
		PawnRenderNode value2;
		PawnRenderNode bodyApparelNode = (nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.ApparelBody, out value2) ? value2 : null);
		foreach (Apparel item in pawn.apparel.WornApparel)
		{
			try
			{
				ProcessApparel(item, headApparelNode, bodyApparelNode);
			}
			catch (Exception arg)
			{
				Log.Error($"Exception setting up node for {item.def.defName} on {pawn}: {arg}");
			}
		}
		layerOffsets.Clear();
	}

	private void ProcessApparel(Apparel ap, PawnRenderNode headApparelNode, PawnRenderNode bodyApparelNode)
	{
		if (ap.def.apparel.HasDefinedGraphicProperties)
		{
			foreach (PawnRenderNodeProperties renderNodeProperty in ap.def.apparel.RenderNodeProperties)
			{
				if (ShouldAddNodeToTree(renderNodeProperty))
				{
					PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, this);
					pawnRenderNode.apparel = ap;
					AddChild(pawnRenderNode, null);
				}
			}
			return;
		}
		if (!ApparelGraphicRecordGetter.TryGetGraphicApparel(ap, pawn.story.bodyType, out var _))
		{
			return;
		}
		PawnRenderNodeProperties pawnRenderNodeProperties = null;
		PawnRenderNode pawnRenderNode2 = null;
		DrawData drawData = ap.def.apparel.drawData;
		ApparelLayerDef lastLayer = ap.def.apparel.LastLayer;
		bool flag = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover;
		if (ap.def.apparel.parentTagDef != null && nodesByTag.TryGetValue(ap.def.apparel.parentTagDef, out var value))
		{
			pawnRenderNode2 = value;
			if (headApparelNode != null && pawnRenderNode2 == headApparelNode)
			{
				flag = true;
			}
			else if (bodyApparelNode != null && pawnRenderNode2 == bodyApparelNode)
			{
				flag = false;
			}
		}
		if (headApparelNode != null && flag)
		{
			if (pawnRenderNode2 == null)
			{
				pawnRenderNode2 = headApparelNode;
			}
			float value2;
			float num = (layerOffsets.TryGetValue(pawnRenderNode2, out value2) ? value2 : 0f);
			pawnRenderNodeProperties = new PawnRenderNodeProperties
			{
				debugLabel = ap.def.defName,
				workerClass = typeof(PawnRenderNodeWorker_Apparel_Head),
				baseLayer = pawnRenderNode2.Props.baseLayer + num,
				drawData = drawData
			};
		}
		else if (bodyApparelNode != null)
		{
			if (pawnRenderNode2 == null)
			{
				pawnRenderNode2 = bodyApparelNode;
			}
			float value3;
			float num2 = (layerOffsets.TryGetValue(pawnRenderNode2, out value3) ? value3 : 0f);
			pawnRenderNodeProperties = new PawnRenderNodeProperties
			{
				debugLabel = ap.def.defName,
				workerClass = typeof(PawnRenderNodeWorker_Apparel_Body),
				baseLayer = pawnRenderNode2.Props.baseLayer + num2,
				drawData = drawData
			};
			if (drawData == null && !ap.def.apparel.shellRenderedBehindHead)
			{
				if (lastLayer == ApparelLayerDefOf.Shell)
				{
					pawnRenderNodeProperties.drawData = DrawData.NewWithData(new DrawData.RotationalData(Rot4.North, 88f));
					pawnRenderNodeProperties.oppositeFacingLayerWhenFlipped = true;
				}
				else if (ap.RenderAsPack())
				{
					pawnRenderNodeProperties.drawData = DrawData.NewWithData(new DrawData.RotationalData(Rot4.North, 93f), new DrawData.RotationalData(Rot4.South, -3f));
					pawnRenderNodeProperties.oppositeFacingLayerWhenFlipped = true;
				}
			}
		}
		if (ShouldAddNodeToTree(pawnRenderNodeProperties))
		{
			AddChild(new PawnRenderNode_Apparel(pawn, pawnRenderNodeProperties, this, ap, flag), pawnRenderNode2);
		}
		if (pawnRenderNode2 != null)
		{
			if (layerOffsets.ContainsKey(pawnRenderNode2))
			{
				layerOffsets[pawnRenderNode2]++;
			}
			else
			{
				layerOffsets.Add(pawnRenderNode2, 1f);
			}
		}
	}

	private void SetupTraitNodes()
	{
		if (pawn.story?.traits == null)
		{
			return;
		}
		foreach (Trait allTrait in pawn.story.traits.allTraits)
		{
			if (allTrait.Suppressed || !allTrait.CurrentData.HasDefinedGraphicProperties)
			{
				continue;
			}
			foreach (PawnRenderNodeProperties renderNodeProperty in allTrait.CurrentData.RenderNodeProperties)
			{
				if (ShouldAddNodeToTree(renderNodeProperty))
				{
					PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, this);
					pawnRenderNode.trait = allTrait;
					AddChild(pawnRenderNode, null);
				}
			}
		}
	}

	private void SetupGeneNodes()
	{
		if (!ModsConfig.BiotechActive || pawn.genes == null)
		{
			return;
		}
		foreach (Gene item in pawn.genes.GenesListForReading)
		{
			if (!item.Active || !item.def.HasDefinedGraphicProperties)
			{
				continue;
			}
			foreach (PawnRenderNodeProperties renderNodeProperty in item.def.RenderNodeProperties)
			{
				if (ShouldAddNodeToTree(renderNodeProperty))
				{
					PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, this);
					pawnRenderNode.gene = item;
					AddChild(pawnRenderNode, null);
				}
			}
		}
	}

	private void SetupHediffNodes()
	{
		List<Hediff> list = pawn.health?.hediffSet?.hediffs;
		if (list == null)
		{
			return;
		}
		foreach (Hediff item in list)
		{
			if (!item.Visible || !item.def.HasDefinedGraphicProperties)
			{
				continue;
			}
			foreach (PawnRenderNodeProperties renderNodeProperty in item.def.RenderNodeProperties)
			{
				if (ShouldAddNodeToTree(renderNodeProperty))
				{
					PawnRenderNode pawnRenderNode = (PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, this);
					pawnRenderNode.hediff = item;
					AddChild(pawnRenderNode, null);
				}
			}
		}
	}

	private void SetupMutantNodes()
	{
		if (!ModsConfig.AnomalyActive || !pawn.IsMutant || !pawn.mutant.Def.HasDefinedGraphicProperties)
		{
			return;
		}
		foreach (PawnRenderNodeProperties renderNodeProperty in pawn.mutant.Def.RenderNodeProperties)
		{
			if (ShouldAddNodeToTree(renderNodeProperty))
			{
				AddChild((PawnRenderNode)Activator.CreateInstance(renderNodeProperty.nodeClass, pawn, renderNodeProperty, this), null);
			}
		}
	}

	private void InitializeAncestors()
	{
		TraverseTree(delegate(PawnRenderNode node)
		{
			if (!nodeAncestors.ContainsKey(node))
			{
				nodeAncestors.Add(node, new List<PawnRenderNode>());
			}
			for (PawnRenderNode pawnRenderNode = node; pawnRenderNode != null; pawnRenderNode = pawnRenderNode.parent)
			{
				nodeAncestors[node].Add(pawnRenderNode);
			}
			nodeAncestors[node].Reverse();
		});
	}

	private void TraverseTree(Action<PawnRenderNode> action)
	{
		try
		{
			nodeQueue.Enqueue(rootNode);
			while (nodeQueue.Count > 0)
			{
				PawnRenderNode pawnRenderNode = nodeQueue.Dequeue();
				if (pawnRenderNode == null)
				{
					Log.ErrorOnce($"Node is null - you must called EnsureGraphicsInitialized() on the drawn dynamic thing {pawn} before drawing it.", Gen.HashCombine(1743846, pawn.GetHashCode()));
					break;
				}
				action(pawnRenderNode);
				if (pawnRenderNode.children != null)
				{
					PawnRenderNode[] children = pawnRenderNode.children;
					foreach (PawnRenderNode item in children)
					{
						nodeQueue.Enqueue(item);
					}
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"Exception traversing pawn render node tree {pawn}: {arg}");
		}
		finally
		{
			nodeQueue.Clear();
		}
	}

	private bool ShouldAddNodeToTree(PawnRenderNodeProperties props)
	{
		if (props == null)
		{
			return false;
		}
		return props.pawnType switch
		{
			PawnRenderNodeProperties.RenderNodePawnType.HumanlikeOnly => pawn.RaceProps.Humanlike, 
			PawnRenderNodeProperties.RenderNodePawnType.NonHumanlikeOnly => !pawn.RaceProps.Humanlike, 
			_ => true, 
		};
	}

	private void AddChild(PawnRenderNode child, PawnRenderNode parent)
	{
		if (parent == null)
		{
			parent = ((child.Props.parentTagDef == null || !nodesByTag.TryGetValue(child.Props.parentTagDef, out var value)) ? rootNode : value);
		}
		if (parent.Props.tagDef != null)
		{
			if (tmpChildTagNodes.TryGetValue(parent.Props.tagDef, out var value2))
			{
				value2.Add(child);
			}
			else
			{
				tmpChildTagNodes.Add(parent.Props.tagDef, new List<PawnRenderNode> { child });
			}
		}
		child.parent = parent;
	}

	public void SetTagNode(PawnRenderNodeTagDef tag, PawnRenderNode node)
	{
		nodesByTag[tag] = node;
	}

	public void SetDirty()
	{
		nodeAncestors.Clear();
		drawRequests.Clear();
		rootNode = null;
		nodesByTag.Clear();
		oldParms = default(PawnDrawParms);
	}

	public bool TryGetAnimationPartForNode(PawnRenderNode node, out AnimationPart animationPart)
	{
		animationPart = null;
		if (currentAnimation == null)
		{
			return false;
		}
		if (node.Props.tagDef == null)
		{
			return false;
		}
		return node.tree.currentAnimation.animationParts.TryGetValue(node.Props.tagDef, out animationPart);
	}
}
