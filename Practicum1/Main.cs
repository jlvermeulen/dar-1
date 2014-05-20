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
        Dictionary<string, Dictionary<string, int>> workloadCounts;
        Dictionary<string, Dictionary<Tuple<string, string>, int>> workloadInCounts;
        string[] attributes = new string[] { "mpg", "cylinders", "displacement", "horsepower", "weight", "acceleration", "model_year", "origin", "brand", "model", "type" };
        public Main()
        {
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
            SQLiteConnection databaseConnection = new SQLiteConnection("Data Source=autompg.sqlite;Version=3;");
            databaseConnection.Open();
            ParseTable(databaseConnection);

            // fill the meta database using the 2 databases and the workload file
            FillMetaDatabase(metaDatabaseConnection, databaseConnection);  
        

           
           // string sql = "create table highscores (name varchar(20), score int)";
           // SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
           // command.ExecuteNonQuery();
            

           /* sql = "insert into highscores (name, score) values ('Me', 3000)";
           / command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('Myself', 6000)";
            command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('And I', 9001)";
            command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();*/

          //  string sql = "select * from autompg";
           // SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
           // SQLiteDataReader reader = command.ExecuteReader();
           // while (reader.Read())
             //   label1.Text += "Model: " + reader["model"] + "\tBrand: " + reader["brand"] + '\n';

        }

        public SQLiteConnection CreateMetaDatabase()
        {
            SQLiteConnection.CreateFile("MetaDatabase.sqlite");
            SQLiteConnection metaDatabaseConnection;
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
            return metaDatabaseConnection;
        }

        public void FillMetaDatabase(SQLiteConnection metaDatabaseConnection, SQLiteConnection databaseConnection)
        {
            // read the database put in a List<Dictionary<string,string>>
            List<Dictionary<string, string>> database = new List<Dictionary<string,string>>();
           
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

            Dictionary<string, int[]> intervals = new Dictionary<string, int[]> 
            {
                {"mpg", new int[]{5,50,1}},
                {"cylinders", new int[]{1,20,1}},
                {"displacement", new int[]{50,500,10}},
                {"horsepower", new int[]{30,300,10}},
                {"weight", new int[]{1000,10000,100}},
                {"acceleration", new int[]{5,30,1}},
                {"model_year", new int[]{60,99,1}}
            };

            List<Tuple<int, decimal>> rqf = new List<Tuple<int, decimal>>();
            decimal max = 0;
            new SQLiteCommand("begin", metaDatabaseConnection).ExecuteNonQuery();
            foreach (KeyValuePair<string, int[]> kvp in intervals)
            {
                for (int t = kvp.Value[0]; t <kvp.Value[1]; t+=kvp.Value[2])
                {
                    decimal idf = CalculateIDFNumeric(database, kvp.Key, n, t);
                    sql = "insert into IDF (attribute, value, IDF) values ('"+kvp.Key+"', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                    command = new SQLiteCommand(sql, metaDatabaseConnection);
                    command.ExecuteNonQuery();
                    // qf                
                    decimal qf = CalculateQFNumeric(kvp.Key, n, t);
                    rqf.Add(new Tuple<int, decimal>(t, qf));
                    if (qf > max)
                        max = qf;
                }
                foreach (Tuple<int, decimal> t in rqf)
                {
                    decimal qf = max==0?0:t.Item2/max;
                    sql = "insert into QF (attribute, value, QF) values ('"+kvp.Key+"', " + t.Item1 + ", " + qf.ToString(CultureInfo.InvariantCulture) + ")";
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
            foreach (KeyValuePair<string, Dictionary<Tuple<string,string>,int>> kvp1 in workloadInCounts)
            {
                foreach (KeyValuePair<Tuple<string, string>, int> kvp2 in kvp1.Value)
                {
                    int x1 = kvp1.Value[new Tuple<string, string>(kvp2.Key.Item1, kvp2.Key.Item1)];
                    int x2 = kvp1.Value[new Tuple<string, string>(kvp2.Key.Item2, kvp2.Key.Item2)];
                    decimal jaccard = (decimal)kvp2.Value / (x1 + x2 - kvp2.Value);
                    sql = "insert into Jaccard (attribute, value_t, value_q, Jaccard) values ('"+kvp1.Key +"', '" + kvp2.Key.Item1 + "', '"+kvp2.Key.Item2+"'," + jaccard.ToString(CultureInfo.InvariantCulture) + ")";
                    if(kvp2.Key.Item1 != kvp2.Key.Item2)
                        sql += "; insert into Jaccard (attribute, value_t, value_q, Jaccard) values ('" + kvp1.Key + "', '" + kvp2.Key.Item2 +"', '" + kvp2.Key.Item1 +  "'," + jaccard.ToString(CultureInfo.InvariantCulture) + ")";
                    command = new SQLiteCommand(sql, metaDatabaseConnection);
                    command.ExecuteNonQuery();
                }
            }
            new SQLiteCommand("end", metaDatabaseConnection).ExecuteNonQuery();
        }

        public decimal CalculateIDFNumeric(List<Dictionary<string, string>> database, string attribute, int n, int t)
        {
                double[] difference = new double[n];
                double[] values = new double[n];
                int index = 0;
                foreach (Dictionary<string, string> row in database)
                {
                    double value = double.Parse(row[attribute]);
                    difference[index] = value - t; // ti-t
                    values[index] = value; // for calculation of h
                    index++;
                }
                //calculate std.dev.
                double average = values.Average();
                double sum = values.Sum(d => (d - average) * (d - average));
                double stdDev = Math.Sqrt((sum) / n);
                // calculate h
                double h = 1.06 * stdDev * Math.Pow(n, -0.2);
                // calculate idf
              //  double test1 = difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * (d / h))));
                double test = Math.Log10(n / difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * (d / h)))));
                return (decimal)test;
        }
        
        public decimal CalculateQFNumeric(string attribute,int n, int t)
        {
            if (workloadCounts[attribute].Count == 0)
                return 0;
            Dictionary<string, double> difference = new Dictionary<string, double>();
            Dictionary<string, double> values = new Dictionary<string, double>();
            int index = 0;
            int totalCount=0;
            foreach (KeyValuePair<string,int> row in workloadCounts[attribute])
            {
                double val = double.Parse(row.Key);
                //double value = double.Parse(row[attribute]);
                totalCount+=row.Value;
                difference[row.Key] = val - t; // ti-t
                values[row.Key] = val; // for calculation of h
                index++;
            }
            //calculate std.dev.
            double average = values.Sum(d=>d.Value*workloadCounts[attribute][d.Key])/totalCount;
            double sum = values.Sum(d => (d.Value - average) * (d.Value - average) * workloadCounts[attribute][d.Key]);
            double stdDev = Math.Sqrt(sum / totalCount);
            // calculate h
            double h = 1.06 * stdDev * Math.Pow(n, -0.2);
            // calculate qf
            //  double test1 = difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * (d / h))));
            double test = difference.Sum(d =>workloadCounts[attribute][d.Key]* Math.Pow(Math.E, (-0.5 * (d.Value / h) * (d.Value / h))));
            return (decimal)test;
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
                decimal idf = (decimal)Math.Log10((double)n / kvp.Value);
                string sql = "insert into IDF (attribute, value, IDF) values (\"" + attribute + "\", \"" + kvp.Key + "\", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
        }

        public void CalculateQFNumeric(string attribute, SQLiteConnection metaDatabaseConnection)
        {
            decimal max= workloadCounts[attribute].Count == 0? 1:workloadCounts[attribute].Max(d => d.Value);

            foreach (KeyValuePair<string, int> kvp in workloadCounts[attribute])
            {
                decimal qf = kvp.Value / max;
                string sql = "insert into QF (attribute, value, QF) values ('" + attribute + "', '" + kvp.Key + "', " + qf.ToString(CultureInfo.InvariantCulture) + ")";
                SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }

        }

        public void ParseTable(SQLiteConnection databaseConnection)
        {
            new SQLiteCommand("begin", databaseConnection).ExecuteNonQuery();
            // read and parse autompg.sql
            string strCommand = File.ReadAllText("autompg.sql");
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
            while ((s = stream.ReadLine() )!= "")
            {
                times = int.Parse(s.Split(' ')[0]);
                string where = s.Split(new string[] {"WHERE"}, StringSplitOptions.None)[1];
                string[] statements = where.Split(new string[] {"AND"}, StringSplitOptions.None);
                for (int i = 0; i < statements.Length; i++)
                {
                   
                    if (!statements[i].Contains("IN"))
                    {
                         string[] ss = statements[i].Split('=');
                        string attribute = ss[0].Trim();
                        string value = ss[1].Trim();
                        value = value.Substring(1, value.Length - 2);
                        if (workloadCounts[attribute].ContainsKey(value))
                            workloadCounts[attribute][value] += times;
                        else
                            workloadCounts[attribute][value] = times;
                    }
                    else
                    {
                        string[] ss = statements[i].Split(new string[]{"IN"}, StringSplitOptions.None);
                        string attribute = ss[0].Trim();
                        string value = ss[1].Trim();
                        string[] values = value.Substring(1, value.Length - 2).Split(',');
                        for (int x = 0; x < values.Length; x++)
                            values[x] = values[x].Substring(1, values[x].Length - 2);
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
    }
}
