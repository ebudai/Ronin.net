// Copyright © 2026 Eric Budai

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Compiler;

/// <summary>
///     Tarjan's strongly-connected components. Iterative rather than recursive, because a
///     chain of a thousand nodes each pointing at the next is a thousand stack frames done
///     as one loop instead — the deep-chain case both callers have. Components come back
///     with every node's successors before the node, so processing them in that order
///     visits a node's dependencies before the node itself.
/// </summary>
///
/// <remarks>
///     One walk, shared. The cascade rings and the return-inference groups ask the same
///     question — which nodes lie on a cycle together, and in what dependency order — and
///     two copies of a graph walk drift apart, like two window predicates. Generic over
///     string nodes and their edges, knowing nothing of what they stand for.
/// </remarks>
internal static class Tarjan
{
    internal static List<List<string>> Components(Dictionary<string, HashSet<string>> edges,
                                                  IEnumerable<string> nodes)
    {
        Dictionary<string, int> index = [];
        Dictionary<string, int> low = [];
        HashSet<string> stacked = [];
        Stack<string> component = new();
        Stack<(string Node, IEnumerator<string> Neighbours)> walking = new();
        List<List<string>> components = [];
        var counter = 0;

        foreach (var start in nodes.Order(StringComparer.Ordinal))
        {
            if (index.ContainsKey(start)) continue;

            Open(start);

            while (walking.Count is not 0)
            {
                var (node, neighbours) = walking.Peek();

                if (neighbours.MoveNext())
                {
                    var next = neighbours.Current;

                    // an edge into a component already closed says nothing about
                    // this one, which is the case a back-edge walk conflates
                    if (index.ContainsKey(next) is false) Open(next);
                    else if (stacked.Contains(next)) low[node] = Math.Min(low[node], index[next]);

                    continue;
                }

                walking.Pop();

                // fold this node's low link into its parent's, which is what the
                // return from the recursive call used to do
                if (walking.Count is not 0) low[walking.Peek().Node] = Math.Min(low[walking.Peek().Node], low[node]);

                if (low[node] != index[node]) continue;

                List<string> closed = [];
                string member;

                do
                {
                    member = component.Pop();
                    stacked.Remove(member);
                    closed.Add(member);
                }
                while (member != node);

                components.Add(closed);
            }
        }

        return components;

        void Open(string node)
        {
            index[node] = low[node] = counter++;
            component.Push(node);
            stacked.Add(node);
            walking.Push((node, edges[node].Order(StringComparer.Ordinal).GetEnumerator()));
        }
    }
}
