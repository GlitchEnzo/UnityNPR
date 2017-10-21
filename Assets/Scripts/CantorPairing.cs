using UnityEngine;
using System.Collections.Generic;

public class CantorPairing : MonoBehaviour
{
    public int start = 1;
    public int end = 10;

    private struct Pair
    {
        public int a;
        public int b;

        public Pair(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    void Start()
    {
        Dictionary<int, Pair> cached = new Dictionary<int, Pair>(end * 2);

        for (int i = start; i <= end; i++)
        {
            for (int j = i + 1; j <= end; j++)
            {
                int cantor = (int)(0.5f * (i + j) * (i + j + 1) + j);
                //Debug.Log(cantor);

                if (cached.ContainsKey(cantor))
                {
                    Debug.LogFormat("Already contains {4}: {0}^{1}.  Identical = {2}^{3}", cached[cantor].a, cached[cantor].b, i, j, cantor);
                }
                else
                {
                    cached.Add(cantor, new Pair(i, j));
                }
            }
        }

        Debug.LogFormat("Count = {0}", cached.Count);
    }
}
