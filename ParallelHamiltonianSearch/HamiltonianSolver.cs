using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelHamiltonianSearch;

namespace ParallelHamiltonianSearch;

public sealed class HamiltonianSolver // sequential + parallel
{
    private readonly Graph _g;
    private readonly int _n;
    private readonly int _start;

    private volatile bool _found;
    private int[]? _solution;
    private readonly object _lock = new();

    public HamiltonianSolver(Graph g, int start = 0)
    {
        _g = g;
        _n = g.VertexCount;
        _start = start;
    }

    public int[]? Solution => _solution;

    // sequential DFS
    public int[]? FindSequential()
    {
        var path = new int[_n];
        var visited = new bool[_n];
        path[0] = _start;
        visited[_start] = true;
        if (DfsSeq(_start, 1, path, visited))
            return _solution;
        return null;
    }

    private bool DfsSeq(int v, int depth, int[] path, bool[] visited)
    {
        if (depth == _n)
        {
            if (_g.HasEdge(v, _start))
            {
                var cycle = new int[_n + 1];
                Array.Copy(path, cycle, _n);
                cycle[_n] = _start;
                lock (_lock)
                {
                    if (!_found)
                    {
                        _found = true;
                        _solution = cycle;
                    }
                }
                return true;
            }
            return false;
        }

        foreach (var u in _g.OutNeighbors(v))
        {
            if (visited[u]) continue;
            visited[u] = true;
            path[depth] = u;
            if (DfsSeq(u, depth + 1, path, visited))
                return true;
            visited[u] = false;
        }

        return false;
    }

    public int[]? FindParallel(int totalThreads)
    {
        _found = false;
        _solution = null;

        var path = new int[_n];
        var visited = new bool[_n];
        path[0] = _start;
        visited[_start] = true;

        SearchParallel(_start, 1, path, visited, totalThreads);
        return _solution;
    }

    private bool SearchParallel(int v, int depth, int[] path, bool[] visited, int threads)
    {
        if (_found) return true;

        if (threads <= 1 || depth >= _n - 2)
        {
            return DfsSeq(v, depth, (int[])path.Clone(), (bool[])visited.Clone());
        }

        var candidates = _g.OutNeighbors(v).Where(u => !visited[u]).ToList();
        if (candidates.Count == 0) return false;

        if (candidates.Count == 1)
        {
            int u = candidates[0];
            var visitedCopy = (bool[])visited.Clone();
            var pathCopy = (int[])path.Clone();
            visitedCopy[u] = true;
            pathCopy[depth] = u;
            return SearchParallel(u, depth + 1, pathCopy, visitedCopy, threads);
        }

        int c = candidates.Count;
        int baseThreads = threads / c;
        int extra = threads % c;

        var tasks = new List<Task<bool>>(c);

        for (int i = 0; i < c; i++)
        {
            int u = candidates[i];
            int threadsForChild = baseThreads + (i < extra ? 1 : 0);
            if (threadsForChild <= 0) threadsForChild = 1;

            var visitedCopy = (bool[])visited.Clone();
            var pathCopy = (int[])path.Clone();
            visitedCopy[u] = true;
            pathCopy[depth] = u;

            if (i == 0)
            {
                tasks.Add(Task.Run(() =>
                    SearchParallel(u, depth + 1, pathCopy, visitedCopy, threadsForChild)));
            }
            else
            {
                tasks.Add(Task.Run(() =>
                    SearchParallel(u, depth + 1, pathCopy, visitedCopy, threadsForChild)));
            }
        }

        while (tasks.Count > 0)
        {
            int idx = Task.WaitAny(tasks.ToArray());
            if (tasks[idx].Result)
                return true;
            tasks.RemoveAt(idx);
        }

        return false;
    }
}