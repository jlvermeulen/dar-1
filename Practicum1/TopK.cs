using System;
using System.Collections.Generic;

namespace Practicum1
{
    public static class TopK
    {
        public static Tuple<long,double>[] Get(long[][] keys, Dictionary<long, double>[] values, int k)
        {
            List<KeyValuePair<long, double>> topK = new List<KeyValuePair<long, double>>();
            Dictionary<long, double> buffer = new Dictionary<long, double>();
            HashSet<long> done = new HashSet<long>();
            double[] max = new double[values.Length];
            double thresh;
            
            int pointer = 0;
            while (topK.Count < k && pointer < values[0].Count)
            {
                thresh = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (!done.Contains(keys[i][pointer]))
                    {
                        double x = 0;
                        for (int j = 0; j < values.Length; j++)
                            x += values[j][keys[i][pointer]];
                        buffer[keys[i][pointer]] = x;
                        done.Add(keys[i][pointer]);
                    }

                    max[i] = values[i][keys[i][pointer]];
                    thresh += max[i];
                }

                Dictionary<long, double> newBuffer = new Dictionary<long, double>();
                foreach (KeyValuePair<long, double> kvp in buffer)
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

            Tuple<long,double>[] result = new Tuple<long,double>[k];
            for (int i = 0; i < k; i++)
                result[i] = new Tuple<long, double>(topK[i].Key, topK[i].Value);

            return result;
        }
    }
}