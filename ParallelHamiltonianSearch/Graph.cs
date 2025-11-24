using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelHamiltonianSearch;

public sealed class Graph
{
    private readonly List<int>[] _adj;

    public int VertexCount => _adj.Length;

    public Graph(int n)
    {
        _adj = new List<int>[n];
        for (int i = 0; i < n; i++)
            _adj[i] = new List<int>();
    }

    public void AddEdge(int from, int to)
    {
        _adj[from].Add(to);
    }

    public IReadOnlyList<int> OutNeighbors(int v) => _adj[v];

    public bool HasEdge(int from, int to) => _adj[from].Contains(to);

    public static Graph Random(int n, double edgeProb, int seed)
    {
        var g = new Graph(n);
        var rng = new Random(seed);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                if (rng.NextDouble() < edgeProb)
                    g.AddEdge(i, j);
            }
        }
        return g;
    }
}