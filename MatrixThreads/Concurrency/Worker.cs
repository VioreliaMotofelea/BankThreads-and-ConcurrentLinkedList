using System;
using System.Collections.Generic;

namespace MatrixThreads.Concurrency;

public sealed class Worker
{
    public int Id { get; }
    private readonly int[,] _A, _B, _C;
    private readonly List<(int i, int j)> _cells;
    private readonly bool _verbose;

    public Worker(int id, int[,] A, int[,] B, int[,] C, List<(int, int)> cells, bool verbose)
    {
        Id = id; _A = A; _B = B; _C = C; _cells = cells; _verbose = verbose;
    }

    public void Run()
    {
        foreach (var (i, j) in _cells)
        {
            _C[i, j] = ComputeCell(i, j);
        }
    }

    private int ComputeCell(int i, int j)
    {
        int n = _A.GetLength(1);
        int sum = 0;
        for (int k = 0; k < n; k++)
            sum += _A[i, k] * _B[k, j];
        if (_verbose)
            Console.WriteLine($"cell({i},{j}) by thread {Id}");
        return sum;
    }
}