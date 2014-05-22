using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Practicum1
{
    public partial class Main : Form
    {
        Dictionary<string, int[]> intervals;

        Dictionary<string, Dictionary<string, int>> workloadCounts;
        Dictionary<string, Dictionary<Tuple<string, string>, int>> workloadInCounts;
        string[] attributes = new string[] { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };

        SQLiteConnection metaDatabaseConnection;
        SQLiteConnection databaseConnection;

        public Main()
        {
            this.WindowState = FormWindowState.Maximized;
            InitializeComponent();
            workloadCounts = new Dictionary<string, Dictionary<string, int>>();
            workloadInCounts = new Dictionary<string, Dictionary<Tuple<string, string>, int>>();
            foreach (string s in attributes)
            {
                workloadInCounts[s] = new Dictionary<Tuple<string, string>, int>();
                workloadCounts[s] = new Dictionary<string, int>();
            }
            // create the MetaDatabase
            SQLiteConnection metaDatabaseConnection = CreateMetaDatabase();

            // make connection to the autompg database
            SQLiteConnection.CreateFile("autompg.sqlite");
            databaseConnection = new SQLiteConnection("Data Source=autompg.sqlite;Version=3;");
            databaseConnection.Open();
            ParseTable(databaseConnection);

            // fill the meta database using the 2 databases and the workload file
            FillMetaDatabase();
        }

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

                for (int t = kvp.Value[0]; t < kvp.Value[1]; t += kvp.Value[2])
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
            CalculateQFNumeric("origin", metaDatabaseConnection);
            // brand
            CalculateIDFCategoric(database, "brand", n, metaDatabaseConnection);
            CalculateQFNumeric("brand", metaDatabaseConnection);
            // model
            CalculateIDFCategoric(database, "model", n, metaDatabaseConnection);
            CalculateQFNumeric("model", metaDatabaseConnection);
            // type
            CalculateIDFCategoric(database, "type", n, metaDatabaseConnection);
            CalculateQFNumeric("type", metaDatabaseConnection);

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

        public void CalculateQFNumeric(string attribute, SQLiteConnection metaDatabaseConnection)
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

        public void ParseTable(SQLiteConnection databaseConnection)
        {
            new SQLiteCommand("begin", databaseConnection).ExecuteNonQuery();
            // read and parse autompg.sql
            string strCommand = File.ReadAllText("autompg.sql").ToLower();
            SQLiteCommand command = databaseConnection.CreateCommand();
            command.CommandText = strCommand;
            command.ExecuteNonQuery();
            new SQLiteCommand("end", databaseConnection).ExecuteNonQuery();
        }

        public void ParseWorkload()
        {
            int nQuerries, times;
            StreamReader stream = new StreamReader("workload.txt");
            string s = stream.ReadLine();
            nQuerries = int.Parse(s.Split(' ')[0]);
            s = stream.ReadLine();
            while ((s = stream.ReadLine()) != "")
            {
                times = int.Parse(s.Split(' ')[0]);
                string where = s.Split(new string[] { "WHERE" }, StringSplitOptions.None)[1];
                string[] statements = where.Split(new string[] { "AND" }, StringSplitOptions.None);
                for (int i = 0; i < statements.Length; i++)
                {

                    if (!statements[i].Contains("IN"))
                    {
                        string[] ss = statements[i].Split('=');
                        string attribute = ss[0].Trim();
                        string value = ss[1].Trim(" '".ToCharArray());

                        if (workloadCounts[attribute].ContainsKey(value))
                            workloadCounts[attribute][value] += times;
                        else
                            workloadCounts[attribute][value] = times;
                    }
                    else
                    {
                        string[] ss = statements[i].Split(new string[] { "IN" }, StringSplitOptions.None);
                        string attribute = ss[0].Trim();
                        string value = ss[1].Trim("() ".ToCharArray());
                        string[] values = value.Split(',');
                        for (int x = 0; x < values.Length; x++)
                            values[x] = values[x].Trim("' ".ToCharArray());
                        Array.Sort(values);
                        for (int ii = 0; ii < values.Length; ii++)
                        {
                            for (int j = ii; j < values.Length; j++)
                            {
                                Tuple<string, string> t1 = new Tuple<string, string>(values[ii], values[j]);
                                //Tuple<string, string> t2 = new Tuple<string, string>(values[j], values[ii]);
                                if (workloadInCounts[attribute].ContainsKey(t1))
                                {
                                    workloadInCounts[attribute][t1] += times;
                                    // workloadInCounts[attribute][t2] += times;
                                }
                                else
                                {
                                    workloadInCounts[attribute][t1] = times;
                                    // workloadInCounts[attribute][t2] = times;
                                }
                            }

                            // also add it to the workloadcounts
                            // Tuple<string, string> t = new Tuple<string, string>(attribute, values[ii]);

                            if (workloadCounts[attribute].ContainsKey(values[ii]))
                                workloadCounts[attribute][values[ii]] += times;
                            else
                                workloadCounts[attribute][values[ii]] = times;
                        }
                    }
                }
            }
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            
            Dictionary<string, string> query = new Dictionary<string,string>();
            resultViewDataGrid.Rows.Clear();
            try
            {

                query = new Dictionary<string, string> { { "k", "10" } };

                string[] input = inputTextBox.Text.ToLower().Split(',');
                foreach (string s in input)
                {
                    string[] pair = s.Split('=');
                    query[pair[0].Trim()] = pair[1].Trim(" '".ToCharArray());
                }
                if (query.Count == 1)
                {
                    MessageBox.Show("Please specify at least 1 query parameter.");
                    return;
                }

            }
            catch (Exception)
            {
                MessageBox.Show("The query could not be processed due to incorrect syntax");
                return;
            }

            NewMethod(query);
            return;
        }

        private void NewMethod(Dictionary<string, string> query)
        {

            Dictionary<string, double> IDFs = new Dictionary<string, double>();
            Dictionary<string, double> hIDFs = new Dictionary<string, double>();
            Dictionary<string, double> QFs = new Dictionary<string, double>();
            Dictionary<string, double> hQFs = new Dictionary<string, double>();

            string sql;
            SQLiteCommand command;
            SQLiteDataReader reader;
            string queryValue = "";

            Dictionary<string, string> roundedQuery = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in query)
            {
                string template = "' AND value = {0}";
                if (true)//check op categorisch
                    template = "' AND value = '{0}'"; // gebruiken
                if (kvp.Key == "k")
                    continue;

                queryValue = kvp.Value;

                // rounding
                if (intervals.ContainsKey(kvp.Key))
                {
                    double q = double.Parse(queryValue, CultureInfo.InvariantCulture);

                    if (q < intervals[kvp.Key][0])
                        q = intervals[kvp.Key][0];
                    if (q > intervals[kvp.Key][1])
                        q = intervals[kvp.Key][1];
                    if (q % intervals[kvp.Key][2] != 0)
                        if (q % intervals[kvp.Key][2] >= intervals[kvp.Key][2] / 2.0)
                            q += intervals[kvp.Key][2] - (q % intervals[kvp.Key][2]);
                        else
                            q -= q % intervals[kvp.Key][2];
                    queryValue = q.ToString(CultureInfo.InvariantCulture);
                }

                roundedQuery[kvp.Key] = queryValue;


                sql = "select IDF from IDF WHERE attribute = '" + kvp.Key + string.Format(template, queryValue) + "";
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

                sql = "select QF from QF WHERE attribute = '" + kvp.Key + string.Format(template, queryValue) + "";
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

            List<Tuple<long, double>>[] results = new List<Tuple<long, double>>[(query.Count - 1) * 2];
            for (int i = 0; i < results.Length; i++)
                results[i] = new List<Tuple<long, double>>();

            sql = "select * from autompg";
            command = new SQLiteCommand(sql, databaseConnection);
            reader = command.ExecuteReader();
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
                        getJaccardString = "select Jaccard from Jaccard WHERE attribute = '" + kvp.Key + "' AND value_q = '" + queryValue + "' AND value_t = '" + value + "'";
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
            Tuple<long, double>[] topK = TopK.Get(keys, values, int.Parse(query["k"]));
            //get results from db + print on screen
            foreach (Tuple<long, double> ld in topK)
            {
                sql = "select * from autompg where id = " + ld.Item1;
                command = new SQLiteCommand(sql, databaseConnection);
                reader = command.ExecuteReader();
                reader.Read();
                resultViewDataGrid.Rows.Add(ld.Item2.ToString(CultureInfo.InvariantCulture), reader["mpg"], reader["cylinders"], reader["displacement"], reader["horsepower"], reader["weight"], reader["acceleration"], reader["model_year"], reader["origin"], reader["brand"], reader["model"], reader["type"]);
            }
            resultViewDataGrid.AutoResizeColumns();
        }

    }
}
