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
        //does everything
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

        // Parse the sql-statements and create the database.
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

        // Parse and process the workload.
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
                                if (workloadInCounts[attribute].ContainsKey(t1))
                                {
                                    workloadInCounts[attribute][t1] += times;
                                }
                                else
                                {
                                    workloadInCounts[attribute][t1] = times;
                                }
                            }
                            // also add it to the workloadcounts
                            if (workloadCounts[attribute].ContainsKey(values[ii]))
                                workloadCounts[attribute][values[ii]] += times;
                            else
                                workloadCounts[attribute][values[ii]] = times;
                        }
                    }
                }
            }
        }

        // Check and process the query.
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
                MessageBox.Show("The query could not be processed due to incorrect syntax.");
                return;
            }

            foreach(string key in query.Keys)
                if (!attributes.Contains(key) && key != "k")
                {
                    MessageBox.Show("There is no attribute named '" + key + "'.");
                    return;
                }

            Dictionary<string, double> IDFs = new Dictionary<string, double>(), hIDFs = new Dictionary<string, double>(),
                QFs = new Dictionary<string, double>(), hQFs = new Dictionary<string, double>();
            Dictionary<string, string> roundedQuery = RoundQuery(query);

            ReadMetaData(IDFs, hIDFs, QFs, hQFs, roundedQuery);
            List<Tuple<long,double>>[] scores = CalculateScores(IDFs, hIDFs, QFs, hQFs, roundedQuery);
            Tuple<long, double>[] topKs = CalculateTopK(roundedQuery, scores);
            PrintTopK(topKs);
        }

        // Round the numeric values in the query
        private Dictionary<string, string> RoundQuery(Dictionary<string, string> query)
        {
            Dictionary<string, string> roundedQuery = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> kvp in query)
            {
                if (kvp.Key == "k")
                {
                    roundedQuery[kvp.Key] = kvp.Value;
                    continue;
                }

                string queryValue = kvp.Value;

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
            }
            return roundedQuery;
        }

        // print the results
        private void PrintTopK(Tuple<long, double>[] topK)
        {
            string sql;
            SQLiteDataReader reader;
            SQLiteCommand command;
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
