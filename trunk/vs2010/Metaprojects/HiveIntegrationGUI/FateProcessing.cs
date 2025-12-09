using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.IO;
using BioNex.Shared.Utils;

namespace BioNex.HiveIntegration
{
    public interface IFateProcessing
    {

        /// <summary>
        /// Write a message to the db, overwriting fate if barcode exists in the db
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>
        void SaveWork(string barcode, string fate);

        /// <summary>
        /// Remove a message from the database, which means that the work has been completed
        /// </summary>
        /// <param name="barcode"></param>
        void DeleteWork(string barcode);

        /// <summary>
        /// Get the next item from the db - 
        ///     1. Should step through items by ordinal so that they can be processed in a round-robin.  Next ordinal is kept internally.
        ///     2. If the next item is an ##END_BATCH## item, then check to see if the batch is complete, otherwise skip the end-batch item for now
        ///        - a batch is complete if none of the items with lower ordinal belong to the same fate as the batch complete marker
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>
        /// <returns>false if the db is empty</returns>
        bool GetNextWorkItem(out string barcode, out string fate);

        /// <summary>
        /// return true if there is any work pending for the specified fate
        /// </summary>
        /// <param name="fate"></param>
        /// <returns></returns>
        bool FatesPending(string fate);
    }

    /// <summary>
    /// Database interface class for handling persistent storage of SetFate and EndBatch messages
    /// </summary>
    public class FateProcessing : IFateProcessing
    {
        public static string END_BATCH_TOKEN = "##END_BATCH##";

        private object _connection_lock = new object();
        private SQLiteConnection _connection;
        private int _next_id = -1;

        public FateProcessing(string path)
        {
            lock (_connection_lock)
            {
                _connection = new SQLiteConnection(String.Format("Data Source={0}", path));
            }
            if (!File.Exists(path))
                CreateDatabase(path);
        }

        private void ProtectedExecuteNonQuery(SQLiteCommand command)
        {
            lock (_connection_lock)
            {
                _connection.Open();
                try
                {
                    command.ExecuteNonQuery();
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        private void CreateDatabase(string path)
        {
            SQLiteConnection.CreateFile(path);

            var create_command = new SQLiteCommand(@"CREATE TABLE [work](
                                                            [id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
                                                            [barcode] TEXT NOT NULL,
                                                            [fate] TEXT NOT NULL,
                                                            CONSTRAINT [unique_barcode] UNIQUE (barcode)
                                                        );");
            create_command.Connection = _connection;
            ProtectedExecuteNonQuery(create_command);
        }

        public void SaveWork(string barcode, string fate)
        {
            var insert_command = new SQLiteCommand( string.Format(@"INSERT OR REPLACE INTO work (barcode, fate) VALUES ('{0}','{1}');", barcode, fate));
            insert_command.Connection = _connection;
            ProtectedExecuteNonQuery(insert_command);
        }

        public void DeleteWork(string barcode)
        {
            var delete_command = new SQLiteCommand(string.Format(@"DELETE FROM work WHERE barcode='{0}'", barcode));
            delete_command.Connection = _connection;
            ProtectedExecuteNonQuery(delete_command);
        }

        private int InitializeNextId()
        {
            var query = new SQLiteCommand(@"SELECT id FROM work ORDER BY id ASC");
            query.Connection = _connection;

            var reader = query.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);

            if (table.Rows.Count == 0)
                return -1;
            return table.Rows[0]["id"].ToInt();
        }

        private bool RecursiveNextWorkItem(out string barcode, out string fate)
        {
            barcode = "";
            fate = "";

            if (_next_id == -1) // if we are starting from the beginning, set _next_id to the first item's id
                _next_id = InitializeNextId();
            if (_next_id == -1) // if there were no items, we're done
                return false;

            // this query returns the first item in each fate -- _next_id is used to determine which of these first items we pick
            var query = new SQLiteCommand(string.Format(@"SELECT * FROM work WHERE id in (SELECT MIN(id) FROM work GROUP BY fate) AND id >= {0}", _next_id));
            query.Connection = _connection;

            var reader = query.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);

            // if we didn't get anything, reset the id counter to the beginning and try again
            if (table.Rows.Count == 0)
            {
                _next_id = -1;
                return RecursiveNextWorkItem(out barcode, out fate);
            }

            // We may have found the next plate to process -- save its information
            var row = table.Rows[0];
            barcode = row["barcode"].ToString();
            fate = row["fate"].ToString();
            var id = row["id"].ToInt();
            _next_id = id + 1;

            return true;
        }

        public bool GetNextWorkItem(out string barcode, out string fate)
        {

            lock (_connection_lock)
            {
                _connection.Open();
                try
                {
                    return RecursiveNextWorkItem(out barcode, out fate); // need to break work out into a function that doesn't re-open the connection, since you can't!
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        public bool FatesPending(string fate)
        {
            var query = new SQLiteCommand(string.Format(@"SELECT id FROM work WHERE fate='{0}'", fate));
            query.Connection = _connection;

            lock (_connection_lock)
            {
                _connection.Open();
                try
                {
                    var reader = query.ExecuteReader();
                    var table = new DataTable();
                    table.Load(reader);

                    // if we didn't find an item at _next_id, reset _next_id to the beginning of the table and try again
                    return table.Rows.Count != 0;
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

    }
}
