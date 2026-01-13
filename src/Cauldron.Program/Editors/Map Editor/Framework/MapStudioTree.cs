using DotNext.Collections.Generic;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Veldrid.Utilities;

namespace StudioCore.Editors.MapEditor;

public class MapStudioTree
{
    /// <summary>
    /// The root node.
    /// </summary>
    public MapStudioTreeNode Root;

    public void BuildBottomUp(List<PartInfo> parts)
    {
        var layer = new List<MapStudioTreeNode>(parts.Count);
        foreach (var part in parts)
        {
            layer.Add(new MapStudioTreeNode()
            {
                Bounds = part.Bounds,
                PartIndices = [part.Index]
            });
        }

        var nextLayer = new List<MapStudioTreeNode>();
        foreach (var nodeA in layer)
        {
            var nodeB = FindClosestNode(layer, nodeA);

            nodeA.NextSibling = nodeB;
            var newNode = new MapStudioTreeNode()
            {
                Bounds = BoundingBox.Combine(nodeA.Bounds, nodeB.Bounds),
                FirstChild = nodeA
            };

            layer.Remove(nodeA);
            layer.Remove(nodeB);
            nextLayer.Add(newNode);
        }

        // TODO
    }

    private static MapStudioTreeNode FindClosestNode(List<MapStudioTreeNode> nodes, MapStudioTreeNode nodeA)
    {
        MapStudioTreeNode closestNode = null;
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < nodes.Count; i++)
        {
            var nodeB = nodes[i];
            float distance = BoundingBox.GetDistanceByCenter(nodeA.Bounds, nodeB.Bounds);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = nodeB;
            }

            if (distance <= 0)
            {
                break;
            }
        }

        return closestNode;
    }
}

public class MapStudioTreeNode
{
    /// <summary>
    /// The bounding box for this node.
    /// </summary>
    public BoundingBox Bounds;

    /// <summary>
    /// The first child of this node.
    /// </summary>
    public MapStudioTreeNode FirstChild;

    /// <summary>
    /// The next sibling of this node.
    /// </summary>
    public MapStudioTreeNode NextSibling;

    /// <summary>
    /// Indices to the parts this node contains.
    /// </summary>
    public List<short> PartIndices;

    public MapStudioTreeNode()
    {
        Bounds = new BoundingBox(Vector3.PositiveInfinity, Vector3.NegativeInfinity);
        PartIndices = [];
    }

    public IMsbTreeNode ToMsbTreeNode(Func<IMsbTreeNode> createNode)
    {
        var node = createNode();
        node.Bounds = new MsbBoundingBox(Bounds.Min, Bounds.Max);
        node.PartIndices = PartIndices;

        if (FirstChild != null)
            node.FirstChild = FirstChild.ToMsbTreeNode(createNode);

        if (NextSibling != null)
            node.NextSibling = NextSibling.ToMsbTreeNode(createNode);

        return node;
    }
}

public record struct PartInfo(BoundingBox Bounds, short Index);