using System.Data.SQLite;

namespace ChessWebsite
{
    public static class SQLClass
    {
        static SQLiteConnection sqlite_conn;
        static SQLClass()
        { sqlite_conn = StartConnection(); }
        private static SQLiteConnection StartConnection()
        {
            sqlite_conn = new SQLiteConnection("Data Source=chessdata.db;Version=3;New=True;Compress=True;");
            sqlite_conn.Open();
            return sqlite_conn;
        }
        public static void BuildTables()
        {
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = "CREATE TABLE Users (Username VARCHAR(20), Password VARCHAR(20))";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = "CREATE TABLE Games (Pgn VARCHAR(20), White VARCHAR(10), Black VARCHAR(10))";
            sqlite_cmd.ExecuteNonQuery();
        }
        public static void WriteData(string table, string columns, string values)
        {
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO {table} {columns} VALUES{values};";
            sqlite_cmd.ExecuteNonQuery();
        }
        public static SQLiteDataReader ReadData(string table, string columns, string condition)
        {
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = $"SELECT {columns} FROM {table}";
            if (condition != "")
                sqlite_cmd.CommandText += $" WHERE {condition}";
            sqlite_datareader = sqlite_cmd.ExecuteReader();
            return sqlite_datareader;
        }
    }
}