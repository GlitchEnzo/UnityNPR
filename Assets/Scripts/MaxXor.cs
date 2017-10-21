using System.Collections.Generic;
using UnityEngine;

public class MaxXor : MonoBehaviour
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
    
	void Start ()
    {
        Dictionary<int, Pair> cached = new Dictionary<int, Pair>(end * 2);

        for (int pow = 1; pow <= 32; pow++)
        {
            int powerOfTwo = (int)System.Math.Pow(2, pow);
            if (powerOfTwo > end)
            {
                Debug.LogFormat("Predicted max = {0}", powerOfTwo - 1);
                break;
            }
        }

        int max = end;
        int maxI = start - 1;
        int maxJ = maxI;

	    for (int i = start; i <= end; i++)
        {
            for (int j = i + 1; j <= end; j++)
            {
                int xor = i ^ j;
                if (xor > max)
                {
                    max = xor;
                    maxI = i;
                    maxJ = j;
                }

                if (cached.ContainsKey(xor))
                {
                    Debug.LogFormat("Already contains {4}: {0}^{1}.  Identical = {2}^{3}", cached[xor].a, cached[xor].b, i, j, xor);
                }
                else
                {
                    cached.Add(xor, new Pair(i, j));
                }
                
            }
        }

        Debug.LogFormat("Max ({1} ^ {2}) = {0}", max, maxI, maxJ);
	}
}
