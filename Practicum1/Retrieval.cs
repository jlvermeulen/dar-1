using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Windows.Forms;

namespace Practicum1
{
    public partial class Main : Form
    {
        // Read necessary data from the metadatabase
        private void ReadMetaData(Dictionary<string, double> IDFs, Dictionary<string, double> hIDFs, Dictionary<string, double> QFs, Dictionary<string, double> hQFs, Dictionary<string, string> roundedQuery)
        {
            string sql;
            SQLiteCommand command;
            SQLiteDataReader reader;

            foreach (KeyValuePair<string, string> kvp in roundedQuery)
            {
                string template = "' AND value = {0}";
                if (true)//check op categorisch
                    template = "' AND value = '{0}'";
                if (kvp.Key == "k")
                    continue;

                sql = "select IDF from IDF WHERE attribute = '" + kvp.Key + string.Format(template, kvp.Value) + "";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                reader = command.ExecuteReader();
                reader.Read();

                IDFs[kvp.Key] = (double)reader["IDF"];

                sql = "select value from BandwidthIDF WHERE attribute = '" + kvp.Key + "'";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                reader = command.ExecuteReader();
                if (reader.Read())
                    hIDFs[kvp.Key] = (double)reader["value"];
                else
                    hIDFs[kvp.Key] = -1;

                sql = "select QF from QF WHERE attribute = '" + kvp.Key + string.Format(template, kvp.Value) + "";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                reader = command.ExecuteReader();
                if (reader.Read())
                    QFs[kvp.Key] = (double)reader["QF"];
                else
                    QFs[kvp.Key] = 1;

                sql = "select value from BandwidthQF WHERE attribute = '" + kvp.Key + "'";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                reader = command.ExecuteReader();
                if (reader.Read())
                    hQFs[kvp.Key] = (double)reader["value"];
                else
                    hQFs[kvp.Key] = -1;
            }

        }

        // Calculate the scores.
        private List<Tuple<long, double>>[] CalculateScores(Dictionary<string, double> IDFs, Dictionary<string, double> hIDFs, Dictionary<string, double> QFs, Dictionary<string, double> hQFs, Dictionary<string, string> roundedQuery)
        {
            List<Tuple<long, double>>[] results = new List<Tuple<long, double>>[(roundedQuery.Count - 1) * 2];
            for (int i = 0; i < results.Length; i++)
                results[i] = new List<Tuple<long, double>>();

            string sql = "select * from autompg";
            SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                int i = 0;
                foreach (KeyValuePair<string, string> kvp in roundedQuery)
                {
                    if (kvp.Key == "k")
                        continue;
                    string value = string.Format(CultureInfo.InvariantCulture, "{0}", reader[kvp.Key]);

                    double t = -1, q = -1, h = -1, idfScore = -1;
                    // string querryValue = kvp.Value;
                    if (hIDFs[kvp.Key] == -1)
                    { // categorisch
                        idfScore = IDFs[kvp.Key];
                    }
                    else
                    {
                        //  niet  categorisch
                        t = double.Parse(value, CultureInfo.InvariantCulture);
                        q = double.Parse(kvp.Value, CultureInfo.InvariantCulture);
                        h = hIDFs[kvp.Key];

                        idfScore = Math.Pow(Math.E, -0.5 * ((t - q) / h) * ((t - q) / h)) * IDFs[kvp.Key];
                    }

                    results[i++].Add(new Tuple<long, double>((long)reader["id"], idfScore));


                    string getJaccardString;
                    // calculation of jaccards
                    if (hIDFs[kvp.Key] == -1)
                        getJaccardString = "select Jaccard from Jaccard WHERE attribute = '" + kvp.Key + "' AND value_q = '" + kvp.Value + "' AND value_t = '" + value + "'";
                    else
                        getJaccardString = "select Jaccard from Jaccard WHERE attribute = '" + kvp.Key + "' AND value_q = " + q.ToString(CultureInfo.InvariantCulture) + " AND value_t = " + t.ToString(CultureInfo.InvariantCulture) + "";
                    SQLiteCommand getJaccardCommand = new SQLiteCommand(getJaccardString, metaDatabaseConnection);
                    SQLiteDataReader jaccardReader = getJaccardCommand.ExecuteReader();

                    // sets default to 1 if the same, 0 if not
                    double jaccard = value == kvp.Value ? 1 : 0;
                    if (jaccardReader.Read())
                    {
                        // if there is an enrty replace jaccard value with it
                        jaccard = (double)jaccardReader["Jaccard"];
                        // makes sure that if you search bmw you get bmw's first
                        jaccard += kvp.Value == value ? 0.01 : 0;
                    }
                    if (intervals.ContainsKey(kvp.Key))
                    {
                        h = hQFs[kvp.Key];
                        jaccard = Math.Pow(Math.E, -0.5 * ((t - q) / h) * ((t - q) / h)) * QFs[kvp.Key];
                    }
                    else
                    {
                        jaccard = jaccard * QFs[kvp.Key];
                    }
                    results[i++].Add(new Tuple<long, double>((long)reader["id"], jaccard));
                }
            }
            return results;

        }

        // Prepare input for topK algoritm + call topK.
        Tuple<long, double>[] CalculateTopK(Dictionary<string, string> roundedQuery, List<Tuple<long, double>>[] results)
        {
            string sql;
            SQLiteCommand command;
            SQLiteDataReader reader;

            long[][] keys = new long[results.Length][];
            Dictionary<long, double>[] values = new Dictionary<long, double>[results.Length];
            int j = 0;

            foreach (List<Tuple<long, double>> list in results)
            {
                list.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                keys[j] = new long[list.Count];
                values[j] = new Dictionary<long, double>();
                for (int l = 0; l < list.Count; l++)
                {
                    keys[j][l] = list[l].Item1;
                    values[j][list[l].Item1] = list[l].Item2;
                }
                j++;
            }
            return TopK.Get(keys, values, int.Parse(roundedQuery["k"]));
        }
    }
}
