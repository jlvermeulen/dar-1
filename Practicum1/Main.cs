using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Globalization;

namespace Practicum1
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            
            // create the MetaDatabase
            SQLiteConnection metaDatabaseConnection = CreateMetaDatabase();
            
            // make connection to the autompg database
            SQLiteConnection.CreateFile("autompg.sqlite");
            SQLiteConnection databaseConnection;
            databaseConnection = new SQLiteConnection("Data Source=autompg.sqlite;Version=3;");
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

            string sql = "select * from autompg";
            SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                label1.Text += "Model: " + reader["model"] + "\tBrand: " + reader["brand"] + '\n';

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
                row.Add("mpg", reader["id"].ToString());
                row.Add("cylinders", reader["cylinders"].ToString());
                row.Add("displacement", reader["displacement"].ToString());
                row.Add("horsepower", reader["horsepower"].ToString());
                row.Add("weight", reader["weight"].ToString());
                row.Add("acceleration", reader["acceleration"].ToString());
                row.Add("model_year", reader["model_year"].ToString());
                row.Add("origin", reader["origin"].ToString());
                row.Add("model", reader["model"].ToString());
                row.Add("type", reader["type"].ToString());
                database.Add(row);
            }


            // calculation and insertion of IDF values
            // mpg
            int n = database.Count;
            for (int t = 5; t < 50; t++)
            {
                decimal idf = CalculateIDFNumeric(database, "mpg", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('mpg', "+t+", "+idf.ToString(CultureInfo.InvariantCulture)+")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            //cylinders
            for (int t = 1; t <= 20; t++)
            {
                decimal idf = CalculateIDFNumeric(database, "cylinders", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('cylinders', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            // displacement
            for (int t = 50; t <= 500; t+=10)
            {
                decimal idf = CalculateIDFNumeric(database, "displacement", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('displacement', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            // horsepower
            for (int t = 30; t <= 300; t+=10)
            {
                decimal idf = CalculateIDFNumeric(database, "horsepower", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('horsepower', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            // weight
            for (int t = 1000; t <= 10000; t += 100)
            {
                decimal idf = CalculateIDFNumeric(database, "weight", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('weight', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            // acceleration
            for (int t = 5; t <= 30; t++)
            {
                decimal idf = CalculateIDFNumeric(database, "acceleration", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('acceleration', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            //model_year
            for (int t = 60; t <= 99; t++)
            {
                decimal idf = CalculateIDFNumeric(database, "model_year", n, t);
                sql = "insert into IDF (attribute, value, IDF) values ('model_year', " + t + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
            // origin
            CalculateIDFCategoric(database, "origin", n, metaDatabaseConnection);
            // model
            CalculateIDFCategoric(database, "model", n, metaDatabaseConnection);
            // type
            CalculateIDFCategoric(database, "type", n, metaDatabaseConnection);
           
            // calculation and insertion of QF values





            // Jaccard
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
                double average = values.Average();
                double sum = values.Sum(d => (d - average) * (d - average));
                //Put it all together      
                double stdDev = Math.Sqrt((sum) / (n - 1));
                double h = 1.06 * stdDev * Math.Pow(n, -0.2);
                double test1 = difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * d / h)));
                double test = Math.Log10(n / difference.Sum(d => Math.Pow(Math.E, (-0.5 * (d / h) * d / h))));
                return (decimal)test;
        }
        public void CalculateIDFCategoric(List<Dictionary<string, string>> database, string attribute, int n, SQLiteConnection metaDatabaseConnection)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (Dictionary<string, string> row in database)
            {
                if (counts.ContainsKey(row["origin"]))
                    counts[row["origin"]] += 1;
                else
                    counts.Add(row["origin"], 0);
            }
            foreach (KeyValuePair<string, int> kvp in counts)
            {
                decimal idf = (decimal)Math.Log10(n / kvp.Value);
                string sql = "insert into IDF (attribute, value, IDF) values (\"" + attribute + "\", " + kvp.Key + ", " + idf.ToString(CultureInfo.InvariantCulture) + ")";
                SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
                command.ExecuteNonQuery();
            }
        }

        public void ParseTable(SQLiteConnection databaseConnection)
        {
            // read and parse autompg.sql
            string strCommand = File.ReadAllText("autompg.sql");
            SQLiteCommand command = databaseConnection.CreateCommand();
            command.CommandText = strCommand;
            command.ExecuteNonQuery();

        }
    }
}
