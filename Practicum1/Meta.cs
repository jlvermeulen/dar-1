using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Practicum1
{
    public partial class Main : Form
    {
        // creates the metaDatabase
        public SQLiteConnection CreateMetaDatabase()
        {
            SQLiteConnection.CreateFile("MetaDatabase.sqlite");
            metaDatabaseConnection = new SQLiteConnection("Data Source=MetaDatabase.sqlite;Version=3;");
            metaDatabaseConnection.Open();

            // table with IDF
            string sql = "create table IDF (attribute varchar(20), value varchar(20), IDF real, primary key(attribute, value))";
            SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with QF
            sql = "create table QF (attribute varchar(20), value varchar(20), QF real, primary key(attribute, value))";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with Jaccard
            sql = "create table Jaccard (attribute varchar(20), value_t varchar(20), value_q varchar(20), Jaccard real, primary key(attribute, value_t, value_q))";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with bandwidth idf
            sql = "create table BandwidthIDF (attribute varchar(20), value real, primary key(attribute))";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with bandwidth qf
            sql = "create table BandwidthQF (attribute varchar(20), value real, primary key(attribute))";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();
            return metaDatabaseConnection;
        }

        // fills the metaDatabase
        public void FillMetaDatabase()
        {
            // read the database put in a List<Dictionary<string,string>>
            List<Dictionary<string, string>> database = new List<Dictionary<string, string>>();

            string sql = "select * from autompg";
            SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Dictionary<string, string> row = new Dictionary<string, string>();
                row.Add("id", reader["id"].ToString());
                foreach (string s in attributes)
                    row.Add(s, reader[s].ToString());
                database.Add(row);
            }

            // calculation and insertion of IDF+qf values
            // mpg
            int n = database.Count;
            ParseWorkload();

            intervals = new Dictionary<string, int[]> 
            {
                {"mpg", new int[]{5,50,1}},
                {"cylinders", new int[]{1,20,1}},
                {"displacement", new int[]{50,500,10}},
                {"horsepower", new int[]{30,300,10}},
                {"weight", new int[]{1000,10000,100}},
                {"acceleration", new int[]{5,30,1}},
                {"model_year", new int[]{60,99,1}}
            };

            List<Tuple<int, double>> rqf = new List<Tuple<int, double>>();
            double max = 0;
            new SQLiteCommand("begin", metaDatabaseConnection).ExecuteNonQuery();
            foreach (KeyValuePair<string, int[]> kvp in intervals)
            {
                double hIDF = CalculateIDFBandwidth(database, kvp.Key, n);
                sql = "insert into BandwidthIDF (attribute, value) values ('" + kvp.Key + "', " + hIDF.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();

                double hQF = CalculateQFBandwidth(kvp.Key);
                sql = "insert into BandwidthQF (attribute, value) values ('" + kvp.Key + "', " + hQF.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();

                for (int t = kvp.Value[0]; t <= kvp.Value[1]; t += kvp.Value[2])
                {
                    double idf = CalculateIDFNumeric(database, kvp.Key, n, t, hIDF);
                    sql = "insert into IDF (attribute, value, IDF) values ('" + kvp.Key + "', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                    command = new SQLiteCommand(sql, metaDatabaseConnection);
                    command.ExecuteNonQuery();

                    // qf
                    double qf = CalculateQFNumeric(kvp.Key, t, hQF);
                    rqf.Add(new Tuple<int, double>(t, qf));
                    if (qf > max)
                        max = qf;
                }
                foreach (Tuple<int, double> t in rqf)
                {
                    double qf = max == 0 ? 0 : t.Item2 / max;
                    sql = "insert into QF (attribute, value, QF) values ('" + kvp.Key + "', " + t.Item1 + ", " + qf.ToString(CultureInfo.InvariantCulture) + ")";
                    command = new SQLiteCommand(sql, metaDatabaseConnection);
                    command.ExecuteNonQuery(); 
                }
                rqf.Clear();
                max = 0;
            }

            // origin
            CalculateIDFCategoric(database, "origin", n, metaDatabaseConnection);
            CalculateQFCategoric("origin", metaDatabaseConnection);
            // brand
            CalculateIDFCategoric(database, "brand", n, metaDatabaseConnection);
            CalculateQFCategoric("brand", metaDatabaseConnection);
            // model
            CalculateIDFCategoric(database, "model", n, metaDatabaseConnection);
            CalculateQFCategoric("model", metaDatabaseConnection);
            // type
            CalculateIDFCategoric(database, "type", n, metaDatabaseConnection);
            CalculateQFCategoric("type", metaDatabaseConnection);

            // Jaccard
            foreach (KeyValuePair<string, Dictionary<Tuple<string, string>, int>> kvp1 in workloadInCounts)
            {
                foreach (KeyValuePair<Tuple<string, string>, int> kvp2 in kvp1.Value)
                {
                    int x1 = kvp1.Value[new Tuple<string, string>(kvp2.Key.Item1, kvp2.Key.Item1)];
                    int x2 = kvp1.Value[new Tuple<string, string>(kvp2.Key.Item2, kvp2.Key.Item2)];
                    double jaccard = (double)kvp2.Value / (x1 + x2 - kvp2.Value);
                    sql = "insert into Jaccard (attribute, value_t, value_q, Jaccard) values ('" + kvp1.Key + "', '" + kvp2.Key.Item1 + "', '" + kvp2.Key.Item2 + "'," + jaccard.ToString(CultureInfo.InvariantCulture) + ")";
                    if (kvp2.Key.Item1 != kvp2.Key.Item2)
                        sql += "; insert into Jaccard (attribute, value_t, value_q, Jaccard) values ('" + kvp1.Key + "', '" + kvp2.Key.Item2 + "', '" + kvp2.Key.Item1 + "'," + jaccard.ToString(CultureInfo.InvariantCulture) + ")";
                    command = new SQLiteCommand(sql, metaDatabaseConnection);
                    command.ExecuteNonQuery();

                }
            }
            new SQLiteCommand("end", metaDatabaseConnection).ExecuteNonQuery();
        }

        // calcualte an IDF-value for a numeric attribute
        public double CalculateIDFNumeric(List<Dictionary<string, string>> database, string attribute, int n, int t, double h)
        {
            double[] difference = new double[n];
            int index = 0;
            foreach (Dictionary<string, string> row in database)
                difference[index++] = double.Parse(row[attribute]) - t; // ti-t

            // calculate idf
            double test = Math.Log10(n / difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * (d / h)))));
            return test;
        }

        // Calculate the h value used for IDF calculation.
        public double CalculateIDFBandwidth(List<Dictionary<string, string>> database, string attribute, int n)
        {
            double[] values = new double[n];
            int index = 0;
            foreach (Dictionary<string, string> row in database)
                values[index++] = double.Parse(row[attribute]); // for calculation of h

            //calculate std.dev.
            double average = values.Average();
            double sum = values.Sum(d => (d - average) * (d - average));
            double stdDev = Math.Sqrt(sum / n);
            // calculate h
            double h = 1.06 * stdDev * Math.Pow(n, -0.2);
            return h;
        }

        // calcualte an QF-value for a numeric attribute
        public double CalculateQFNumeric(string attribute, int t, double h)
        {
            if (workloadCounts[attribute].Count == 0)
                return 0;
            Dictionary<string, double> difference = new Dictionary<string, double>();
            int totalCount = 0;
            foreach (KeyValuePair<string, int> row in workloadCounts[attribute])
            {
                totalCount += row.Value;
                difference[row.Key] = double.Parse(row.Key) - t; // ti-t
            }

            // calculate qf
            return difference.Sum(d => workloadCounts[attribute][d.Key] * Math.Pow(Math.E, (-0.5 * (d.Value / h) * (d.Value / h))));
        }

        // Calculate the h value used for QF calculation.
        public double CalculateQFBandwidth(string attribute)
        {
            if (workloadCounts[attribute].Count == 0)
                return 0;
            Dictionary<string, double> values = new Dictionary<string, double>();
            int totalCount = 0;
            foreach (KeyValuePair<string, int> row in workloadCounts[attribute])
            {
                totalCount += row.Value;
                values[row.Key] = double.Parse(row.Key); // for calculation of h
            }

            //calculate std.dev.
            double average = values.Sum(d => d.Value * workloadCounts[attribute][d.Key]) / totalCount;
            double sum = values.Sum(d => (d.Value - average) * (d.Value - average) * workloadCounts[attribute][d.Key]);
            double stdDev = Math.Sqrt(sum / totalCount);
            // calculate h
            double h = 1.06 * stdDev * Math.Pow(totalCount, -0.2);

            return h;
        }

        // calcualte an IDF-value for a categorical attribute
        public void CalculateIDFCategoric(List<Dictionary<string, string>> database, string attribute, int n, SQLiteConnection metaDatabaseConnection)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (Dictionary<string, string> row in database)
            {
                if (counts.ContainsKey(row[attribute]))
                    counts[row[attribute]] += 1;
                else
                    counts.Add(row[attribute], 1);
            }
            foreach (KeyValuePair<string, int> kvp in counts)
            {
                double idf = Math.Log10(n / kvp.Value);
                string sql = "insert into IDF (attribute, value, IDF) values (\"" + attribute + "\", \"" + kvp.Key + "\", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
        }

        // calcualte an QF-value for a categorical attribute
        public void CalculateQFCategoric(string attribute, SQLiteConnection metaDatabaseConnection)
        {
            double max = workloadCounts[attribute].Count == 0 ? 1 : workloadCounts[attribute].Max(d => d.Value);

            foreach (KeyValuePair<string, int> kvp in workloadCounts[attribute])
            {
                double qf = kvp.Value / max;
                string sql = "insert into QF (attribute, value, QF) values ('" + attribute + "', '" + kvp.Key + "', " + qf.ToString(CultureInfo.InvariantCulture) + ")";
                SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }

        }
    }
}
