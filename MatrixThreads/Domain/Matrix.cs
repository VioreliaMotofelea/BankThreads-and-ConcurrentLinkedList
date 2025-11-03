using System;

namespace MatrixThreads.Domain;

public static class Matrix
{
    public static int[,] Random(int rows, int cols, int seed = 123)
    {
        var rnd = new Random(seed);
        var m = new int[rows, cols];
        for (int i = 0; i < rows; i++)
        for (int j = 0; j < cols; j++)
            m[i, j] = rnd.Next(1, 8);
        return m;
    }

    public static int[,] MultiplyReference(int[,] A, int[,] B)
    {
        int m = A.GetLength(0);
        int n = A.GetLength(1);
        int p = B.GetLength(1);
        var C = new int[m, p];

        for (int i = 0; i < m; i++)
        for (int j = 0; j < p; j++)
        {
            int s = 0;
            for (int k = 0; k < n; k++)
                s += A[i, k] * B[k, j];
            C[i, j] = s;
        }

        return C;
    }

    public static bool AreEqual(int[,] A, int[,] B)
    {
        if (A.GetLength(0) != B.GetLength(0) || A.GetLength(1) != B.GetLength(1))
            return false;

        for (int i = 0; i < A.GetLength(0); i++)
        for (int j = 0; j < A.GetLength(1); j++)
            if (A[i, j] != B[i, j])
                return false;

        return true;
    }
}