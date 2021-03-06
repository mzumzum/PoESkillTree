﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PoESkillTree.TreeGenerator.Algorithm.Model;

namespace PoESkillTree.TreeGenerator.Algorithm.SteinerReductions
{
    /// <summary>
    /// Base class for reduction tests on steiner tree problem instances.
    /// 
    /// Provides access to node states and other data used by concrete classes and contains methods used
    /// by multiple reduction tests.
    /// </summary>
    public abstract class SteinerReduction
    {
        /// <summary>
        /// Counts the number of times <see cref="RunTest"/> was called.
        /// </summary>
        private int _iteration;

        private readonly IData _data;

        /// <summary>
        /// Gets the set of edges currently in the search space.
        /// </summary>
        protected GraphEdgeSet EdgeSet
            => _data.EdgeSet;

        /// <summary>
        /// Gets the number of nodes currently in the search space. The actual nodes are described as
        /// search space indices from 0 (inclusive) to SearchSpaceSize (exlusive).
        /// </summary>
        protected int SearchSpaceSize
            => _data.DistanceCalculator.CacheSize;

        /// <summary>
        /// Gets a lookup for the distances between the nodes currently in the search space.
        /// </summary>
        protected DistanceLookup DistanceLookup
            => _data.DistanceCalculator.DistanceLookup;

        /// <summary>
        /// Gets a lookup for the bottleneck Steiner distances between the nodes currently in the search space.
        /// </summary>
        protected DistanceLookup SMatrix
            => _data.SMatrix;

        /// <summary>
        /// Gets the search space index of the start node which is used for tree generation and connection tests.
        /// </summary>
        protected int StartNodeIndex
            => _data.StartNodeIndex;

        /// <summary>
        /// Gets the identifier of this reduction test.
        /// </summary>
        protected abstract string TestId { get; }

        /// <summary>
        /// Gets the object holding information about node states.
        /// </summary>
        protected INodeStates NodeStates { get; private set; }

        /// <summary>
        /// Executes this reduction test.
        /// </summary>
        protected abstract int ExecuteTest();

        /// <summary>
        /// Gets or sets whether this reduction test is enabled. If it is not enabled, <see cref="RunTest"/>
        /// is a no-op.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Creates a reduction test instance which is initially enabled.
        /// </summary>
        protected SteinerReduction(INodeStates nodeStates, IData data)
        {
            _data = data;
            NodeStates = nodeStates;
            IsEnabled = true;
        }

        /// <summary>
        /// Runs this reduction test once.
        /// </summary>
        /// <param name="edgeElims">The number of edges eliminated by this test run are added to this parameter.</param>
        /// <param name="nodeElims">The number of nodes eliminated by this test run are added to this parameter.</param>
        /// <returns>True iff this test run eliminated edges.</returns>
        public bool RunTest(ref int edgeElims, ref int nodeElims)
        {
            if (!IsEnabled)
            {
                return false;
            }
            var edgeCountBefore = EdgeSet.Count;
            var removedNodes = ExecuteTest();
            Debug.WriteLine("{0} Test #{1}:", TestId, ++_iteration);
            if (removedNodes > 0)
                Debug.WriteLine("   removed nodes: " + removedNodes);
            if (edgeCountBefore - EdgeSet.Count > 0)
                Debug.WriteLine("   removed edges: " + (edgeCountBefore - EdgeSet.Count));
            edgeElims += edgeCountBefore - EdgeSet.Count;
            nodeElims += removedNodes;
            return edgeCountBefore - EdgeSet.Count > 0;
        }

        /// <summary>
        /// Merges the node x into the fixed target node into.
        /// 
        /// Edges between these nodes, edges to x and related distance information is updated.
        /// 
        /// x is marked as to be removed from the search space. If x was the start node, into
        /// will now be the start node.
        /// </summary>
        /// <returns>All neighbors of x before merging. These are the nodes that had their adjacency
        /// information changed.</returns>
        protected IEnumerable<int> MergeInto(int x, int into)
        {
            if (!NodeStates.IsFixedTarget(into))
                throw new ArgumentException("Nodes can only be merged into fixed target nodes", "into");

            _data.DistanceCalculator.IndexToNode(into).MergeWith(_data.DistanceCalculator.IndexToNode(x), _data.DistanceCalculator.GetShortestPath(x, into));
            _data.DistanceCalculator.MergeInto(x, into);

            EdgeSet.Remove(x, into);
            var intoNeighbors = EdgeSet.NeighborsOf(into);
            var xNeighbors = EdgeSet.NeighborsOf(x);
            var neighbors = intoNeighbors.Union(xNeighbors);
            foreach (var neighbor in xNeighbors)
            {
                EdgeSet.Remove(x, neighbor);
            }
            foreach (var neighbor in neighbors)
            {
                EdgeSet.Add(into, neighbor, _data.DistanceCalculator[into, neighbor]);
            }

            if (StartNodeIndex == x)
            {
                _data.StartNodeIndex = into;
            }

            NodeStates.MarkNodeAsRemoved(x);

            return xNeighbors;
        }
        
    }
}