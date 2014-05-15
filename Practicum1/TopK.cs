using System;
using System.Collections.Generic;

namespace Practicum1
{
    public static class TopK
    {
        public static int[] Get(int[][] keys, Dictionary<int, decimal>[] values, int k)
        {
            List<KeyValuePair<int, decimal>> topK = new List<KeyValuePair<int, decimal>>();
            Dictionary<int, decimal> buffer = new Dictionary<int, decimal>();
            HashSet<int> done = new HashSet<int>();
            decimal[] max = new decimal[values.Length];
            decimal thresh;
            
            int pointer = 0;
            while (topK.Count < k && pointer < values[0].Count)
            {
                thresh = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (!done.Contains(keys[i][pointer]))
                    {
                        decimal x = 0;
                        for (int j = 0; j < values.Length; j++)
                            x += values[j][keys[i][pointer]];
                        buffer[keys[i][pointer]] = x;
                        done.Add(keys[i][pointer]);
                    }

                    max[i] = values[i][keys[i][pointer]];
                    thresh += max[i];
                }

                Dictionary<int, decimal> newBuffer = new Dictionary<int, decimal>();
                foreach (KeyValuePair<int, decimal> kvp in buffer)
                {
                    if (kvp.Value >= thresh)
                        topK.Add(kvp);
                    else
                        newBuffer[kvp.Key] = kvp.Value;
                }
                buffer = newBuffer;

                pointer++;
            }

            topK.Sort((a, b) => { return -a.Value.CompareTo(b.Value); });

            int[] result = new int[k];
            for (int i = 0; i < k; i++)
                result[i] = topK[i].Key;

            return result;
        }
    }
}