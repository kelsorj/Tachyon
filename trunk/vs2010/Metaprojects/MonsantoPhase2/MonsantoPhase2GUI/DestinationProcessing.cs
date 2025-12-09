using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.IO;
using BioNex.Shared.Utils;

namespace BioNex.CustomerGUIPlugins
{
    public interface IDestinationProcessing
    {

        /// <summary>
        /// Write destination plate data to the db, overwriting existing data if the dest plate already exists
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>
        void SaveWork( string destination_barcode, BioNex.GemsRpc.TransferMap mapping);

        /// <summary>
        /// Remove a message from the database, which means that the work has been completed
        /// </summary>
        /// <param name="barcode"></param>
        void DeleteWork( string destination_barcode);

        /// <summary>
        /// Get the next item from the db cache 
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>
        /// <returns>false if the db is empty</returns>
        bool GetNextWorkItem( out string destination_barcode, out BioNex.GemsRpc.TransferMap mapping);
    }

    /// <summary>
    /// Database interface class for handling persistent storage of SetFate and EndBatch messages
    /// </summary>
    public class DestinationProcessing : IDestinationProcessing
    {
        private object _connection_lock = new object();
        private SQLiteConnection _connection;
        private int _next_id = -1;

        private const string SourceBarcode = "source_barcode";
        private const string SourceRowIndex = "source_row_index";
        private const string SourceColumnIndex = "source_column_index";
        private const string DestinationBarcode = "destination_barcode";
        private const string DestinationRowIndex = "destination_row_index";
        private const string DestinationColumnIndex = "destination_column_index";
        private const string TransferVolume = "transfer_volume";
        private const string SensedVolume = "sensed_volume";

        public DestinationProcessing(string path)
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

        internal void CreateDatabase(string path)
        {
            SQLiteConnection.CreateFile(path);

            var create_command = new SQLiteCommand(@"CREATE TABLE [work](
                                                            [id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
                                                            [source_barcode] TEXT NOT NULL,
                                                            [source_row_index] INTEGER NOT NULL,
                                                            [source_column_index] INTEGER NOT NULL,
                                                            [destination_barcode] TEXT NOT NULL,
                                                            [destination_row_index] INTEGER NOT NULL,
                                                            [destination_column_index] INTEGER NOT NULL,
                                                            [transfer_volume] FLOAT NOT NULL,
                                                            [sensed_volume] FLOAT NOT NULL
                                                        );");
            create_command.Connection = _connection;
            ProtectedExecuteNonQuery(create_command);
        }

        public void SaveWork( string destination_barcode, BioNex.GemsRpc.TransferMap mapping)
        {
            var insert_command = new SQLiteCommand( string.Format(@"INSERT OR REPLACE INTO work (source_barcode, source_row_index, source_column_index, destination_barcode, destination_row_index, destination_column_index, transfer_volume, sensed_volume)
                                                                  VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}');",
                                                                  mapping.source_barcode, mapping.source_row, mapping.source_column,
                                                                  destination_barcode, mapping.destination_row, mapping.destination_column, mapping.transfer_volume, mapping.sensed_volume));
            insert_command.Connection = _connection;
            ProtectedExecuteNonQuery(insert_command);
        }

        public void DeleteWork( string destination_barcode)
        {
            var delete_command = new SQLiteCommand(string.Format(@"DELETE FROM work WHERE {0}='{1}'", DestinationBarcode, destination_barcode));
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

        private bool RecursiveNextWorkItem(out string destination_barcode, out BioNex.GemsRpc.TransferMap mapping)
        {
            destination_barcode = "";
            mapping = new GemsRpc.TransferMap();
            
            if (_next_id == -1) // if we are starting from the beginning, set _next_id to the first item's id
                _next_id = InitializeNextId();
            if (_next_id == -1) // if there were no items, we're done
                return false;

            // this query returns the first item in each fate -- _next_id is used to determine which of these first items we pick
            var query = new SQLiteCommand(string.Format(@"SELECT * FROM work WHERE id in (SELECT MIN(id) FROM work GROUP BY {0}) AND id >= {1}", DestinationBarcode, _next_id));
            query.Connection = _connection;

            var reader = query.ExecuteReader();
            var table = new DataTable();
            table.Load(reader);

            // if we didn't get anything, reset the id counter to the beginning and try again
            if (table.Rows.Count == 0)
            {
                _next_id = -1;
                return RecursiveNextWorkItem(out destination_barcode, out mapping);
            }

            // We may have found the next plate to process -- save its information
            var row = table.Rows[0];
            destination_barcode = row[DestinationBarcode].ToString();
            mapping.source_barcode = row[SourceBarcode].ToString();
            mapping.source_column = row[SourceColumnIndex].ToInt();
            mapping.source_row = row[SourceRowIndex].ToInt();
            mapping.destination_column = row[DestinationColumnIndex].ToInt();
            mapping.destination_row = row[DestinationRowIndex].ToInt();
            mapping.transfer_volume = row[TransferVolume].ToDouble();
            mapping.sensed_volume = row[SensedVolume].ToDouble();
            var id = row["id"].ToInt();
            _next_id = id + 1;

            return true;
        }

        public bool GetNextWorkItem( out string destination_barcode, out BioNex.GemsRpc.TransferMap mapping)
        {
            lock (_connection_lock)
            {
                _connection.Open();
                try
                {
                    return RecursiveNextWorkItem(out destination_barcode, out mapping); // need to break work out into a function that doesn't re-open the connection, since you can't!
                }
                finally
                {
                    _connection.Close();
                }
            }
        }

        public void SaveTransferMap( string destination_barcode, IList< BioNex.GemsRpc.TransferMap> mapping)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( "BEGIN IMMEDIATE TRANSACTION;");
            foreach( BioNex.GemsRpc.TransferMap map_item in mapping){
                sb.AppendFormat( @"INSERT OR REPLACE INTO work (source_barcode, source_row_index, source_column_index, destination_barcode, destination_row_index, destination_column_index, transfer_volume, sensed_volume)
                                   VALUES ('{0}','{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}');",
                                   map_item.source_barcode, map_item.source_row, map_item.source_column,
                                   destination_barcode, map_item.destination_row, map_item.destination_column, map_item.transfer_volume, map_item.sensed_volume);
            }
            sb.AppendFormat( "COMMIT TRANSACTION;");
            var insert_command = new SQLiteCommand( sb.ToString());
            insert_command.Connection = _connection;
            ProtectedExecuteNonQuery(insert_command);
        }

        public bool GetNextTransferMap( out string destination_barcode, out IList< BioNex.GemsRpc.TransferMap> mapping)
        {
            lock( _connection_lock){
                _connection.Open();
                try{
                    destination_barcode = null;
                    mapping = new List< GemsRpc.TransferMap>();
            
                    // this query returns the work pending to be retrieved from the work table.
                    var query = new SQLiteCommand( string.Format( @"SELECT * FROM work"));
                    query.Connection = _connection;

                    var reader = query.ExecuteReader();
                    var table = new DataTable();
                    table.Load(reader);

                    // if we didn't get anything, reset the id counter to the beginning and try again
                    if (table.Rows.Count == 0)
                    {
                        return false;
                    }

                    // we may have found the next plate to process -- save its information
                    foreach( int i in Enumerable.Range( 0, table.Rows.Count)){
                        var row = table.Rows[ i];
                        if( destination_barcode == null){
                            destination_barcode = row[ DestinationBarcode].ToString();
                        } else if( destination_barcode != row[ DestinationBarcode].ToString()){
                            continue;
                        }
                        mapping.Add( new GemsRpc.TransferMap(){ 
                            source_barcode = row[SourceBarcode].ToString(),
                            source_column = row[SourceColumnIndex].ToInt(),
                            source_row = row[SourceRowIndex].ToInt(),
                            destination_column = row[DestinationColumnIndex].ToInt(),
                            destination_row = row[DestinationRowIndex].ToInt(),
                            transfer_volume = row[TransferVolume].ToDouble(),
                            sensed_volume = row[SensedVolume].ToDouble()
                        });
                    }

                    return true;
                } finally{
                    _connection.Close();
                }
            }
        }
    }
}
