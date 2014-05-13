using System.Windows.Forms;
using System.Data.SQLite;

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
            
            
           
           
            string sql = "create table highscores (name varchar(20), score int)";
            SQLiteCommand command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();
            

            sql = "insert into highscores (name, score) values ('Me', 3000)";
            command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('Myself', 6000)";
            command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();
            sql = "insert into highscores (name, score) values ('And I', 9001)";
            command = new SQLiteCommand(sql, databaseConnection);
            command.ExecuteNonQuery();

            sql = "select * from highscores order by score desc";
            command = new SQLiteCommand(sql, databaseConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                label1.Text += "Name: " + reader["name"] + "\tScore: " + reader["score"] + '\n';

        }

        public SQLiteConnection CreateMetaDatabase()
        {
            SQLiteConnection.CreateFile("MetaDatabase.sqlite");
            SQLiteConnection metaDatabaseConnection;
            metaDatabaseConnection = new SQLiteConnection("Data Source=MetaDatabase.sqlite;Version=3;");
            metaDatabaseConnection.Open();

            // table with IDF
            string sql = "create table IDF (atribute varchar(20), value varchar(20), IDF real)";
            SQLiteCommand command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with QF
            sql = "create table QF (atribute varchar(20), value varchar(20), QF real)";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();

            // table with Jaccard
            sql = "create table Jaccard (atribute varchar(20), value1 varchar(20), value2 varchar(20),Jaccard real)";
            command = new SQLiteCommand(sql, metaDatabaseConnection);
            command.ExecuteNonQuery();
            return metaDatabaseConnection;
        }

        public void FillMetaDatabase(SQLiteConnection metaDatabaseConnection, SQLiteConnection databaseConnection)
        {
            // IDF



            // QF



            // Jaccard
        }

        public void ParseTable(SQLiteConnection databaseConnection)
        {
            // read and parse autompg

        }
    }
}
